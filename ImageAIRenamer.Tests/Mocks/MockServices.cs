using System.IO;
using ImageAIRenamer.Domain.Entities;
using ImageAIRenamer.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace ImageAIRenamer.Tests.Mocks;

/// <summary>
/// Factory for creating mock services used in testing
/// </summary>
public static class MockServices
{
    /// <summary>
    /// Creates a mock IGeminiService
    /// </summary>
    public static Mock<IGeminiService> CreateGeminiService(string? defaultTitle = "TestTitle")
    {
        var mock = new Mock<IGeminiService>();
        
        mock.Setup(x => x.GenerateTitleAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultTitle ?? "TestTitle");

        mock.Setup(x => x.SearchImageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResult { IsMatch = true, Confidence = "0.9" });

        return mock;
    }

    /// <summary>
    /// Creates a mock IFileService
    /// </summary>
    public static Mock<IFileService> CreateFileService()
    {
        var mock = new Mock<IFileService>();

        mock.Setup(x => x.SanitizeFilename(It.IsAny<string>()))
            .Returns<string>(name => name?.Replace(" ", "_") ?? "صورة");

        mock.Setup(x => x.LoadImageFilesAsync(It.IsAny<string>(), It.IsAny<string[]>()))
            .ReturnsAsync(Enumerable.Empty<string>());

        mock.Setup(x => x.EnsureUniqueFilename(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns<string, string, string>((dir, name, ext) => Path.Combine(dir, name + ext));

        mock.Setup(x => x.CopyFileAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        return mock;
    }

    /// <summary>
    /// Creates a mock IConfigurationService
    /// </summary>
    public static Mock<IConfigurationService> CreateConfigurationService(string[]? apiKeys = null)
    {
        var mock = new Mock<IConfigurationService>();

        mock.Setup(x => x.GetApiKeysAsync())
            .ReturnsAsync(apiKeys ?? new[] { "test-api-key" });

        mock.Setup(x => x.SaveApiKeysAsync(It.IsAny<string[]>()))
            .Returns(Task.CompletedTask);

        mock.Setup(x => x.GetGeminiModel())
            .Returns("gemini-2.0-flash");

        mock.Setup(x => x.GetSupportedExtensions())
            .Returns(new[] { ".jpg", ".jpeg", ".png", ".webp" });

        return mock;
    }

    /// <summary>
    /// Creates a mock INavigationService
    /// </summary>
    public static Mock<INavigationService> CreateNavigationService()
    {
        var mock = new Mock<INavigationService>();
        return mock;
    }

    /// <summary>
    /// Creates a mock IImageProcessingService
    /// </summary>
    public static Mock<IImageProcessingService> CreateImageProcessingService()
    {
        var mock = new Mock<IImageProcessingService>();
        
        mock.Setup(x => x.GenerateUniqueFileName(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, int>>()))
            .Returns<string, string, Dictionary<string, int>>((name, ext, used) => name + ext);

        mock.Setup(x => x.ProcessImageForRenameAsync(
                It.IsAny<ImageItem>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<Dictionary<string, int>>(),
                It.IsAny<IGeminiService>(),
                It.IsAny<IFileService>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessRenameResult { Success = true, NewFileName = "renamed.jpg", Status = "تم" });

        mock.Setup(x => x.ProcessImageForSearchAsync(
                It.IsAny<ImageItem>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, int>>(),
                It.IsAny<IGeminiService>(),
                It.IsAny<IFileService>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessSearchResult { IsMatch = true, NewFileName = "matched.jpg", Status = "مطابق" });

        return mock;
    }

    public static Mock<ILogger<T>> CreateLogger<T>()
    {
        var mock = new Mock<ILogger<T>>();
        return mock;
    }
}
