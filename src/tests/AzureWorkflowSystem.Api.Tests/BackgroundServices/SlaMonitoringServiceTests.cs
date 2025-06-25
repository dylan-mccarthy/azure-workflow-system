using AzureWorkflowSystem.Api.BackgroundServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AzureWorkflowSystem.Api.Tests.BackgroundServices;

public class SlaMonitoringServiceTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<SlaMonitoringService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public SlaMonitoringServiceTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<SlaMonitoringService>>();
        _mockConfiguration = new Mock<IConfiguration>();

        // Setup default configuration
        _mockConfiguration.Setup(x => x.GetValue<int>("SlaMonitoring:CheckIntervalMinutes", 15))
            .Returns(1); // Use 1 minute for faster testing
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var service = new SlaMonitoringService(
            _mockServiceProvider.Object,
            _mockLogger.Object,
            _mockConfiguration.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Configuration_UsesCustomCheckInterval_WhenConfigured()
    {
        // Arrange
        _mockConfiguration.Setup(x => x.GetValue<int>("SlaMonitoring:CheckIntervalMinutes", 15))
            .Returns(30);

        // Act
        var service = new SlaMonitoringService(
            _mockServiceProvider.Object,
            _mockLogger.Object,
            _mockConfiguration.Object);

        // Assert
        Assert.NotNull(service);
        _mockConfiguration.Verify(x => x.GetValue<int>("SlaMonitoring:CheckIntervalMinutes", 15), Times.Once);
    }

    [Fact]
    public void Configuration_UsesDefaultCheckInterval_WhenNotConfigured()
    {
        // Arrange
        _mockConfiguration.Setup(x => x.GetValue<int>("SlaMonitoring:CheckIntervalMinutes", 15))
            .Returns(15); // Default value

        // Act
        var service = new SlaMonitoringService(
            _mockServiceProvider.Object,
            _mockLogger.Object,
            _mockConfiguration.Object);

        // Assert
        Assert.NotNull(service);
        _mockConfiguration.Verify(x => x.GetValue<int>("SlaMonitoring:CheckIntervalMinutes", 15), Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    public void Configuration_AcceptsVariousCheckIntervals(int intervalMinutes)
    {
        // Arrange
        _mockConfiguration.Setup(x => x.GetValue<int>("SlaMonitoring:CheckIntervalMinutes", 15))
            .Returns(intervalMinutes);

        // Act
        var service = new SlaMonitoringService(
            _mockServiceProvider.Object,
            _mockLogger.Object,
            _mockConfiguration.Object);

        // Assert
        Assert.NotNull(service);
        _mockConfiguration.Verify(x => x.GetValue<int>("SlaMonitoring:CheckIntervalMinutes", 15), Times.Once);
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SlaMonitoringService(
            null!,
            _mockLogger.Object,
            _mockConfiguration.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SlaMonitoringService(
            _mockServiceProvider.Object,
            null!,
            _mockConfiguration.Object));
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SlaMonitoringService(
            _mockServiceProvider.Object,
            _mockLogger.Object,
            null!));
    }
}