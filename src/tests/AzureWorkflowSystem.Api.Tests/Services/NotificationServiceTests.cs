using AzureWorkflowSystem.Api.Models;
using AzureWorkflowSystem.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using Xunit;

namespace AzureWorkflowSystem.Api.Tests.Services;

public class NotificationServiceTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<NotificationService>> _mockLogger;
    private readonly NotificationService _notificationService;

    public NotificationServiceTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<NotificationService>>();

        _notificationService = new NotificationService(
            _mockHttpClientFactory.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SendSlaBreachNotification_WithSingleTicket_CallsSendSlaBreachNotifications()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["Teams:WebhookUrl"])
            .Returns("https://outlook.office.com/webhook/test");

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var ticket = CreateTestTicket("Test Breach", TicketPriority.Critical);

        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        await _notificationService.SendSlaBreachNotification(ticket, isImminentBreach: true);

        // Assert
        mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SendSlaBreachNotifications_WithNoWebhookUrl_LogsWarningAndReturns()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["Teams:WebhookUrl"])
            .Returns((string?)null);

        var tickets = new List<Ticket>
        {
            CreateTestTicket("Test Ticket", TicketPriority.High)
        };

        // Act
        await _notificationService.SendSlaBreachNotifications(tickets, isImminentBreach: true);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Teams webhook URL not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendSlaBreachNotifications_WithEmptyTicketList_ReturnsEarly()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["Teams:WebhookUrl"])
            .Returns("https://outlook.office.com/webhook/test");

        var emptyTickets = new List<Ticket>();

        // Act
        await _notificationService.SendSlaBreachNotifications(emptyTickets, isImminentBreach: true);

        // Assert - Should not create any HTTP client
        _mockHttpClientFactory.Verify(
            x => x.CreateClient(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task SendSlaBreachNotifications_WithMultipleTickets_SendsNotificationWithCorrectContent()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["Teams:WebhookUrl"])
            .Returns("https://outlook.office.com/webhook/test");

        var tickets = new List<Ticket>
        {
            CreateTestTicket("Critical Incident", TicketPriority.Critical),
            CreateTestTicket("High Priority Alert", TicketPriority.High)
        };

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        HttpRequestMessage? capturedRequest = null;
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, token) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        await _notificationService.SendSlaBreachNotifications(tickets, isImminentBreach: true);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Contains("application/json", capturedRequest.Content!.Headers.ContentType!.ToString());

        var content = await capturedRequest.Content.ReadAsStringAsync();
        Assert.Contains("\\u26A0\\uFE0F SLA Breach Warning", content);  // Unicode escaped emoji
        Assert.Contains("Critical Incident", content);
        Assert.Contains("High Priority Alert", content);
        Assert.Contains("2 tickets", content);
    }

    [Fact]
    public async Task SendSlaBreachNotifications_WithActualBreach_SendsCorrectNotificationType()
    {
        // Arrange
        _mockConfiguration.Setup(x => x["Teams:WebhookUrl"])
            .Returns("https://outlook.office.com/webhook/test");

        var tickets = new List<Ticket>
        {
            CreateTestTicket("Breached Ticket", TicketPriority.Emergency)
        };

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

        HttpRequestMessage? capturedRequest = null;
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, token) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        await _notificationService.SendSlaBreachNotifications(tickets, isImminentBreach: false);

        // Assert
        Assert.NotNull(capturedRequest);
        var content = await capturedRequest.Content.ReadAsStringAsync();
        Assert.Contains("\\uD83D\\uDEA8 SLA Breach Alert", content);  // Unicode escaped emoji
        Assert.Contains("Breached Ticket", content);
    }

    private static Ticket CreateTestTicket(string title, TicketPriority priority)
    {
        return new Ticket
        {
            Id = Random.Shared.Next(1, 1000),
            Title = title,
            Description = $"Test description for {title}",
            Priority = priority,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedAt = DateTime.UtcNow.AddMinutes(-30),
            CreatedById = 1,
            SlaTargetDate = DateTime.UtcNow.AddMinutes(5),
            CreatedBy = new User
            {
                Id = 1,
                Email = "creator@test.com",
                FirstName = "Test",
                LastName = "Creator",
                Role = UserRole.Admin,
                IsActive = true
            }
        };
    }
}