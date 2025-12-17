using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageAIRenamer.Application.Common;
using ImageAIRenamer.Domain.Entities;
using ImageAIRenamer.Domain.Interfaces;
using Microsoft.Win32;

namespace ImageAIRenamer.Application.ViewModels;

/// <summary>
/// ViewModel for the image search page
/// </summary>
public partial class ImageSearchViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IGeminiService _geminiService;
    private readonly IFileService _fileService;
    private readonly IConfigurationService _configurationService;
    private CancellationTokenSource? _cancellationTokenSource;

    public ImageSearchViewModel(
        INavigationService navigationService,
        IGeminiService geminiService,
        IFileService fileService,
        IConfigurationService configurationService)
    {
        _navigationService = navigationService;
        _geminiService = geminiService;
        _fileService = fileService;
        _configurationService = configurationService;
        
        // Initialize SearchCommand once
        _searchCommand = new AsyncRelayCommand(SearchImagesAsync, () => IsSearchEnabled);
        
        _ = LoadApiKeysAsync();
    }

    [ObservableProperty]
    private ObservableCollection<ImageItem> matchedImages = new();

    [ObservableProperty]
    private string sourceFolder = string.Empty;

    [ObservableProperty]
    private string outputFolder = string.Empty;

    [ObservableProperty]
    private string searchDescription = string.Empty;

    [ObservableProperty]
    private string apiKeys = string.Empty;

    [ObservableProperty]
    private string statusText = string.Empty;

    [ObservableProperty]
    private string progressText = string.Empty;

    [ObservableProperty]
    private double progressValue;

    [ObservableProperty]
    private double progressMaximum;

    [ObservableProperty]
    private bool isProgressVisible;

    [ObservableProperty]
    private bool isSearchEnabled = false;

    private readonly IAsyncRelayCommand _searchCommand;

    /// <summary>
    /// Command to search images
    /// </summary>
    public IAsyncRelayCommand SearchCommand => _searchCommand;

    /// <summary>
    /// Command to browse for source folder
    /// </summary>
    public IRelayCommand BrowseSourceCommand => new RelayCommand(() =>
    {
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            SourceFolder = dialog.FolderName;
            // Load images asynchronously - CheckReady() is called after loading completes
            _ = LoadImagesAsync(SourceFolder);
        }
    });

    /// <summary>
    /// Command to browse for output folder
    /// </summary>
    public IRelayCommand BrowseOutputCommand => new RelayCommand(() =>
    {
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            OutputFolder = dialog.FolderName;
            CheckReady();
        }
    });

    /// <summary>
    /// Command to clear the list
    /// </summary>
    public IRelayCommand ClearListCommand => new RelayCommand(() =>
    {
        MatchedImages.Clear();
        SourceFolder = string.Empty;
        OutputFolder = string.Empty;
        SearchDescription = string.Empty;
        StatusText = string.Empty;
        ProgressText = string.Empty;
        ProgressValue = 0;
        IsProgressVisible = false;
        CheckReady();
    });

    /// <summary>
    /// Command to navigate back to home
    /// </summary>
    public IRelayCommand BackToHomeCommand => new RelayCommand(() =>
    {
        _navigationService.NavigateToWelcome();
    });

    /// <summary>
    /// Command to copy selected images
    /// </summary>
    public IRelayCommand CopySelectedCommand => new RelayCommand(() => _ = CopySelectedImagesAsync());

    /// <summary>
    /// Command to open an image
    /// </summary>
    public IRelayCommand<ImageItem> OpenImageCommand => new RelayCommand<ImageItem>(item =>
    {
        if (item != null)
        {
            try
            {
                Process.Start(new ProcessStartInfo(item.FilePath) { UseShellExecute = true });
            }
            catch
            {
                // Silently fail
            }
        }
    });

    /// <summary>
    /// Handles search description text changes
    /// </summary>
    partial void OnSearchDescriptionChanged(string value)
    {
        CheckReady();
    }

    /// <summary>
    /// Handles output folder changes
    /// </summary>
    partial void OnOutputFolderChanged(string value)
    {
        CheckReady();
    }

    /// <summary>
    /// Handles source folder changes
    /// </summary>
    partial void OnSourceFolderChanged(string value)
    {
        // SourceFolder changes trigger LoadImagesAsync which calls CheckReady() after loading
        // No need to check here as images haven't loaded yet
    }

    private async Task LoadApiKeysAsync()
    {
        try
        {
            var keys = await _configurationService.GetApiKeysAsync();
            ApiKeys = string.Join(Environment.NewLine, keys);
        }
        catch
        {
            // Silently fail
        }
    }

    private async Task LoadImagesAsync(string folder)
    {
        try
        {
            MatchedImages.Clear();
            var extensions = _configurationService.GetSupportedExtensions();
            var files = await _fileService.LoadImageFilesAsync(folder, extensions);

            foreach (var file in files)
            {
                MatchedImages.Add(new ImageItem
                {
                    FilePath = file,
                    OriginalName = Path.GetFileNameWithoutExtension(file),
                    Status = "في الانتظار",
                    IsSelected = true
                });
            }

            StatusText = $"تم تحميل {MatchedImages.Count} صورة.";
        }
        catch (Exception ex)
        {
            StatusText = $"حدث خطأ أثناء تحميل الصور: {ex.Message}";
            MatchedImages.Clear();
        }
        finally
        {
            // Always check ready state after loading attempt
            CheckReady();
        }
    }

    private void CheckReady()
    {
        IsSearchEnabled = MatchedImages.Count > 0
            && !string.IsNullOrWhiteSpace(OutputFolder)
            && !string.IsNullOrWhiteSpace(SearchDescription);
        _searchCommand.NotifyCanExecuteChanged();
    }

    private async Task SearchImagesAsync()
    {
        var apiKeysArray = ApiKeys.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (apiKeysArray.Length == 0)
        {
            ShowError("الرجاء إدخال مفتاح Gemini API واحد على الأقل.", "خطأ");
            return;
        }

        if (string.IsNullOrWhiteSpace(SearchDescription))
        {
            ShowError("الرجاء إدخال وصف البحث.", "خطأ");
            return;
        }

        await _configurationService.SaveApiKeysAsync(apiKeysArray);

        if (!Directory.Exists(OutputFolder))
        {
            try
            {
                Directory.CreateDirectory(OutputFolder);
            }
            catch
            {
                ShowError("تعذر إنشاء مجلد الإخراج.", "خطأ");
                return;
            }
        }

        // Set API keys on service
        if (_geminiService is Infrastructure.Services.GeminiService geminiServiceImpl)
        {
            geminiServiceImpl.SetApiKeys(apiKeysArray);
        }

        IsSearchEnabled = false;
        _searchCommand.NotifyCanExecuteChanged();
        IsProgressVisible = true;
        ProgressMaximum = MatchedImages.Count;
        ProgressValue = 0;

        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        var searchDescription = SearchDescription;
        var usedNames = new Dictionary<string, int>();
        int processedCount = 0;
        int matchedCount = 0;

        try
        {
            foreach (var img in MatchedImages)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                img.Status = "جاري المعالجة...";
                ProgressText = $"جاري المعالجة {processedCount + 1} من {MatchedImages.Count}...";

                try
                {
                    var result = await _geminiService.SearchImageAsync(img.FilePath, searchDescription, cancellationToken);

                    if (result.IsMatch)
                    {
                        img.Status = "مطابق";
                        matchedCount++;

                        // Generate name if not provided
                        if (string.IsNullOrWhiteSpace(result.SuggestedName))
                        {
                            result.SuggestedName = await _geminiService.GenerateTitleAsync(img.FilePath, cancellationToken: cancellationToken);
                        }

                        var sanitized = _fileService.SanitizeFilename(result.SuggestedName ?? "صورة");
                        var baseName = sanitized;
                        int counter = 0;

                        if (usedNames.ContainsKey(baseName))
                        {
                            counter = usedNames[baseName];
                        }
                        usedNames[baseName] = counter + 1;

                        if (counter > 0)
                        {
                            sanitized = $"{baseName}_{counter}";
                        }

                        var ext = Path.GetExtension(img.FilePath);
                        var uniquePath = _fileService.EnsureUniqueFilename(OutputFolder, sanitized, ext);
                        var newFileName = Path.GetFileName(uniquePath);
                        img.NewName = newFileName;
                    }
                    else
                    {
                        img.Status = "غير مطابق";
                        img.IsSelected = false;
                    }
                }
                catch (OperationCanceledException)
                {
                    img.Status = "ملغي";
                    break;
                }
                catch (Exception)
                {
                    img.Status = "خطأ";
                    img.IsSelected = false;
                }

                processedCount++;
                ProgressValue = processedCount;
            }
        }
        finally
        {
            IsProgressVisible = false;
            IsSearchEnabled = true;
            _searchCommand.NotifyCanExecuteChanged();
            StatusText = $"اكتمل البحث! تم العثور على {matchedCount} صورة مطابقة من أصل {MatchedImages.Count}.";
            ProgressText = $"مطابق: {matchedCount} / {MatchedImages.Count}";
        }
    }

    private async Task CopySelectedImagesAsync()
    {
        if (!Directory.Exists(OutputFolder))
        {
            ShowError("الرجاء اختيار مجلد الإخراج أولاً.", "خطأ");
            return;
        }

        var selectedImages = MatchedImages.Where(i => i.IsSelected && i.Status == "مطابق").ToList();

        if (selectedImages.Count == 0)
        {
            ShowWarning("الرجاء تحديد صورة واحدة على الأقل للنسخ.", "تنبيه");
            return;
        }

        int copiedCount = 0;
        int skippedCount = 0;

        foreach (var img in selectedImages)
        {
            try
            {
                var ext = Path.GetExtension(img.FilePath);
                var fileName = !string.IsNullOrWhiteSpace(img.NewName)
                    ? img.NewName
                    : Path.GetFileName(img.FilePath);

                var destPath = Path.Combine(OutputFolder, fileName);
                var uniquePath = _fileService.EnsureUniqueFilename(OutputFolder, Path.GetFileNameWithoutExtension(fileName), ext);

                await _fileService.CopyFileAsync(img.FilePath, uniquePath, true);
                img.NewName = Path.GetFileName(uniquePath);
                img.Status = "تم النسخ";
                copiedCount++;
            }
            catch
            {
                img.Status = "خطأ في النسخ";
                skippedCount++;
            }
        }

        string message = copiedCount > 0
            ? $"تم نسخ {copiedCount} صورة بنجاح إلى مجلد الإخراج."
            : "لم يتم نسخ أي صورة.";

        if (skippedCount > 0)
        {
            message += $"\n{skippedCount} صورة فشل نسخها.";
        }

        if (copiedCount > 0)
        {
            ShowInfo(message, "نجح");
        }
        else
        {
            ShowWarning(message, "تنبيه");
        }
    }
}
