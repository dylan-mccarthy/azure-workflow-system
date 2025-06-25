using AzureWorkflowSystem.Api.Data;
using AzureWorkflowSystem.Api.Models;
using AzureWorkflowSystem.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AzureWorkflowSystem.Api.Tests.Services;

public class SlaServiceTests : IDisposable
{
    private readonly WorkflowDbContext _context;
    private readonly Mock<ILogger<SlaService>> _mockLogger;
    private readonly SlaService _slaService;

    public SlaServiceTests()
    {
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new WorkflowDbContext(options);
        _mockLogger = new Mock<ILogger<SlaService>>();
        _slaService = new SlaService(_context, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task CalculateSlaTargetDate_WithValidConfiguration_SetsSlaTargetDate()
    {
        // Arrange
        var slaConfig = new SlaConfiguration
        {
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            ResolutionTimeMinutes = 60,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.SlaConfigurations.Add(slaConfig);
        await _context.SaveChangesAsync();

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            CreatedAt = DateTime.UtcNow,
            CreatedById = 1
        };

        // Act
        await _slaService.CalculateSlaTargetDate(ticket);

        // Assert
        Assert.NotNull(ticket.SlaTargetDate);
        Assert.Equal(ticket.CreatedAt.AddMinutes(60), ticket.SlaTargetDate);
    }

    [Fact]
    public async Task IsSlaBreach_WithPastDueTicket_ReturnsTrue()
    {
        // Arrange
        var ticket = new Ticket
        {
            SlaTargetDate = DateTime.UtcNow.AddHours(-1), // 1 hour past due
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };

        // Act
        var result = await _slaService.IsSlaBreach(ticket);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsSlaBreach_WithFutureTicket_ReturnsFalse()
    {
        // Arrange
        var ticket = new Ticket
        {
            SlaTargetDate = DateTime.UtcNow.AddHours(1), // 1 hour in the future
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        // Act
        var result = await _slaService.IsSlaBreach(ticket);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsImminentSlaBreach_WithTicketIn10PercentBuffer_ReturnsTrue()
    {
        // Arrange
        var slaConfig = new SlaConfiguration
        {
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            ResolutionTimeMinutes = 360, // 6 hours
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.SlaConfigurations.Add(slaConfig);
        await _context.SaveChangesAsync();

        var createdAt = DateTime.UtcNow.AddMinutes(-330); // Created 330 minutes ago (5.5 hours)
        // With 360 minute SLA, target will be 30 minutes from now
        // 30 minutes remaining out of 360 = 8.3% remaining (< 10%)
        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            CreatedAt = createdAt,
            CreatedById = 1
        };

        // Calculate SLA first
        await _slaService.CalculateSlaTargetDate(ticket);

        // Act - 10% of 6 hours = 36 minutes, with 30 minutes remaining, it should be imminent
        var result = await _slaService.IsImminentSlaBreach(ticket, 0.1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsImminentSlaBreach_WithTicketOutside10PercentBuffer_ReturnsFalse()
    {
        // Arrange
        var slaConfig = new SlaConfiguration
        {
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            ResolutionTimeMinutes = 360, // 6 hours
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.SlaConfigurations.Add(slaConfig);
        await _context.SaveChangesAsync();

        var createdAt = DateTime.UtcNow.AddHours(-2); // Created 2 hours ago
        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            CreatedAt = createdAt,
            CreatedById = 1
        };

        // Calculate SLA first
        await _slaService.CalculateSlaTargetDate(ticket);

        // Act - 10% of 6 hours = 36 minutes, so with 4 hours remaining, it should not be imminent
        var result = await _slaService.IsImminentSlaBreach(ticket, 0.1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetImminentSlaBreachTickets_ReturnsOnlyImminentBreachTickets()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Engineer,
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var slaConfig = new SlaConfiguration
        {
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            ResolutionTimeMinutes = 60, // 1 hour
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.SlaConfigurations.Add(slaConfig);

        // Ticket with imminent breach (55 minutes into 60 minute SLA = 5 minutes left = 8.3% remaining)
        var imminentTicket = new Ticket
        {
            Title = "Imminent Breach",
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedAt = DateTime.UtcNow.AddMinutes(-55), // 55 minutes ago
            CreatedById = user.Id
        };

        // Ticket with plenty of time left (10 minutes into 60 minute SLA = 50 minutes left = 83% remaining)
        var safeTicket = new Ticket
        {
            Title = "Safe Ticket",
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10), // 10 minutes ago
            CreatedById = user.Id
        };

        _context.Tickets.AddRange(imminentTicket, safeTicket);
        await _context.SaveChangesAsync();

        // Act
        var result = await _slaService.GetImminentSlaBreachTickets();

        // Assert
        var tickets = result.ToList();
        Assert.Single(tickets);
        Assert.Equal("Imminent Breach", tickets[0].Title);
    }

    [Fact]
    public async Task IsImminentSlaBreach_WithExactlyAtBufferThreshold_ReturnsTrue()
    {
        // Arrange
        var slaConfig = new SlaConfiguration
        {
            Priority = TicketPriority.High,
            Category = TicketCategory.Alert,
            ResolutionTimeMinutes = 120, // 2 hours
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.SlaConfigurations.Add(slaConfig);
        await _context.SaveChangesAsync();

        // Created 108 minutes ago, so 12 minutes remaining out of 120 (exactly 10%)
        var createdAt = DateTime.UtcNow.AddMinutes(-108);
        var ticket = new Ticket
        {
            Title = "Threshold Test",
            Priority = TicketPriority.High,
            Category = TicketCategory.Alert,
            CreatedAt = createdAt,
            CreatedById = 1
        };

        await _slaService.CalculateSlaTargetDate(ticket);

        // Act - at exactly 10% threshold
        var result = await _slaService.IsImminentSlaBreach(ticket, 0.1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsImminentSlaBreach_WithCustomBufferPercentage_WorksCorrectly()
    {
        // Arrange
        var slaConfig = new SlaConfiguration
        {
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            ResolutionTimeMinutes = 240, // 4 hours
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.SlaConfigurations.Add(slaConfig);
        await _context.SaveChangesAsync();

        // Created 200 minutes ago, so 40 minutes remaining out of 240 (16.67%)
        var createdAt = DateTime.UtcNow.AddMinutes(-200);
        var ticket = new Ticket
        {
            Title = "Custom Buffer Test",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            CreatedAt = createdAt,
            CreatedById = 1
        };

        await _slaService.CalculateSlaTargetDate(ticket);

        // Act - with 20% buffer (48 minutes), 40 minutes remaining should be imminent
        var result = await _slaService.IsImminentSlaBreach(ticket, 0.2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsImminentSlaBreach_WithNoSlaTargetDate_ReturnsFalse()
    {
        // Arrange
        var ticket = new Ticket
        {
            Title = "No SLA Config",
            Priority = TicketPriority.Low,
            Category = TicketCategory.Alert, // No config exists for Low/Alert
            CreatedAt = DateTime.UtcNow,
            CreatedById = 1,
            SlaTargetDate = null
        };

        // Act
        var result = await _slaService.IsImminentSlaBreach(ticket);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsImminentSlaBreach_WithAlreadyBreachedTicket_ReturnsFalse()
    {
        // Arrange
        var slaConfig = new SlaConfiguration
        {
            Priority = TicketPriority.Critical,
            Category = TicketCategory.Incident,
            ResolutionTimeMinutes = 60,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.SlaConfigurations.Add(slaConfig);
        await _context.SaveChangesAsync();

        // Created 90 minutes ago (already breached by 30 minutes)
        var createdAt = DateTime.UtcNow.AddMinutes(-90);
        var ticket = new Ticket
        {
            Title = "Already Breached",
            Priority = TicketPriority.Critical,
            Category = TicketCategory.Incident,
            CreatedAt = createdAt,
            CreatedById = 1
        };

        await _slaService.CalculateSlaTargetDate(ticket);

        // Act
        var result = await _slaService.IsImminentSlaBreach(ticket);

        // Assert
        Assert.False(result); // Should return false because it's already breached
    }

    [Fact]
    public async Task UpdateSlaStatus_WithBreachingTicket_UpdatesStatusAndLogs()
    {
        // Arrange
        var slaConfig = new SlaConfiguration
        {
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Alert,
            ResolutionTimeMinutes = 30,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.SlaConfigurations.Add(slaConfig);
        await _context.SaveChangesAsync();

        var ticket = new Ticket
        {
            Title = "Status Update Test",
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Alert,
            CreatedAt = DateTime.UtcNow.AddMinutes(-45), // 45 minutes ago (breached)
            CreatedById = 1,
            IsSlaBreach = false // Initially not marked as breached
        };

        await _slaService.CalculateSlaTargetDate(ticket);

        // Act
        await _slaService.UpdateSlaStatus(ticket);

        // Assert
        Assert.True(ticket.IsSlaBreach);
    }

    [Fact]
    public async Task CalculateSlaTargetDate_WithInactiveConfiguration_DoesNotSetTargetDate()
    {
        // Arrange
        var inactiveSlaConfig = new SlaConfiguration
        {
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            ResolutionTimeMinutes = 120,
            IsActive = false, // Inactive configuration
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.SlaConfigurations.Add(inactiveSlaConfig);
        await _context.SaveChangesAsync();

        var ticket = new Ticket
        {
            Title = "Inactive Config Test",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            CreatedAt = DateTime.UtcNow,
            CreatedById = 1
        };

        // Act
        await _slaService.CalculateSlaTargetDate(ticket);

        // Assert
        Assert.Null(ticket.SlaTargetDate);
    }

    [Fact]
    public async Task GetImminentSlaBreachTickets_WithMultiplePriorities_ReturnsOnlyImminent()
    {
        // Arrange
        var user = new User
        {
            Email = "multitest@example.com",
            FirstName = "Multi",
            LastName = "User",
            Role = UserRole.Engineer,
            IsActive = true
        };
        _context.Users.Add(user);

        var slaConfigEmergency = new SlaConfiguration
        {
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            ResolutionTimeMinutes = 60,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var slaConfigHigh = new SlaConfiguration
        {
            Priority = TicketPriority.High,
            Category = TicketCategory.Incident,
            ResolutionTimeMinutes = 240, // 4 hours
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.SlaConfigurations.AddRange(slaConfigEmergency, slaConfigHigh);

        // Emergency ticket with imminent breach (55 minutes ago, 5 minutes remaining = 8.3%)
        var emergencyTicket = new Ticket
        {
            Title = "Emergency Imminent",
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            Status = TicketStatus.InProgress,
            CreatedAt = DateTime.UtcNow.AddMinutes(-55),
            CreatedById = user.Id
        };

        // High priority ticket with plenty of time (60 minutes ago, 180 minutes remaining = 75%)
        var highTicket = new Ticket
        {
            Title = "High Priority Safe",
            Priority = TicketPriority.High,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedAt = DateTime.UtcNow.AddMinutes(-60),
            CreatedById = user.Id
        };

        // Already resolved ticket (should be excluded)
        var resolvedTicket = new Ticket
        {
            Title = "Resolved Ticket",
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            Status = TicketStatus.Resolved,
            CreatedAt = DateTime.UtcNow.AddMinutes(-55),
            CreatedById = user.Id
        };

        _context.Tickets.AddRange(emergencyTicket, highTicket, resolvedTicket);
        await _context.SaveChangesAsync();

        // Act
        var result = await _slaService.GetImminentSlaBreachTickets();

        // Assert
        var tickets = result.ToList();
        Assert.Single(tickets);
        Assert.Equal("Emergency Imminent", tickets[0].Title);
        Assert.Equal(TicketStatus.InProgress, tickets[0].Status);
    }

    [Fact]
    public async Task GetImminentSlaBreachTickets_WithClosedAndResolvedTickets_ExcludesThemFromResults()
    {
        // Arrange
        var user = new User
        {
            Email = "exclude@example.com",
            FirstName = "Exclude",
            LastName = "User",
            Role = UserRole.Engineer,
            IsActive = true
        };
        _context.Users.Add(user);

        var slaConfig = new SlaConfiguration
        {
            Priority = TicketPriority.Critical,
            Category = TicketCategory.Alert,
            ResolutionTimeMinutes = 120,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.SlaConfigurations.Add(slaConfig);

        // Closed ticket that would be imminent if it were open
        var closedTicket = new Ticket
        {
            Title = "Closed Ticket",
            Priority = TicketPriority.Critical,
            Category = TicketCategory.Alert,
            Status = TicketStatus.Closed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-110), // 10 minutes remaining = 8.3%
            CreatedById = user.Id
        };

        // Resolved ticket that would be imminent if it were open
        var resolvedTicket = new Ticket
        {
            Title = "Resolved Ticket",
            Priority = TicketPriority.Critical,
            Category = TicketCategory.Alert,
            Status = TicketStatus.Resolved,
            CreatedAt = DateTime.UtcNow.AddMinutes(-110),
            CreatedById = user.Id
        };

        _context.Tickets.AddRange(closedTicket, resolvedTicket);
        await _context.SaveChangesAsync();

        // Act
        var result = await _slaService.GetImminentSlaBreachTickets();

        // Assert
        var tickets = result.ToList();
        Assert.Empty(tickets); // Should be empty because all tickets are closed/resolved
    }
}