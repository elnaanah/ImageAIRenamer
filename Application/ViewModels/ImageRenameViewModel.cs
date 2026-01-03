using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageAIRenamer.Application.Common;
using ImageAIRenamer.Domain.Entities;
using ImageAIRenamer.Domain.Interfaces;
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
        var usedNames = new Dictionary<string, int>();

        try
        {
            int processedCount = 0;
            foreach (var img in Images)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                img.Status = ImageStatusConstants.Renaming;
                ProgressText = $"جاري إعادة التسمية {processedCount + 1} من {Images.Count}...";

                var result = await _imageProcessingService.ProcessImageForRenameAsync(
                    img,
                    OutputFolder,
                    customInstructions,
                    usedNames,
                    _geminiService,
                    _fileService,
                    cancellationToken);

                img.Status = result.Status;
                
                if (result.Success)
                {
                    img.NewName = result.NewFileName ?? string.Empty;
                }

                processedCount++;
                ProgressValue++;
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
}
