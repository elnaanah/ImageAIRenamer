using ImageAIRenamer.Domain.Entities;

namespace ImageAIRenamer.Domain.Interfaces;

public interface IImageProcessingService
{
    string GenerateUniqueFileName(string baseName, string extension, Dictionary<string, int> usedNames);
    
    Task<ProcessRenameResult> ProcessImageForRenameAsync(
        ImageItem imageItem,
        string outputFolder,
        string? customInstructions,
        Dictionary<string, int> usedNames,
        IGeminiService geminiService,
        IFileService fileService,
        CancellationToken cancellationToken);
    
    Task<ProcessSearchResult> ProcessImageForSearchAsync(
        ImageItem imageItem,
        string searchDescription,
        string outputFolder,
        Dictionary<string, int> usedNames,
        IGeminiService geminiService,
        IFileService fileService,
        CancellationToken cancellationToken);
}

public class ProcessRenameResult
{
    public bool Success { get; set; }
    public string? NewFileName { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ProcessSearchResult
{
    public bool IsMatch { get; set; }
    public string? NewFileName { get; set; }
    public string Status { get; set; } = string.Empty;
}

