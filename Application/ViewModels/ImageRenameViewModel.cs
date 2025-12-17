using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageAIRenamer.Application.Common;
using ImageAIRenamer.Domain.Entities;
using ImageAIRenamer.Domain.Interfaces;
using Microsoft.Win32;

namespace ImageAIRenamer.Application.ViewModels;

/// <summary>
/// ViewModel for the image rename page
/// </summary>
public partial class ImageRenameViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;
    private readonly IGeminiService _geminiService;
    private readonly IFileService _fileService;
    private readonly IConfigurationService _configurationService;
    private CancellationTokenSource? _cancellationTokenSource;

    public ImageRenameViewModel(
        INavigationService navigationService,
        IGeminiService geminiService,
        IFileService fileService,
        IConfigurationService configurationService)
    {
        _navigationService = navigationService;
        _geminiService = geminiService;
        _fileService = fileService;
        _configurationService = configurationService;
        _ = LoadApiKeysAsync();
    }

    [ObservableProperty]
    private ObservableCollection<ImageItem> images = new();

    [ObservableProperty]
    private string sourceFolder = string.Empty;

    [ObservableProperty]
    private string outputFolder = string.Empty;

    [ObservableProperty]
    private string customInstructions = string.Empty;

    [ObservableProperty]
    private string apiKeys = string.Empty;

    [ObservableProperty]
    private string statusText = string.Empty;

    [ObservableProperty]
    private double progressValue;

    [ObservableProperty]
    private double progressMaximum;

    [ObservableProperty]
    private bool isProgressVisible;

    [ObservableProperty]
    private bool isProcessEnabled = false;

    /// <summary>
    /// Command to browse for source folder
    /// </summary>
    public IRelayCommand BrowseSourceCommand => new RelayCommand(() =>
    {
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            SourceFolder = dialog.FolderName;
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
        Images.Clear();
        SourceFolder = string.Empty;
        OutputFolder = string.Empty;
        CustomInstructions = string.Empty;
        StatusText = string.Empty;
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
    /// Command to process images
    /// </summary>
    public IAsyncRelayCommand ProcessCommand => new AsyncRelayCommand(ProcessImagesAsync, () => IsProcessEnabled);

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
        Images.Clear();
        var extensions = _configurationService.GetSupportedExtensions();
        var files = await _fileService.LoadImageFilesAsync(folder, extensions);

        foreach (var file in files)
        {
            Images.Add(new ImageItem
            {
                FilePath = file,
                OriginalName = Path.GetFileNameWithoutExtension(file),
                Status = "في الانتظار"
            });
        }

        StatusText = $"تم تحميل {Images.Count} صورة.";
        CheckReady();
    }

    private void CheckReady()
    {
        IsProcessEnabled = Images.Count > 0 && !string.IsNullOrWhiteSpace(OutputFolder);
        ProcessCommand.NotifyCanExecuteChanged();
    }

    private async Task ProcessImagesAsync()
    {
        var apiKeysArray = ApiKeys.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (apiKeysArray.Length == 0)
        {
            ShowError("الرجاء إدخال مفتاح Gemini API واحد على الأقل.", "خطأ");
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

        IsProcessEnabled = false;
        ProcessCommand.NotifyCanExecuteChanged();
        IsProgressVisible = true;
        ProgressMaximum = Images.Count;
        ProgressValue = 0;

        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        var customInstructions = CustomInstructions;
        var usedNames = new Dictionary<string, int>();

        try
        {
            foreach (var img in Images)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                img.Status = "جاري إعادة التسمية...";

                try
                {
                    var title = await _geminiService.GenerateTitleAsync(img.FilePath, customInstructions, cancellationToken);
                    var sanitized = _fileService.SanitizeFilename(title);

                    var baseName = sanitized;
                    int counter = 0;

                    // Check if this name was already used in this batch
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

                    await _fileService.CopyFileAsync(img.FilePath, uniquePath, true);

                    img.NewName = newFileName;
                    img.Status = "تم";
                }
                catch (OperationCanceledException)
                {
                    img.Status = "ملغي";
                    break;
                }
                catch (Exception)
                {
                    img.Status = "خطأ";
                }

                ProgressValue++;
            }
        }
        finally
        {
            IsProgressVisible = false;
            IsProcessEnabled = true;
            ProcessCommand.NotifyCanExecuteChanged();
            StatusText = "اكتملت المعالجة!";
            ShowInfo("اكتملت المعالجة!", "نجح");
        }
    }
}
