using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageAIRenamer.Application.Common;
using ImageAIRenamer.Domain.Entities;
using ImageAIRenamer.Domain.Interfaces;
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
        var usedNames = new Dictionary<string, int>();
        int processedCount = 0;
        int matchedCount = 0;

        try
        {
            foreach (var img in MatchedImages)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                img.Status = ImageStatusConstants.Processing;
                ProgressText = $"جاري المعالجة {processedCount + 1} من {MatchedImages.Count}...";

                var result = await _imageProcessingService.ProcessImageForSearchAsync(
                    img,
                    searchDescription,
                    OutputFolder,
                    usedNames,
                    _geminiService,
                    _fileService,
                    cancellationToken);

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
            }
        }
        finally
        {
            IsSearchEnabled = true;
            ProgressText = $"مطابق: {matchedCount} / {MatchedImages.Count}";
            var message = string.Format(SuccessMessages.SearchCompleted, matchedCount, MatchedImages.Count);
            EndProcessing(message);
            _logger.LogInformation("Search completed. Found {MatchedCount} matches out of {TotalCount}", matchedCount, MatchedImages.Count);
        }
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
