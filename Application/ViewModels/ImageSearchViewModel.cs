using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageAIRenamer.Application.Common;
using ImageAIRenamer.Domain.Entities;
using ImageAIRenamer.Domain.Interfaces;
using ImageAIRenamer.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace ImageAIRenamer.Application.ViewModels;

public partial class ImageSearchViewModel : ImageProcessingViewModelBase
{
    private readonly IGeminiService _geminiService;
    private readonly IImageProcessingService _imageProcessingService;


    [ObservableProperty]
    private ObservableCollection<ImageItem> matchedImages = new();

    [ObservableProperty]
    private string searchDescription = string.Empty;

    [ObservableProperty]
    private string progressText = string.Empty;

    [ObservableProperty]
    private bool isSearchEnabled = false;

    private readonly IAsyncRelayCommand _searchCommand;

    public ImageSearchViewModel(
        INavigationService navigationService,
        IGeminiService geminiService,
        IFileService fileService,
        IConfigurationService configurationService,
        IImageProcessingService imageProcessingService,
        ILogger<ImageSearchViewModel> logger)
        : base(navigationService, fileService, configurationService, logger)
    {
        _geminiService = geminiService;
        _imageProcessingService = imageProcessingService;
        
        _searchCommand = new AsyncRelayCommand(SearchImagesAsync, () => IsSearchEnabled);
    }

    public IAsyncRelayCommand SearchCommand => _searchCommand;

    protected override IAsyncRelayCommand? MainCommand => _searchCommand;

    protected override void OnCancelOperation()
    {
        IsSearchEnabled = true;
    }

    protected override void ClearAllData()
    {
        base.ClearAllData();
        SearchDescription = string.Empty;
        ProgressText = string.Empty;
    }

    protected override void OnClearList()
    {
        OnReadyStateChanged();
    }

    public IRelayCommand CopySelectedCommand => new RelayCommand(() => _ = CopySelectedImagesAsync());

    protected override ObservableCollection<ImageItem> ImagesCollection => MatchedImages;

    protected override bool ShouldSetSelectedOnLoad() => true;

    protected override void OnReadyStateChanged()
    {
        IsSearchEnabled = MatchedImages.Count > 0
            && !string.IsNullOrWhiteSpace(OutputFolder)
            && !string.IsNullOrWhiteSpace(SearchDescription);
        NotifyCommands();
    }

    partial void OnSearchDescriptionChanged(string value)
    {
        OnReadyStateChanged();
    }

    private async Task SearchImagesAsync()
    {
        var apiKeysArray = ValidateAndSetupApiKeys();
        if (apiKeysArray.Length == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(SearchDescription))
        {
            ShowError(ErrorMessages.NoSearchDescription, "خطأ");
            return;
        }

        await _configurationService.SaveApiKeysAsync(apiKeysArray);

        if (!EnsureOutputFolderExists())
        {
            return;
        }

        _geminiService.SetApiKeys(apiKeysArray);

        IsSearchEnabled = false;
        StartProcessing(MatchedImages.Count);
        var cancellationToken = _cancellationTokenSource!.Token;

        var searchDescription = SearchDescription;

        try
        {
            int matchedCount;
            if (EnableSpeedBoost && apiKeysArray.Length >= 2)
            {
                matchedCount = await SearchImagesParallelAsync(apiKeysArray, searchDescription, cancellationToken);
            }
            else
            {
                matchedCount = await SearchImagesSequentialAsync(searchDescription, cancellationToken);
            }

            ProgressText = $"مطابق: {matchedCount} / {MatchedImages.Count}";
            var message = string.Format(SuccessMessages.SearchCompleted, matchedCount, MatchedImages.Count);
            EndProcessing(message);
            _logger.LogInformation("Search completed. Found {MatchedCount} matches out of {TotalCount}", matchedCount, MatchedImages.Count);
        }
        finally
        {
            IsSearchEnabled = true;
        }
    }

