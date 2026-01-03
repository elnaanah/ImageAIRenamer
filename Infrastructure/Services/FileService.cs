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

    public async Task<IEnumerable<string>> LoadImageFilesAsync(string folderPath, string[] supportedExtensions)
    {
        if (!Directory.Exists(folderPath))
            return Enumerable.Empty<string>();

        return await Task.Run(() =>
        {
            var files = Directory.GetFiles(folderPath);
            return files.Where(file =>
            {
                var ext = Path.GetExtension(file).ToLower();
                return supportedExtensions.Contains(ext);
            });
        });
    }

    public string EnsureUniqueFilename(string directory, string baseName, string extension)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Directory cannot be null or empty", nameof(directory));
        
        if (string.IsNullOrWhiteSpace(baseName))
            throw new ArgumentException("Base name cannot be null or empty", nameof(baseName));

        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException($"Directory not found: {directory}");

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
            
            if (counter > 10000)
                throw new InvalidOperationException("Unable to generate unique filename after 10000 attempts");
        } while (File.Exists(uniquePath));

        return uniquePath;
    }

    public async Task CopyFileAsync(string sourcePath, string destinationPath, bool overwrite = false)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("Source path cannot be null or empty", nameof(sourcePath));
        
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new ArgumentException("Destination path cannot be null or empty", nameof(destinationPath));

        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Source file not found", sourcePath);

        var destinationDir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destinationDir) && !Directory.Exists(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }

        await Task.Run(() => File.Copy(sourcePath, destinationPath, overwrite));
    }
}
