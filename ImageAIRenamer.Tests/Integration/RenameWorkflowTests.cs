using System.IO;
using ImageAIRenamer.Application.ViewModels;
using ImageAIRenamer.Domain.Entities;
using ImageAIRenamer.Infrastructure.Services;
using ImageAIRenamer.Tests.Mocks;
using Moq;

namespace ImageAIRenamer.Tests.Integration;

/// <summary>
/// Integration tests for the rename workflow
/// Tests the interaction between FileService and ViewModels
/// </summary>
public class RenameWorkflowTests
{
    #region FileService Integration Tests

    [Fact]
    public async Task FileService_SanitizeAndCopy_WorksCorrectly()
    {
        // Arrange
        var fileService = new FileService();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create source file
            var sourcePath = Path.Combine(tempDir, "source.txt");
            await File.WriteAllTextAsync(sourcePath, "test content");

            // Act
            var sanitizedName = fileService.SanitizeFilename("Test File @#$ Name");
            var destPath = fileService.EnsureUniqueFilename(tempDir, sanitizedName, ".txt");
            await fileService.CopyFileAsync(sourcePath, destPath);

            // Assert
            Assert.True(File.Exists(destPath));
            Assert.Equal("Test_File_Name", sanitizedName);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task FileService_LoadAndFilter_WorksCorrectly()
    {
        // Arrange
        var fileService = new FileService();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create test files
            await File.WriteAllTextAsync(Path.Combine(tempDir, "image1.jpg"), "");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "image2.png"), "");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "document.pdf"), "");
            await File.WriteAllTextAsync(Path.Combine(tempDir, "image3.webp"), "");

            var extensions = new[] { ".jpg", ".png", ".webp" };

            // Act
            var files = (await fileService.LoadImageFilesAsync(tempDir, extensions)).ToList();

            // Assert
            Assert.Equal(3, files.Count);
            Assert.DoesNotContain(files, f => f.EndsWith(".pdf"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    #endregion

    #region ViewModel with Real FileService Tests

    [Fact]
    public void ViewModel_WithRealFileService_InitializesCorrectly()
    {
        var navMock = MockServices.CreateNavigationService();
        var geminiMock = MockServices.CreateGeminiService();
        var configMock = MockServices.CreateConfigurationService();
        var imageProcessingMock = MockServices.CreateImageProcessingService();
        var loggerMock = MockServices.CreateLogger<ImageRenameViewModel>();
        var fileService = new FileService();

        var viewModel = new ImageRenameViewModel(
            navMock.Object,
            geminiMock.Object,
            fileService,
            configMock.Object,
            imageProcessingMock.Object,
            loggerMock.Object
        );

        Assert.NotNull(viewModel);
        Assert.Empty(viewModel.Images);
    }

    #endregion

    #region End-to-End Workflow Simulation

    [Fact]
    public async Task Workflow_SanitizeMultipleFilenames_HandlesCollisions()
    {
        // Arrange
        var fileService = new FileService();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Simulate multiple images with similar names
            var names = new[] { "Test Image", "Test Image", "Test Image" };
            var usedNames = new Dictionary<string, int>();

            foreach (var name in names)
            {
                var sanitized = fileService.SanitizeFilename(name);
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

                var path = fileService.EnsureUniqueFilename(tempDir, sanitized, ".jpg");
                await File.WriteAllTextAsync(path, "");
            }

            // Assert - should have 3 different files
            var files = Directory.GetFiles(tempDir);
            Assert.Equal(3, files.Length);
            Assert.Contains(files, f => Path.GetFileName(f) == "Test_Image.jpg");
            Assert.Contains(files, f => Path.GetFileName(f) == "Test_Image_1.jpg");
            Assert.Contains(files, f => Path.GetFileName(f) == "Test_Image_2.jpg");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task Workflow_CopyWithSubdirectory_CreatesDirectory()
    {
        // Arrange
        var fileService = new FileService();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var sourcePath = Path.Combine(tempDir, "source.txt");
            await File.WriteAllTextAsync(sourcePath, "content");

            var outputSubDir = Path.Combine(tempDir, "output", "subdir");
            var destPath = Path.Combine(outputSubDir, "dest.txt");

            // Act
            await fileService.CopyFileAsync(sourcePath, destPath);

            // Assert
            Assert.True(Directory.Exists(outputSubDir));
            Assert.True(File.Exists(destPath));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    #endregion
}
