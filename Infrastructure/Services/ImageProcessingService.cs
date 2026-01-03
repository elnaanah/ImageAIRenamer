using System.IO;
using ImageAIRenamer.Application.Common;
using ImageAIRenamer.Domain.Entities;
using ImageAIRenamer.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ImageAIRenamer.Infrastructure.Services;

public class ImageProcessingService : IImageProcessingService
{
    private readonly ILogger<ImageProcessingService> _logger;

    public ImageProcessingService(ILogger<ImageProcessingService> logger)
    {
        _logger = logger;
    }

    public string GenerateUniqueFileName(string baseName, string extension, Dictionary<string, int> usedNames)
    {
        var sanitized = baseName;
        int counter = 0;

        if (usedNames.ContainsKey(sanitized))
        {
            counter = usedNames[sanitized];
        }
        usedNames[sanitized] = counter + 1;

        if (counter > 0)
        {
            sanitized = $"{baseName}_{counter}";
        }

        return sanitized;
    }

    public async Task<ProcessRenameResult> ProcessImageForRenameAsync(
        ImageItem imageItem,
        string outputFolder,
        string? customInstructions,
        Dictionary<string, int> usedNames,
        IGeminiService geminiService,
        IFileService fileService,
        CancellationToken cancellationToken)
    {
        try
        {
            var title = await geminiService.GenerateTitleAsync(imageItem.FilePath, customInstructions, cancellationToken);
            var sanitized = fileService.SanitizeFilename(title);
            var baseName = sanitized;
            var ext = Path.GetExtension(imageItem.FilePath);

            var uniqueBaseName = GenerateUniqueFileName(baseName, ext, usedNames);
            var uniquePath = fileService.EnsureUniqueFilename(outputFolder, uniqueBaseName, ext);
            var newFileName = Path.GetFileName(uniquePath);

            await fileService.CopyFileAsync(imageItem.FilePath, uniquePath, true);

            _logger.LogDebug("Successfully processed image for rename: {FilePath} -> {NewFileName}", imageItem.FilePath, newFileName);

            return new ProcessRenameResult
            {
                Success = true,
                NewFileName = newFileName,
                Status = ImageStatusConstants.Completed
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Image processing cancelled: {FilePath}", imageItem.FilePath);
            return new ProcessRenameResult
            {
                Success = false,
                Status = ImageStatusConstants.Cancelled
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image for rename: {FilePath}", imageItem.FilePath);
            return new ProcessRenameResult
            {
                Success = false,
                Status = ImageStatusConstants.Error
            };
        }
    }

    public async Task<ProcessSearchResult> ProcessImageForSearchAsync(
        ImageItem imageItem,
        string searchDescription,
        string outputFolder,
        Dictionary<string, int> usedNames,
        IGeminiService geminiService,
        IFileService fileService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await geminiService.SearchImageAsync(imageItem.FilePath, searchDescription, cancellationToken);

            if (result.IsMatch)
            {
                string? suggestedName = result.SuggestedName;

                if (string.IsNullOrWhiteSpace(suggestedName))
                {
                    suggestedName = await geminiService.GenerateTitleAsync(imageItem.FilePath, cancellationToken: cancellationToken);
                }

                var sanitized = fileService.SanitizeFilename(suggestedName ?? "صورة");
                var baseName = sanitized;
                var ext = Path.GetExtension(imageItem.FilePath);

                var uniqueBaseName = GenerateUniqueFileName(baseName, ext, usedNames);
                var uniquePath = fileService.EnsureUniqueFilename(outputFolder, uniqueBaseName, ext);
                var newFileName = Path.GetFileName(uniquePath);

                _logger.LogDebug("Image matched search criteria: {FilePath} -> {NewFileName}", imageItem.FilePath, newFileName);

                return new ProcessSearchResult
                {
                    IsMatch = true,
                    NewFileName = newFileName,
                    Status = ImageStatusConstants.Matched
                };
            }
            else
            {
                return new ProcessSearchResult
                {
                    IsMatch = false,
                    Status = ImageStatusConstants.NotMatched
                };
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Image search cancelled: {FilePath}", imageItem.FilePath);
            return new ProcessSearchResult
            {
                IsMatch = false,
                Status = ImageStatusConstants.Cancelled
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing image for search: {FilePath}", imageItem.FilePath);
            return new ProcessSearchResult
            {
                IsMatch = false,
                Status = ImageStatusConstants.Error
            };
        }
    }
}

