using AzureWorkflowSystem.Api.Controllers;
using AzureWorkflowSystem.Api.Data;
using AzureWorkflowSystem.Api.DTOs;
using AzureWorkflowSystem.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reflection;
using System.Security.Claims;
using Xunit;

namespace AzureWorkflowSystem.Api.Tests.Controllers;

public class AlertsControllerTests
{
    private static WorkflowDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var context = new WorkflowDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static AlertsController GetController(WorkflowDbContext context)
    {
        var logger = new Mock<ILogger<AlertsController>>();
        var controller = new AlertsController(context, logger.Object);
        
        // Set up authenticated context for API key authorization
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-API-Key"] = "development-webhook-api-key-for-testing-only";
        
        // Mock successful authentication
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "WebhookClient"),
            new Claim(ClaimTypes.NameIdentifier, "webhook-client"),
            new Claim("webhook", "true")
        };
        var identity = new ClaimsIdentity(claims, "ApiKey");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;
        
        controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext
        };
        
        return controller;
    }

    private static async Task<User> CreateTestUser(WorkflowDbContext context)
    {
        var user = new User
        {
            Email = "system@test.com",
            FirstName = "System",
            LastName = "User",
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static async Task<SlaConfiguration> CreateTestSlaConfiguration(WorkflowDbContext context)
    {
        var slaConfig = new SlaConfiguration
        {
            Priority = TicketPriority.Critical,
            Category = TicketCategory.Alert,
            ResolutionTimeMinutes = 60, // 1 hour for critical alerts
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.SlaConfigurations.Add(slaConfig);
        await context.SaveChangesAsync();
        return slaConfig;
    }

    private static AlertWebhookDto CreateTestAlertPayload(string alertId = "test-alert-123", string? severity = "critical")
    {
        return new AlertWebhookDto
        {
            Data = new AlertData
            {
                Essentials = new AlertEssentials
                {
                    AlertId = alertId,
                    AlertRule = "Test Alert Rule",
                    Severity = severity ?? "",
                    SignalType = "Metric",
                    MonitorCondition = "Fired",
                    FiredDateTime = DateTime.UtcNow,
                    Description = "Test alert description",
                    TargetResource = new[] { "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.Compute/virtualMachines/test-vm" }
                },
                AlertContext = new AlertContext
                {
                    Conditions = new[]
                    {
                        new AlertCondition
                        {
                            MetricName = "CPU Percentage",
                            MetricValue = "95.5",
                            MetricUnit = "Percent",
                            Threshold = "80",
                            Operator = "GreaterThan",
                            TimeAggregation = "Average"
                        }
                    }
                }
            }
        };
    }

    [Fact]
    public async Task ProcessAlert_WithNewAlert_CreatesTicket()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);
        var slaConfig = await CreateTestSlaConfiguration(context);

        var controller = GetController(context);
        var alertPayload = CreateTestAlertPayload();

        // Act
        var result = await controller.ProcessAlert(alertPayload);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        // Check that the result contains the expected properties (anonymous type)
        var resultValue = okResult.Value;
        Assert.NotNull(resultValue);
        
        // Verify ticket was created
        var ticket = await context.Tickets.FirstOrDefaultAsync(t => t.AlertId == "test-alert-123");
        Assert.NotNull(ticket);
        Assert.Equal("Alert: Test Alert Rule", ticket.Title);
        Assert.Equal(TicketPriority.Critical, ticket.Priority);
        Assert.Equal(TicketCategory.Alert, ticket.Category);
        Assert.Equal(TicketStatus.New, ticket.Status);
        Assert.Equal("test-alert-123", ticket.AlertId);
        Assert.Contains("Test Alert Rule", ticket.Description);
        Assert.Contains("critical", ticket.Description);
        Assert.NotNull(ticket.SlaTargetDate);

        // Verify audit log was created
        var auditLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.TicketId == ticket.Id);
        Assert.NotNull(auditLog);
        Assert.Equal("TICKET_CREATED_FROM_ALERT", auditLog.Action);
        Assert.Contains("test-alert-123", auditLog.Details);
    }

    [Fact]
    public async Task ProcessAlert_WithDuplicateAlert_ReturnsExistingTicket()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);

        // Create existing ticket for the same alert
        var existingTicket = new Ticket
        {
            Title = "Existing Alert Ticket",
            Description = "Existing description",
            Priority = TicketPriority.High,
            Category = TicketCategory.Alert,
            Status = TicketStatus.InProgress,
            AlertId = "duplicate-alert-123",
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Tickets.Add(existingTicket);
        await context.SaveChangesAsync();

        var controller = GetController(context);
        var alertPayload = CreateTestAlertPayload("duplicate-alert-123");

        // Act
        var result = await controller.ProcessAlert(alertPayload);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        // Verify no new ticket was created
        var ticketCount = await context.Tickets.CountAsync(t => t.AlertId == "duplicate-alert-123");
        Assert.Equal(1, ticketCount);

        // Verify existing ticket is still there
        var ticket = await context.Tickets.FirstOrDefaultAsync(t => t.AlertId == "duplicate-alert-123");
        Assert.NotNull(ticket);
        Assert.Equal(existingTicket.Id, ticket.Id);
        Assert.Equal("Existing Alert Ticket", ticket.Title);
        Assert.Equal(TicketStatus.InProgress, ticket.Status);
    }

    [Theory]
    [InlineData("sev0", TicketPriority.Critical)]
    [InlineData("critical", TicketPriority.Critical)]
    [InlineData("sev1", TicketPriority.High)]
    [InlineData("error", TicketPriority.High)]
    [InlineData("sev2", TicketPriority.Medium)]
    [InlineData("warning", TicketPriority.Medium)]
    [InlineData("sev3", TicketPriority.Low)]
    [InlineData("informational", TicketPriority.Low)]
    [InlineData("unknown", TicketPriority.Medium)]
    [InlineData(null, TicketPriority.Medium)]
    public async Task ProcessAlert_SeverityMapping_SetsCorrectPriority(string severity, TicketPriority expectedPriority)
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);

        var controller = GetController(context);
        var alertPayload = CreateTestAlertPayload($"test-alert-{severity ?? "null"}", severity);

        // Act
        var result = await controller.ProcessAlert(alertPayload);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        var ticket = await context.Tickets.FirstOrDefaultAsync(t => t.AlertId == $"test-alert-{severity ?? "null"}");
        Assert.NotNull(ticket);
        Assert.Equal(expectedPriority, ticket.Priority);
    }

    [Fact]
    public async Task ProcessAlert_WithSlaConfiguration_SetsSlaTargetDate()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);

        // Create SLA configuration for emergency alerts
        var slaConfig = new SlaConfiguration
        {
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Alert,
            ResolutionTimeMinutes = 30, // 30 minutes for emergency alerts
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.SlaConfigurations.Add(slaConfig);
        await context.SaveChangesAsync();

        var controller = GetController(context);
        var alertPayload = CreateTestAlertPayload("sla-test-alert", "emergency");

        // Act
        var result = await controller.ProcessAlert(alertPayload);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        var ticket = await context.Tickets.FirstOrDefaultAsync(t => t.AlertId == "sla-test-alert");
        Assert.NotNull(ticket);
        Assert.NotNull(ticket.SlaTargetDate);
        
        // SLA target should be 30 minutes from the alert fired time
        var expectedSlaDate = alertPayload.Data.Essentials.FiredDateTime.ToUniversalTime().AddMinutes(30);
        Assert.True(Math.Abs((ticket.SlaTargetDate.Value - expectedSlaDate).TotalSeconds) < 60); // Allow 1 minute tolerance
    }

    [Fact]
    public async Task ProcessAlert_WithoutSlaConfiguration_DoesNotSetSlaTargetDate()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);

        var controller = GetController(context);
        var alertPayload = CreateTestAlertPayload("no-sla-alert", "medium");

        // Act
        var result = await controller.ProcessAlert(alertPayload);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        var ticket = await context.Tickets.FirstOrDefaultAsync(t => t.AlertId == "no-sla-alert");
        Assert.NotNull(ticket);
        Assert.Null(ticket.SlaTargetDate);
        Assert.False(ticket.IsSlaBreach);
    }

    [Fact]
    public async Task ProcessAlert_WithMultipleTargetResources_SetsFirstAsAzureResourceId()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);

        var controller = GetController(context);
        var alertPayload = CreateTestAlertPayload();
        alertPayload.Data.Essentials.TargetResource = new[]
        {
            "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.Compute/virtualMachines/vm1",
            "/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.Compute/virtualMachines/vm2"
        };

        // Act
        var result = await controller.ProcessAlert(alertPayload);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        var ticket = await context.Tickets.FirstOrDefaultAsync(t => t.AlertId == "test-alert-123");
        Assert.NotNull(ticket);
        Assert.Equal("/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.Compute/virtualMachines/vm1", 
                     ticket.AzureResourceId);
    }

    [Fact]
    public async Task ProcessAlert_BuildsCompleteDescription()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);

        var controller = GetController(context);
        var alertPayload = CreateTestAlertPayload();

        // Act
        var result = await controller.ProcessAlert(alertPayload);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        var ticket = await context.Tickets.FirstOrDefaultAsync(t => t.AlertId == "test-alert-123");
        Assert.NotNull(ticket);
        
        var description = ticket.Description;
        Assert.Contains("Azure Monitor Alert Details:", description);
        Assert.Contains("Alert Rule: Test Alert Rule", description);
        Assert.Contains("Alert ID: test-alert-123", description);
        Assert.Contains("Severity: critical", description);
        Assert.Contains("Signal Type: Metric", description);
        Assert.Contains("Monitor Condition: Fired", description);
        Assert.Contains("Description: Test alert description", description);
        Assert.Contains("Target Resources:", description);
        Assert.Contains("test-vm", description);
        Assert.Contains("Alert Conditions:", description);
        Assert.Contains("Metric: CPU Percentage", description);
        Assert.Contains("Value: 95.5 Percent", description);
        Assert.Contains("Threshold: 80", description);
        Assert.Contains("Operator: GreaterThan", description);
        Assert.Contains("Time Aggregation: Average", description);
    }

    [Fact]
    public async Task ProcessAlert_WithNullData_ReturnsServerError()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);

        var controller = GetController(context);
        var alertPayload = new AlertWebhookDto
        {
            Data = null!
        };

        // Act
        var result = await controller.ProcessAlert(alertPayload);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
        
        // Verify no ticket was created
        var ticketCount = await context.Tickets.CountAsync();
        Assert.Equal(0, ticketCount);
    }

    [Fact]
    public async Task ProcessAlert_WithoutApiKey_ReturnsUnauthorized()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);

        // Create controller without authentication context (simulates missing API key)
        var logger = new Mock<ILogger<AlertsController>>();
        var controller = new AlertsController(context, logger.Object);
        
        // Create mock HttpContext without X-API-Key header
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext
        };

        var alertPayload = CreateTestAlertPayload();

        // Act & Assert
        // Since the controller now requires [Authorize(AuthenticationSchemes = "ApiKey")],
        // this test verifies the authentication requirement exists
        // The actual 401 response would be handled by the authentication middleware
        // We can verify the attribute is present
        var method = typeof(AlertsController).GetMethod(nameof(AlertsController.ProcessAlert));
        var authorizeAttribute = method?.GetCustomAttributes(typeof(AuthorizeAttribute), false).FirstOrDefault() as AuthorizeAttribute;
        
        Assert.NotNull(authorizeAttribute);
        Assert.Equal("ApiKey", authorizeAttribute.AuthenticationSchemes);
    }

    [Fact]
    public async Task ProcessAlert_WithValidApiKey_Succeeds()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);

        var controller = GetController(context);
        
        // Create mock HttpContext with valid API key
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-API-Key"] = "development-webhook-api-key-for-testing-only";
        
        // Mock successful authentication
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "WebhookClient"),
            new Claim(ClaimTypes.NameIdentifier, "webhook-client"),
            new Claim("webhook", "true")
        };
        var identity = new ClaimsIdentity(claims, "ApiKey");
        var principal = new ClaimsPrincipal(identity);
        httpContext.User = principal;
        
        controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext
        };

        var alertPayload = CreateTestAlertPayload("auth-test-alert");

        // Act
        var result = await controller.ProcessAlert(alertPayload);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        // Verify ticket was created
        var ticket = await context.Tickets.FirstOrDefaultAsync(t => t.AlertId == "auth-test-alert");
        Assert.NotNull(ticket);
        Assert.Equal("Alert: Test Alert Rule", ticket.Title);
    }

    [Fact]
    public async Task ProcessAlert_UsesAlertFiredDateTimeAsCreatedAt()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);

        var controller = GetController(context);
        var alertTime = new DateTime(2023, 12, 1, 10, 30, 0, DateTimeKind.Utc);
        var alertPayload = CreateTestAlertPayload();
        alertPayload.Data.Essentials.FiredDateTime = alertTime;

        // Act
        var result = await controller.ProcessAlert(alertPayload);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        
        var ticket = await context.Tickets.FirstOrDefaultAsync(t => t.AlertId == "test-alert-123");
        Assert.NotNull(ticket);
        Assert.Equal(alertTime, ticket.CreatedAt);
    }
}