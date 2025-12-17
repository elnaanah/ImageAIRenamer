namespace ImageAIRenamer.Domain.Interfaces;

/// <summary>
/// Service interface for file operations
/// </summary>
public interface IFileService
{
    /// <summary>
    /// Sanitizes a filename by removing invalid characters
    /// </summary>
    /// <param name="name">Original filename</param>
    /// <returns>Sanitized filename</returns>
    string SanitizeFilename(string name);

    /// <summary>
    /// Loads image files from a directory
    /// </summary>
    /// <param name="folderPath">Path to the folder</param>
    /// <param name="supportedExtensions">Array of supported file extensions</param>
    /// <returns>List of image file paths</returns>
    Task<IEnumerable<string>> LoadImageFilesAsync(string folderPath, string[] supportedExtensions);

    /// <summary>
    /// Ensures a unique filename in the target directory
    /// </summary>
    /// <param name="directory">Target directory</param>
    /// <param name="baseName">Base filename without extension</param>
    /// <param name="extension">File extension</param>
    /// <returns>Unique file path</returns>
    string EnsureUniqueFilename(string directory, string baseName, string extension);

    /// <summary>
    /// Copies a file to a destination
    /// </summary>
    /// <param name="sourcePath">Source file path</param>
    /// <param name="destinationPath">Destination file path</param>
    /// <param name="overwrite">Whether to overwrite if file exists</param>
    Task CopyFileAsync(string sourcePath, string destinationPath, bool overwrite = false);
}