    private async Task<int> SearchImagesSequentialAsync(string searchDescription, CancellationToken cancellationToken)
    {
        var usedNames = new Dictionary<string, int>();
        int processedCount = 0;
        int matchedCount = 0;

        foreach (var img in MatchedImages)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                img.Status = ImageStatusConstants.Processing;
                ProgressText = $"جاري المعالجة {processedCount + 1} من {MatchedImages.Count}...";
            });

            var result = await _imageProcessingService.ProcessImageForSearchAsync(
                img,
                searchDescription,
                OutputFolder,
                usedNames,
                _geminiService,
                _fileService,
                cancellationToken);

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                img.Status = result.Status;
                if (result.IsMatch)
                {
                    matchedCount++;
                    img.NewName = result.NewFileName ?? string.Empty;
                }
                else
                {
                    img.IsSelected = false;
                }
                processedCount++;
                ProgressValue = processedCount;
            });
        }

        return matchedCount;
    }

    private async Task<int> SearchImagesParallelAsync(string[] apiKeysArray, string searchDescription, CancellationToken cancellationToken)
    {
        var usedNames = new ConcurrentDictionary<string, int>();
        var semaphore = new SemaphoreSlim(apiKeysArray.Length, apiKeysArray.Length);
        int processedCount = 0;
        int matchedCount = 0;
        var tasks = new List<Task>();

        foreach (var img in MatchedImages)
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
                        image.Status = ImageStatusConstants.Processing;
                    });

                    var result = await _imageProcessingService.ProcessImageForSearchAsync(
                        image,
                        searchDescription,
                        OutputFolder,
                        usedNames,
                        geminiServiceWrapper,
                        _fileService,
                        cancellationToken);

                    var currentCount = Interlocked.Increment(ref processedCount);
                    bool isMatch = result.IsMatch;
                    if (isMatch)
                    {
                        Interlocked.Increment(ref matchedCount);
                    }

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        image.Status = result.Status;
                        if (isMatch)
                        {
                            image.NewName = result.NewFileName ?? string.Empty;
                        }
                        else
                        {
                            image.IsSelected = false;
                        }
                        ProgressText = $"جاري المعالجة {currentCount} من {MatchedImages.Count}...";
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
                        ProgressText = $"جاري المعالجة {currentCount} من {MatchedImages.Count}...";
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
                        ProgressText = $"جاري المعالجة {currentCount} من {MatchedImages.Count}...";
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
        return matchedCount;
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
            if (_service is Infrastructure.Services.GeminiService gs)
            {
                return gs.GenerateTitleAsync(imagePath, customInstructions, _apiKeyIndex, cancellationToken);
            }
            return _service.GenerateTitleAsync(imagePath, customInstructions, cancellationToken);
        }

        public Task<Domain.Entities.SearchResult> SearchImageAsync(string imagePath, string searchDescription, CancellationToken cancellationToken = default)
        {
            if (_service is Infrastructure.Services.GeminiService gs)
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

    private async Task CopySelectedImagesAsync()
    {
        if (!Directory.Exists(OutputFolder))
        {
            ShowError(ErrorMessages.NoOutputFolder, "خطأ");
            return;
        }

        var selectedImages = MatchedImages.Where(i => i.IsSelected && i.Status == ImageStatusConstants.Matched).ToList();

        if (selectedImages.Count == 0)
        {
            ShowWarning(ErrorMessages.NoImagesSelected, "تنبيه");
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

                var uniquePath = _fileService.EnsureUniqueFilename(OutputFolder, Path.GetFileNameWithoutExtension(fileName), ext);

                await _fileService.CopyFileAsync(img.FilePath, uniquePath, true);
                img.NewName = Path.GetFileName(uniquePath);
                img.Status = ImageStatusConstants.Copied;
                copiedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to copy image: {FilePath}", img.FilePath);
                img.Status = ImageStatusConstants.CopyError;
                skippedCount++;
            }
        }

        string message = copiedCount > 0
            ? string.Format(SuccessMessages.ImagesCopied, copiedCount)
            : SuccessMessages.NoImagesCopied;

        if (skippedCount > 0)
        {
            message += $"\n{string.Format(SuccessMessages.CopyFailed, skippedCount)}";
        }

        if (copiedCount > 0)
        {
            ShowInfo(message, "نجح");
            _logger.LogInformation("Copied {Count} images successfully", copiedCount);
        }
        else
        {
            ShowWarning(message, "تنبيه");
        }
    }
}
