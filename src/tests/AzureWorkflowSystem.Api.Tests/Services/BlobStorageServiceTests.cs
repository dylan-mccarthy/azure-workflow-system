using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AzureWorkflowSystem.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AzureWorkflowSystem.Api.Tests.Services;

public class BlobStorageServiceTests
{
    private readonly Mock<BlobServiceClient> _mockBlobServiceClient;
    private readonly Mock<BlobContainerClient> _mockContainerClient;
    private readonly Mock<BlobClient> _mockBlobClient;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<BlobStorageService>> _mockLogger;
    private readonly BlobStorageService _blobStorageService;

    public BlobStorageServiceTests()
    {
        _mockBlobServiceClient = new Mock<BlobServiceClient>();
        _mockContainerClient = new Mock<BlobContainerClient>();
        _mockBlobClient = new Mock<BlobClient>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<BlobStorageService>>();

        _mockConfiguration.Setup(c => c["BlobStorage:ContainerName"]).Returns("test-attachments");
        _mockBlobServiceClient.Setup(b => b.GetBlobContainerClient("test-attachments")).Returns(_mockContainerClient.Object);

        _blobStorageService = new BlobStorageService(_mockBlobServiceClient.Object, _mockConfiguration.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task UploadFileAsync_WithValidFile_ReturnsExpectedUrl()
    {
        // Arrange
        var fileName = "test-file.pdf";
        var contentType = "application/pdf";
        var fileContent = "test file content";
        var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        var expectedUrl = "https://test.blob.core.windows.net/test-attachments/guid_test-file.pdf";

        _mockContainerClient.Setup(c => c.CreateIfNotExistsAsync(PublicAccessType.None, null, null, default))
            .Returns(Task.FromResult(Response.FromValue(Mock.Of<BlobContainerInfo>(), Mock.Of<Response>())));

        _mockContainerClient.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(_mockBlobClient.Object);
        _mockBlobClient.Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), default))
            .Returns(Task.FromResult(Response.FromValue(Mock.Of<BlobContentInfo>(), Mock.Of<Response>())));
        _mockBlobClient.Setup(b => b.Uri).Returns(new Uri(expectedUrl));

        // Act
        var result = await _blobStorageService.UploadFileAsync(fileStream, fileName, contentType);

        // Assert
        Assert.Equal(expectedUrl, result);
        _mockContainerClient.Verify(c => c.CreateIfNotExistsAsync(PublicAccessType.None, null, null, default), Times.Once);
        _mockBlobClient.Verify(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), default), Times.Once);
    }

    [Fact]
    public async Task UploadFileAsync_WithException_ThrowsException()
    {
        // Arrange
        var fileName = "test-file.pdf";
        var contentType = "application/pdf";
        var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test"));
        var expectedException = new RequestFailedException("Upload failed");

        _mockContainerClient.Setup(c => c.CreateIfNotExistsAsync(PublicAccessType.None, null, null, default))
            .Returns(Task.FromResult(Response.FromValue(Mock.Of<BlobContainerInfo>(), Mock.Of<Response>())));
        _mockContainerClient.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(_mockBlobClient.Object);
        _mockBlobClient.Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), default))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RequestFailedException>(
            () => _blobStorageService.UploadFileAsync(fileStream, fileName, contentType));
        Assert.Equal("Upload failed", exception.Message);
    }

    [Fact]
    public void BlobStorageService_Constructor_InitializesCorrectly()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["BlobStorage:ContainerName"]).Returns("test-container");
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var loggerMock = new Mock<ILogger<BlobStorageService>>();

        // Act
        var service = new BlobStorageService(mockBlobServiceClient.Object, mockConfig.Object, loggerMock.Object);

        // Assert
        Assert.NotNull(service);
        mockConfig.Verify(c => c["BlobStorage:ContainerName"], Times.Once);
    }

    [Fact]
    public void BlobStorageService_Constructor_UsesDefaultContainerName()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["BlobStorage:ContainerName"]).Returns((string?)null);
        var mockBlobServiceClient = new Mock<BlobServiceClient>();
        var loggerMock = new Mock<ILogger<BlobStorageService>>();

        // Act
        var service = new BlobStorageService(mockBlobServiceClient.Object, mockConfig.Object, loggerMock.Object);

        // Assert
        Assert.NotNull(service);
        mockConfig.Verify(c => c["BlobStorage:ContainerName"], Times.Once);
    }

    [Fact]
    public async Task DeleteFileAsync_WithInvalidUrl_ThrowsException()
    {
        // Arrange
        var blobUrl = "invalid-url";

        // Act & Assert
        await Assert.ThrowsAsync<UriFormatException>(
            () => _blobStorageService.DeleteFileAsync(blobUrl));
    }

    [Fact]
    public async Task DownloadFileAsync_WithInvalidUrl_ThrowsException()
    {
        // Arrange
        var blobUrl = "invalid-url";

        // Act & Assert
        await Assert.ThrowsAsync<UriFormatException>(
            () => _blobStorageService.DownloadFileAsync(blobUrl));
    }
}