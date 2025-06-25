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

        var createdAt = DateTime.UtcNow.AddHours(-5); // Created 5 hours ago
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

        // Act - 10% of 6 hours = 36 minutes, so with 1 hour remaining, it should be imminent
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
            ResolutionTimeMinutes = 60,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.SlaConfigurations.Add(slaConfig);

        // Ticket with imminent breach (50 minutes into 60 minute SLA)
        var imminentTicket = new Ticket
        {
            Title = "Imminent Breach",
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedAt = DateTime.UtcNow.AddMinutes(-50),
            CreatedById = user.Id,
            SlaTargetDate = DateTime.UtcNow.AddMinutes(10) // 10 minutes left
        };

        // Ticket with plenty of time left
        var safeTicket = new Ticket
        {
            Title = "Safe Ticket",
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10),
            CreatedById = user.Id,
            SlaTargetDate = DateTime.UtcNow.AddMinutes(50) // 50 minutes left
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
}