using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgenticShopper.Core.Services;

/// <summary>
/// Azure Blob Storage service implementation
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BlobStorageService> _logger;
    private readonly string _connectionString;
    private readonly string _defaultContainer;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _connectionString = configuration["AzureStorage:ConnectionString"] ?? string.Empty;
        _defaultContainer = configuration["AzureStorage:ContainerName"] ?? "receipts";
    }

    public async Task<string> UploadAsync(string fileName, Stream content, string containerName = "receipts")
    {
        try
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                _logger.LogWarning("Azure Storage connection string not configured. Using mock URL.");
                return $"https://mockstore.blob.core.windows.net/{containerName}/{fileName}";
            }

            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            var blobClient = containerClient.GetBlobClient(fileName);
            
            // Set content type based on file extension
            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = GetContentType(fileName)
            };
            
            await blobClient.UploadAsync(content, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            _logger.LogInformation("Uploaded blob: {FileName} to container: {Container}", fileName, containerName);
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload blob: {FileName}", fileName);
            throw;
        }
    }

    public async Task<Stream> DownloadAsync(string blobUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                _logger.LogWarning("Azure Storage connection string not configured. Returning empty stream.");
                return new MemoryStream();
            }

            var blobClient = new BlobClient(new Uri(blobUrl), new Azure.Storage.StorageSharedKeyCredential(
                GetAccountName(_connectionString), GetAccountKey(_connectionString)));
            var response = await blobClient.DownloadStreamingAsync();
            
            // Copy to memory stream to return seekable stream
            var memoryStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            _logger.LogInformation("Downloaded blob: {BlobUrl}", blobUrl);
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download blob: {BlobUrl}", blobUrl);
            throw;
        }
    }

    public async Task DeleteAsync(string blobUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                _logger.LogWarning("Azure Storage connection string not configured.");
                return;
            }

            var blobClient = new BlobClient(new Uri(blobUrl), new Azure.Storage.StorageSharedKeyCredential(
                GetAccountName(_connectionString), GetAccountKey(_connectionString)));
            await blobClient.DeleteIfExistsAsync();

            _logger.LogInformation("Deleted blob: {BlobUrl}", blobUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blob: {BlobUrl}", blobUrl);
            throw;
        }
    }

    public async Task<string> GetSasUrlAsync(string blobUrl, int expiryMinutes = 60)
    {
        try
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                _logger.LogWarning("Azure Storage connection string not configured. Returning original URL.");
                return blobUrl;
            }

            var blobClient = new BlobClient(new Uri(blobUrl), new Azure.Storage.StorageSharedKeyCredential(
                GetAccountName(_connectionString), GetAccountKey(_connectionString)));
                
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            
            var sasUri = blobClient.GenerateSasUri(sasBuilder);

            _logger.LogInformation("Generated SAS URL for blob: {BlobUrl}", blobUrl);
            await Task.CompletedTask;
            return sasUri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SAS URL for blob: {BlobUrl}", blobUrl);
            throw;
        }
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private string GetAccountName(string connectionString)
    {
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            if (part.StartsWith("AccountName=", StringComparison.OrdinalIgnoreCase))
            {
                return part.Substring("AccountName=".Length);
            }
        }
        throw new InvalidOperationException("AccountName not found in connection string");
    }

    private string GetAccountKey(string connectionString)
    {
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            if (part.StartsWith("AccountKey=", StringComparison.OrdinalIgnoreCase))
            {
                return part.Substring("AccountKey=".Length);
            }
        }
        throw new InvalidOperationException("AccountKey not found in connection string");
    }
}
