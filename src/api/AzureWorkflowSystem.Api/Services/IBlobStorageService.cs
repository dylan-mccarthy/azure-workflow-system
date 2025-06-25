namespace AzureWorkflowSystem.Api.Services;

public interface IBlobStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
    Task DeleteFileAsync(string blobUrl);
    Task<Stream> DownloadFileAsync(string blobUrl);
}