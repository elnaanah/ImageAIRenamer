using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageAIRenamer.Domain.Entities;
using ImageAIRenamer.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace ImageAIRenamer.Application.Common;

public abstract partial class ImageProcessingViewModelBase : ViewModelBase, IDisposable
{
    protected readonly INavigationService _navigationService;
    protected readonly IFileService _fileService;
    protected readonly IConfigurationService _configurationService;
    protected readonly ILogger _logger;
    protected CancellationTokenSource? _cancellationTokenSource;

    protected ImageProcessingViewModelBase(
        INavigationService navigationService,
        IFileService fileService,
        IConfigurationService configurationService,
        ILogger logger)
    {
        _navigationService = navigationService;
        _fileService = fileService;
        _configurationService = configurationService;
        _logger = logger;
        
        _ = LoadApiKeysAsync();
    }

    [ObservableProperty]
    protected string sourceFolder = string.Empty;

    [ObservableProperty]
    protected string outputFolder = string.Empty;

    [ObservableProperty]
    protected string apiKeys = string.Empty;

    [ObservableProperty]
    protected string statusText = string.Empty;

    [ObservableProperty]
    protected double progressValue;

    [ObservableProperty]
    protected double progressMaximum;

    [ObservableProperty]
    protected bool isProgressVisible;

    [ObservableProperty]
    protected bool enableSpeedBoost = false;

    [ObservableProperty]
    protected string elapsedTime = string.Empty;

    private DateTime? _processingStartTime;
    private System.Timers.Timer? _elapsedTimeTimer;

    protected abstract ObservableCollection<ImageItem> ImagesCollection { get; }

    protected abstract IAsyncRelayCommand? MainCommand { get; }
    protected IRelayCommand? _cancelCommand;

    public IRelayCommand CancelCommand => _cancelCommand ??= new RelayCommand(() =>
    {
        CancelCurrentOperation();
        IsProgressVisible = false;
        OnCancelOperation();
        NotifyCommands();
        StatusText = "تم إلغاء العملية.";
    }, () => IsProgressVisible);

    protected virtual void OnCancelOperation()
    {
    }

    protected void NotifyCommands()
    {
        MainCommand?.NotifyCanExecuteChanged();
        _cancelCommand?.NotifyCanExecuteChanged();
    }

