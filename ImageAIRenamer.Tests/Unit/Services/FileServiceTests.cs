using System.IO;
using ImageAIRenamer.Infrastructure.Services;

namespace ImageAIRenamer.Tests.Unit.Services;

/// <summary>
/// Unit tests for FileService
/// </summary>
public class FileServiceTests
{
    private readonly FileService _fileService;

    public FileServiceTests()
    {
        _fileService = new FileService();
    }

    #region SanitizeFilename Tests

    [Fact]
    public void SanitizeFilename_WithValidName_ReturnsUnchanged()
    {
        // Arrange
        var input = "ValidFileName";

        // Act
        var result = _fileService.SanitizeFilename(input);

        // Assert
        Assert.Equal("ValidFileName", result);
    }

    [Fact]
    public void SanitizeFilename_WithSpaces_ReplacesWithUnderscores()
    {
        // Arrange
        var input = "File Name With Spaces";

        // Act
        var result = _fileService.SanitizeFilename(input);

        // Assert
        Assert.Equal("File_Name_With_Spaces", result);
    }

    [Fact]
    public void SanitizeFilename_WithSpecialCharacters_RemovesThem()
    {
        // Arrange
        var input = "File@Name#Test!";

        // Act
        var result = _fileService.SanitizeFilename(input);

        // Assert
        Assert.Equal("FileNameTest", result);
    }

    [Fact]
    public void SanitizeFilename_WithArabicText_PreservesIt()
    {
        // Arrange
        var input = "صورة_اختبار";

        // Act
        var result = _fileService.SanitizeFilename(input);

        // Assert
        Assert.Equal("صورة_اختبار", result);
    }

    [Fact]
    public void SanitizeFilename_WithEmptyString_ReturnsDefault()
    {
        // Arrange
        var input = "";

        // Act
        var result = _fileService.SanitizeFilename(input);

        // Assert
        Assert.Equal("صورة", result);
    }

    [Fact]
    public void SanitizeFilename_WithNullString_ReturnsDefault()
    {
        // Arrange
        string? input = null;

        // Act
        var result = _fileService.SanitizeFilename(input!);

        // Assert
        Assert.Equal("صورة", result);
    }

    [Fact]
    public void SanitizeFilename_WithOnlySpecialChars_ReturnsDefault()
    {
        // Arrange
        var input = "!@#$%^&*()";

        // Act
        var result = _fileService.SanitizeFilename(input);

        // Assert
        Assert.Equal("صورة", result);
    }

    [Fact]
    public void SanitizeFilename_WithMultipleUnderscores_NormalizesToSingle()
    {
        // Arrange
        var input = "File___Name__Test";

        // Act
        var result = _fileService.SanitizeFilename(input);

        // Assert
        Assert.Equal("File_Name_Test", result);
    }

    #endregion

    #region LoadImageFilesAsync Tests

    [Fact]
    public async Task LoadImageFilesAsync_WithNonExistentFolder_ReturnsEmpty()
    {
        // Arrange
        var folderPath = @"C:\NonExistentFolder12345";
        var extensions = new[] { ".jpg", ".png" };

        // Act
        var result = await _fileService.LoadImageFilesAsync(folderPath, extensions);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadImageFilesAsync_WithValidFolder_ReturnsMatchingFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create test files
            File.WriteAllText(Path.Combine(tempDir, "test1.jpg"), "");
            File.WriteAllText(Path.Combine(tempDir, "test2.png"), "");
            File.WriteAllText(Path.Combine(tempDir, "test3.txt"), ""); // Should not match

            var extensions = new[] { ".jpg", ".png" };

            // Act
            var result = (await _fileService.LoadImageFilesAsync(tempDir, extensions)).ToList();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, f => f.EndsWith(".jpg"));
            Assert.Contains(result, f => f.EndsWith(".png"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task LoadImageFilesAsync_WithEmptyFolder_ReturnsEmpty()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var extensions = new[] { ".jpg", ".png" };

            // Act
            var result = await _fileService.LoadImageFilesAsync(tempDir, extensions);

            // Assert
            Assert.Empty(result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    #endregion

    #region EnsureUniqueFilename Tests

    [Fact]
    public void EnsureUniqueFilename_WhenFileDoesNotExist_ReturnsOriginalPath()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = _fileService.EnsureUniqueFilename(tempDir, "test", ".jpg");

            // Assert
            Assert.Equal(Path.Combine(tempDir, "test.jpg"), result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void EnsureUniqueFilename_WhenFileExists_ReturnsIncrementedPath()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create existing file
            File.WriteAllText(Path.Combine(tempDir, "test.jpg"), "");

            // Act
            var result = _fileService.EnsureUniqueFilename(tempDir, "test", ".jpg");

            // Assert
            Assert.Equal(Path.Combine(tempDir, "test_1.jpg"), result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void EnsureUniqueFilename_WhenMultipleFilesExist_ReturnsNextAvailable()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Create existing files
            File.WriteAllText(Path.Combine(tempDir, "test.jpg"), "");
            File.WriteAllText(Path.Combine(tempDir, "test_1.jpg"), "");
            File.WriteAllText(Path.Combine(tempDir, "test_2.jpg"), "");

            // Act
            var result = _fileService.EnsureUniqueFilename(tempDir, "test", ".jpg");

            // Assert
            Assert.Equal(Path.Combine(tempDir, "test_3.jpg"), result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    #endregion

    #region CopyFileAsync Tests

    [Fact]
    public async Task CopyFileAsync_CopiesFileSuccessfully()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var sourcePath = Path.Combine(tempDir, "source.txt");
            var destPath = Path.Combine(tempDir, "dest.txt");
            await File.WriteAllTextAsync(sourcePath, "test content");

            // Act
            await _fileService.CopyFileAsync(sourcePath, destPath);

            // Assert
            Assert.True(File.Exists(destPath));
            Assert.Equal("test content", await File.ReadAllTextAsync(destPath));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task CopyFileAsync_CreatesDestinationDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var sourcePath = Path.Combine(tempDir, "source.txt");
            var destDir = Path.Combine(tempDir, "subdir");
            var destPath = Path.Combine(destDir, "dest.txt");
            await File.WriteAllTextAsync(sourcePath, "test content");

            // Act
            await _fileService.CopyFileAsync(sourcePath, destPath);

            // Assert
            Assert.True(Directory.Exists(destDir));
            Assert.True(File.Exists(destPath));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    #endregion
}
