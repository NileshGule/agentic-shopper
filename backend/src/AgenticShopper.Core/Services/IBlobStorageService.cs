using System.IO;
using System.Threading.Tasks;

namespace AgenticShopper.Core.Services;

/// <summary>
/// Interface for blob storage operations
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Upload a file to blob storage
    /// </summary>
    /// <param name="fileName">Name of the file</param>
    /// <param name="content">File content stream</param>
    /// <param name="containerName">Container name (default: receipts)</param>
    /// <returns>URL of the uploaded blob</returns>
    Task<string> UploadAsync(string fileName, Stream content, string containerName = "receipts");

    /// <summary>
    /// Download a file from blob storage
    /// </summary>
    /// <param name="blobUrl">URL of the blob</param>
    /// <returns>File content stream</returns>
    Task<Stream> DownloadAsync(string blobUrl);

    /// <summary>
    /// Delete a file from blob storage
    /// </summary>
    /// <param name="blobUrl">URL of the blob</param>
    Task DeleteAsync(string blobUrl);

    /// <summary>
    /// Get a SAS URL for temporary access to a blob
    /// </summary>
    /// <param name="blobUrl">URL of the blob</param>
    /// <param name="expiryMinutes">Expiry time in minutes</param>
    /// <returns>SAS URL</returns>
    Task<string> GetSasUrlAsync(string blobUrl, int expiryMinutes = 60);
}
