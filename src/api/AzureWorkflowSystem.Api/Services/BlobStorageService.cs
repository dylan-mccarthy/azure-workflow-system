using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AzureWorkflowSystem.Api.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(BlobServiceClient blobServiceClient, IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _containerName = configuration["BlobStorage:ContainerName"] ?? "attachments";
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            
            // Ensure container exists
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            // Generate unique blob name to avoid conflicts
            var blobName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            // Upload file with metadata
            var uploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                },
                Metadata = new Dictionary<string, string>
                {
                    ["OriginalFileName"] = fileName,
                    ["UploadDate"] = DateTime.UtcNow.ToString("O")
                }
            };

            await blobClient.UploadAsync(fileStream, uploadOptions);
            
            _logger.LogInformation("Successfully uploaded file {FileName} as blob {BlobName}", fileName, blobName);
            
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {FileName} to blob storage", fileName);
            throw;
        }
    }

    public async Task DeleteFileAsync(string blobUrl)
    {
        try
        {
            var blobClient = new BlobClient(new Uri(blobUrl));
            await blobClient.DeleteIfExistsAsync();
            
            _logger.LogInformation("Successfully deleted blob {BlobUrl}", blobUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete blob {BlobUrl}", blobUrl);
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(string blobUrl)
    {
        try
        {
            var blobClient = new BlobClient(new Uri(blobUrl));
            var response = await blobClient.DownloadStreamingAsync();
            
            _logger.LogInformation("Successfully downloaded blob {BlobUrl}", blobUrl);
            
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download blob {BlobUrl}", blobUrl);
            throw;
        }
    }
}