    public IRelayCommand BrowseSourceCommand => new RelayCommand(() =>
    {
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            SourceFolder = dialog.FolderName;
            _ = LoadImagesAsync(SourceFolder, ShouldSetSelectedOnLoad());
        }
    });

    protected virtual bool ShouldSetSelectedOnLoad() => false;

    public IRelayCommand BrowseOutputCommand => new RelayCommand(() =>
    {
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            OutputFolder = dialog.FolderName;
            OnOutputFolderChanged();
        }
    });

    public IRelayCommand BackToHomeCommand => new RelayCommand(() =>
    {
        _navigationService.NavigateToWelcome();
    });

    public IRelayCommand<ImageItem> OpenImageCommand => new RelayCommand<ImageItem>(item =>
    {
        if (item != null)
        {
            try
            {
                Process.Start(new ProcessStartInfo(item.FilePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to open image: {FilePath}", item.FilePath);
            }
        }
    });

    public IRelayCommand ClearListCommand => new RelayCommand(() =>
    {
        ClearAllData();
        OnClearList();
    });

    protected virtual void OnClearList()
    {
    }

    protected virtual void ClearAllData()
    {
        _elapsedTimeTimer?.Stop();
        _elapsedTimeTimer?.Dispose();
        _elapsedTimeTimer = null;
        _processingStartTime = null;
        
        ImagesCollection.Clear();
        SourceFolder = string.Empty;
        OutputFolder = string.Empty;
        StatusText = string.Empty;
        ElapsedTime = string.Empty;
        ProgressValue = 0;
        IsProgressVisible = false;
    }

    partial void OnOutputFolderChanged(string value)
    {
        OnOutputFolderChanged();
    }

    partial void OnSourceFolderChanged(string value)
    {
    }

    protected virtual void OnOutputFolderChanged()
    {
        OnReadyStateChanged();
    }

    protected virtual void OnImagesLoaded()
    {
        OnReadyStateChanged();
    }

    protected virtual void OnReadyStateChanged()
    {
    }

    protected async Task LoadApiKeysAsync()
    {
        try
        {
            var keys = await _configurationService.GetApiKeysAsync();
            ApiKeys = string.Join(Environment.NewLine, keys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load API keys");
        }
    }

    protected async Task LoadImagesAsync(string folder, bool setSelected = false)
    {
        try
        {
            ImagesCollection.Clear();
            var extensions = _configurationService.GetSupportedExtensions();
            var files = await _fileService.LoadImageFilesAsync(folder, extensions);

            foreach (var file in files)
            {
                ImagesCollection.Add(new ImageItem
                {
                    FilePath = file,
                    OriginalName = Path.GetFileNameWithoutExtension(file),
                    Status = ImageStatusConstants.Pending,
                    IsSelected = setSelected
                });
            }

            StatusText = string.Format(SuccessMessages.ImagesLoaded, ImagesCollection.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load images from folder: {Folder}", folder);
            StatusText = string.Format(ErrorMessages.LoadImagesError, ex.Message);
            ImagesCollection.Clear();
        }
        finally
        {
            OnImagesLoaded();
        }
    }

    protected string[] ValidateAndSetupApiKeys()
    {
        var apiKeysArray = ApiKeys.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (apiKeysArray.Length == 0)
        {
            ShowError(ErrorMessages.NoApiKeys, "خطأ");
            return Array.Empty<string>();
        }

        if (EnableSpeedBoost && apiKeysArray.Length < 2)
        {
            ShowWarning("يحتاج التسريع إلى مفتاحين API على الأقل. سيتم استخدام المعالجة المتسلسلة.", "تنبيه");
            EnableSpeedBoost = false;
        }

        return apiKeysArray;
    }

    protected bool EnsureOutputFolderExists()
    {
        if (!Directory.Exists(OutputFolder))
        {
            try
            {
                Directory.CreateDirectory(OutputFolder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create output folder: {Folder}", OutputFolder);
                ShowError(ErrorMessages.CannotCreateOutputFolder, "خطأ");
                return false;
            }
        }

        return true;
    }

    protected void StartProcessing(int totalCount)
    {
        IsProgressVisible = true;
        ProgressMaximum = totalCount;
        ProgressValue = 0;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();
        _processingStartTime = DateTime.Now;
        ElapsedTime = "00:00:00";
        
        _elapsedTimeTimer?.Stop();
        _elapsedTimeTimer?.Dispose();
        _elapsedTimeTimer = new System.Timers.Timer(100);
        _elapsedTimeTimer.Elapsed += (sender, e) =>
        {
            if (_processingStartTime.HasValue)
            {
                var elapsed = DateTime.Now - _processingStartTime.Value;
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ElapsedTime = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
                });
            }
        };
        _elapsedTimeTimer.Start();
        
        NotifyCommands();
    }

    protected void EndProcessing(string completionMessage)
    {
        _elapsedTimeTimer?.Stop();
        _elapsedTimeTimer?.Dispose();
        _elapsedTimeTimer = null;
        
        if (_processingStartTime.HasValue)
        {
            var elapsed = DateTime.Now - _processingStartTime.Value;
            ElapsedTime = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
            _processingStartTime = null;
        }
        
        IsProgressVisible = false;
        StatusText = completionMessage;
        NotifyCommands();
        OnProcessingCompleted();
    }

    protected virtual void OnProcessingCompleted()
    {
    }

    protected void CancelCurrentOperation()
    {
        _cancellationTokenSource?.Cancel();
    }

    public void Dispose()
    {
        _elapsedTimeTimer?.Stop();
        _elapsedTimeTimer?.Dispose();
        _elapsedTimeTimer = null;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }
}

