using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageAIRenamer.Application.Common;
using ImageAIRenamer.Domain.Entities;
using ImageAIRenamer.Domain.Interfaces;
using ImageAIRenamer.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace ImageAIRenamer.Application.ViewModels;

public partial class ImageRenameViewModel : ImageProcessingViewModelBase
{
    private readonly IGeminiService _geminiService;
    private readonly IImageProcessingService _imageProcessingService;


    [ObservableProperty]
    private ObservableCollection<ImageItem> images = new();

    [ObservableProperty]
    private string customInstructions = string.Empty;

    [ObservableProperty]
    private string progressText = string.Empty;

    [ObservableProperty]
    private bool isProcessEnabled = false;

    private readonly IAsyncRelayCommand _processCommand;

    public ImageRenameViewModel(
        INavigationService navigationService,
        IGeminiService geminiService,
        IFileService fileService,
        IConfigurationService configurationService,
        IImageProcessingService imageProcessingService,
        ILogger<ImageRenameViewModel> logger)
        : base(navigationService, fileService, configurationService, logger)
    {
        _geminiService = geminiService;
        _imageProcessingService = imageProcessingService;
        
        _processCommand = new AsyncRelayCommand(ProcessImagesAsync, () => IsProcessEnabled);
    }

    public IAsyncRelayCommand ProcessCommand => _processCommand;

    protected override IAsyncRelayCommand? MainCommand => _processCommand;

    protected override void OnCancelOperation()
    {
        IsProcessEnabled = true;
        ProgressText = string.Empty;
    }

    protected override void ClearAllData()
    {
        base.ClearAllData();
        CustomInstructions = string.Empty;
        ProgressText = string.Empty;
    }

    protected override void OnClearList()
    {
        OnReadyStateChanged();
    }

    protected override ObservableCollection<ImageItem> ImagesCollection => Images;

    protected override void OnReadyStateChanged()
    {
        IsProcessEnabled = Images.Count > 0 && !string.IsNullOrWhiteSpace(OutputFolder);
        NotifyCommands();
    }

    private async Task ProcessImagesAsync()
    {
        var apiKeysArray = ValidateAndSetupApiKeys();
        if (apiKeysArray.Length == 0)
        {
            return;
        }

        await _configurationService.SaveApiKeysAsync(apiKeysArray);

        if (!EnsureOutputFolderExists())
        {
            return;
        }

        _geminiService.SetApiKeys(apiKeysArray);

        IsProcessEnabled = false;
        StartProcessing(Images.Count);
        var cancellationToken = _cancellationTokenSource!.Token;

        var customInstructions = CustomInstructions;

        try
        {
            if (EnableSpeedBoost && apiKeysArray.Length >= 2)
            {
                await ProcessImagesParallelAsync(apiKeysArray, customInstructions, cancellationToken);
            }
            else
            {
                await ProcessImagesSequentialAsync(customInstructions, cancellationToken);
            }
        }
        finally
        {
            IsProcessEnabled = true;
            ProgressText = $"تمت معالجة {Images.Count} صورة.";
            EndProcessing(SuccessMessages.ProcessingCompleted);
            ShowInfo(SuccessMessages.ProcessingCompleted, "نجح");
            _logger.LogInformation("Image renaming completed. Processed {Count} images", Images.Count);
        }
    }

    private async Task ProcessImagesSequentialAsync(string customInstructions, CancellationToken cancellationToken)
    {
        var usedNames = new Dictionary<string, int>();
        int processedCount = 0;

        foreach (var img in Images)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                img.Status = ImageStatusConstants.Renaming;
                ProgressText = $"جاري إعادة التسمية {processedCount + 1} من {Images.Count}...";
            });

            var result = await _imageProcessingService.ProcessImageForRenameAsync(
                img,
                OutputFolder,
                customInstructions,
                usedNames,
                _geminiService,
                _fileService,
                cancellationToken);

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                img.Status = result.Status;
                if (result.Success)
                {
                    img.NewName = result.NewFileName ?? string.Empty;
                }
                processedCount++;
                ProgressValue = processedCount;
            });
        }
    }

    private async Task ProcessImagesParallelAsync(string[] apiKeysArray, string customInstructions, CancellationToken cancellationToken)
    {
        var usedNames = new ConcurrentDictionary<string, int>();
        var semaphore = new SemaphoreSlim(apiKeysArray.Length, apiKeysArray.Length);
        int processedCount = 0;
        var tasks = new List<Task>();

        foreach (var img in Images)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var image = img;
            var task = Task.Run(async () =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    var apiKeyIndex = _geminiService.GetNextApiKeyIndex();
                    var geminiServiceWrapper = new GeminiServiceWrapper(_geminiService, apiKeyIndex);

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        image.Status = ImageStatusConstants.Renaming;
                    });

                    var result = await _imageProcessingService.ProcessImageForRenameAsync(
                        image,
                        OutputFolder,
                        customInstructions,
                        usedNames,
                        geminiServiceWrapper,
                        _fileService,
                        cancellationToken);

                    var currentCount = Interlocked.Increment(ref processedCount);
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        image.Status = result.Status;
                        if (result.Success)
                        {
                            image.NewName = result.NewFileName ?? string.Empty;
                        }
                        ProgressText = $"جاري إعادة التسمية {currentCount} من {Images.Count}...";
                        ProgressValue = currentCount;
                    });
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("نفاذ"))
                {
                    _logger.LogWarning("Quota exceeded while processing image in parallel: {FilePath}", image.FilePath);
                    var currentCount = Interlocked.Increment(ref processedCount);
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        image.Status = ImageStatusConstants.QuotaExceeded;
                        ProgressText = $"جاري إعادة التسمية {currentCount} من {Images.Count}...";
                        ProgressValue = currentCount;
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing image in parallel: {FilePath}", image.FilePath);
                    var currentCount = Interlocked.Increment(ref processedCount);
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        image.Status = ImageStatusConstants.Error;
                        ProgressText = $"جاري إعادة التسمية {currentCount} من {Images.Count}...";
                        ProgressValue = currentCount;
                    });
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken);

            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    private class GeminiServiceWrapper : IGeminiService
    {
        private readonly IGeminiService _service;
        private readonly int _apiKeyIndex;

        public GeminiServiceWrapper(IGeminiService service, int apiKeyIndex)
        {
            _service = service;
            _apiKeyIndex = apiKeyIndex;
        }

        public Task<string> GenerateTitleAsync(string imagePath, string? customInstructions = null, CancellationToken cancellationToken = default)
        {
            if (_service is GeminiService gs)
            {
                return gs.GenerateTitleAsync(imagePath, customInstructions, _apiKeyIndex, cancellationToken);
            }
            return _service.GenerateTitleAsync(imagePath, customInstructions, cancellationToken);
        }

        public Task<Domain.Entities.SearchResult> SearchImageAsync(string imagePath, string searchDescription, CancellationToken cancellationToken = default)
        {
            if (_service is GeminiService gs)
            {
                return gs.SearchImageAsync(imagePath, searchDescription, _apiKeyIndex, cancellationToken);
            }
            return _service.SearchImageAsync(imagePath, searchDescription, cancellationToken);
        }

        public void SetApiKeys(string[] apiKeys) => _service.SetApiKeys(apiKeys);
        public string GetApiKeyForIndex(int index) => _service.GetApiKeyForIndex(index);
        public int ApiKeysCount => _service.ApiKeysCount;
        public int GetNextApiKeyIndex() => _service.GetNextApiKeyIndex();
    }
}
