using System.IO;
using System.Text.RegularExpressions;
using ImageAIRenamer.Domain.Interfaces;

namespace ImageAIRenamer.Infrastructure.Services;

/// <summary>
/// Service for file operations including sanitization and I/O
/// </summary>
public class FileService : IFileService
{
    /// <inheritdoc/>
    public string SanitizeFilename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "صورة";

        // Replace whitespace with underscores
        var sanitized = Regex.Replace(name, @"\s+", "_");
        
        // Remove invalid characters, keep only letters, numbers, and underscores
        sanitized = Regex.Replace(sanitized, @"[^\p{L}\p{N}_]", "");
        
        // Replace multiple underscores with single underscore
        sanitized = Regex.Replace(sanitized, @"_+", "_");
        
        // Trim underscores from start and end
        sanitized = sanitized.Trim('_');
        
        // If empty after sanitization, return default
        return string.IsNullOrWhiteSpace(sanitized) ? "صورة" : sanitized;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<string>> LoadImageFilesAsync(string folderPath, string[] supportedExtensions)
    {
        if (!Directory.Exists(folderPath))
            return Enumerable.Empty<string>();

        var files = Directory.GetFiles(folderPath);
        var imageFiles = files.Where(file =>
        {
            var ext = Path.GetExtension(file).ToLower();
            return supportedExtensions.Contains(ext);
        });

        return await Task.FromResult(imageFiles);
    }

    /// <inheritdoc/>
    public string EnsureUniqueFilename(string directory, string baseName, string extension)
    {
        var fileName = baseName + extension;
        var fullPath = Path.Combine(directory, fileName);
        
        if (!File.Exists(fullPath))
            return fullPath;

        int counter = 1;
        string uniquePath;
        do
        {
            var uniqueFileName = $"{baseName}_{counter}{extension}";
            uniquePath = Path.Combine(directory, uniqueFileName);
            counter++;
        } while (File.Exists(uniquePath));

        return uniquePath;
    }

    /// <inheritdoc/>
    public async Task CopyFileAsync(string sourcePath, string destinationPath, bool overwrite = false)
    {
        // Ensure destination directory exists
        var destinationDir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        await Task.Run(() => File.Copy(sourcePath, destinationPath, overwrite));
    }
}
