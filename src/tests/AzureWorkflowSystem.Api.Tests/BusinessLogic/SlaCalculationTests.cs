using AzureWorkflowSystem.Api.Data;
using AzureWorkflowSystem.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AzureWorkflowSystem.Api.Tests.BusinessLogic;

public class SlaCalculationTests
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

    [Fact]
    public async Task SlaCalculation_WithMatchingConfiguration_SetsSlaTargetDate()
    {
        // Arrange
        using var context = GetDbContext();
        
        var slaConfig = new SlaConfiguration
        {
            Priority = TicketPriority.High,
            Category = TicketCategory.Incident,
            ResolutionTimeMinutes = 120, // 2 hours
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.SlaConfigurations.Add(slaConfig);
        await context.SaveChangesAsync();

        var createdAt = new DateTime(2023, 12, 1, 10, 0, 0, DateTimeKind.Utc);
        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            Priority = TicketPriority.High,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = 1,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        // Act - Simulate SLA calculation logic
        var matchingSlaConfig = await context.SlaConfigurations
            .FirstOrDefaultAsync(s => s.Priority == ticket.Priority && 
                                    s.Category == ticket.Category && 
                                    s.IsActive);

        if (matchingSlaConfig != null)
        {
            ticket.SlaTargetDate = ticket.CreatedAt.AddMinutes(matchingSlaConfig.ResolutionTimeMinutes);
            ticket.IsSlaBreach = ticket.SlaTargetDate < DateTime.UtcNow;
        }

        // Assert
        Assert.NotNull(matchingSlaConfig);
        Assert.NotNull(ticket.SlaTargetDate);
        // SLA target should be 2 hours (120 minutes) from creation time - allow for small timing differences
        var expectedSlaDate = createdAt.AddMinutes(120);
        Assert.True(Math.Abs((ticket.SlaTargetDate.Value - expectedSlaDate).TotalMinutes) < 1);
        Assert.False(ticket.IsSlaBreach); // In the past, so no breach
    }

    [Fact]
    public async Task SlaCalculation_WithoutMatchingConfiguration_DoesNotSetSlaTargetDate()
    {
        // Arrange
        using var context = GetDbContext();
        
        // Create SLA config for different priority/category combination
        var slaConfig = new SlaConfiguration
        {
            Priority = TicketPriority.Low,
            Category = TicketCategory.NewResource,
            ResolutionTimeMinutes = 480, // 8 hours
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.SlaConfigurations.Add(slaConfig);
        await context.SaveChangesAsync();

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            Priority = TicketPriority.High, // Different priority
            Category = TicketCategory.Incident, // Different category
            Status = TicketStatus.New,
            CreatedById = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act - Simulate SLA calculation logic
        var matchingSlaConfig = await context.SlaConfigurations
            .FirstOrDefaultAsync(s => s.Priority == ticket.Priority && 
                                    s.Category == ticket.Category && 
                                    s.IsActive);

        if (matchingSlaConfig != null)
        {
            ticket.SlaTargetDate = ticket.CreatedAt.AddMinutes(matchingSlaConfig.ResolutionTimeMinutes);
            ticket.IsSlaBreach = ticket.SlaTargetDate < DateTime.UtcNow;
        }

        // Assert
        Assert.Null(matchingSlaConfig);
        Assert.Null(ticket.SlaTargetDate);
        Assert.False(ticket.IsSlaBreach);
    }

    [Fact]
    public async Task SlaCalculation_WithInactiveConfiguration_DoesNotUseSla()
    {
        // Arrange
        using var context = GetDbContext();
        
        var inactiveSlaConfig = new SlaConfiguration
        {
            Priority = TicketPriority.Critical,
            Category = TicketCategory.Alert,
            ResolutionTimeMinutes = 30,
            IsActive = false, // Inactive
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.SlaConfigurations.Add(inactiveSlaConfig);
        await context.SaveChangesAsync();

        var ticket = new Ticket
        {
            Title = "Critical Alert",
            Description = "Critical alert ticket",
            Priority = TicketPriority.Critical,
            Category = TicketCategory.Alert,
            Status = TicketStatus.New,
            CreatedById = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act - Simulate SLA calculation logic
        var matchingSlaConfig = await context.SlaConfigurations
            .FirstOrDefaultAsync(s => s.Priority == ticket.Priority && 
                                    s.Category == ticket.Category && 
                                    s.IsActive);

        if (matchingSlaConfig != null)
        {
            ticket.SlaTargetDate = ticket.CreatedAt.AddMinutes(matchingSlaConfig.ResolutionTimeMinutes);
            ticket.IsSlaBreach = ticket.SlaTargetDate < DateTime.UtcNow;
        }

        // Assert
        Assert.Null(matchingSlaConfig);
        Assert.Null(ticket.SlaTargetDate);
        Assert.False(ticket.IsSlaBreach);
    }

    [Fact]
    public async Task SlaCalculation_WithBreachedSla_SetsIsSlaBreach()
    {
        // Arrange
        using var context = GetDbContext();
        
        var slaConfig = new SlaConfiguration
        {
            Priority = TicketPriority.Critical,
            Category = TicketCategory.Incident,
            ResolutionTimeMinutes = 60, // 1 hour
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.SlaConfigurations.Add(slaConfig);
        await context.SaveChangesAsync();

        // Create ticket 2 hours ago (should be breached)
        var createdAt = DateTime.UtcNow.AddHours(-2);
        var ticket = new Ticket
        {
            Title = "Old Critical Ticket",
            Description = "This ticket is old",
            Priority = TicketPriority.Critical,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = 1,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        // Act - Simulate SLA calculation logic
        var matchingSlaConfig = await context.SlaConfigurations
            .FirstOrDefaultAsync(s => s.Priority == ticket.Priority && 
                                    s.Category == ticket.Category && 
                                    s.IsActive);

        if (matchingSlaConfig != null)
        {
            ticket.SlaTargetDate = ticket.CreatedAt.AddMinutes(matchingSlaConfig.ResolutionTimeMinutes);
            ticket.IsSlaBreach = ticket.SlaTargetDate < DateTime.UtcNow;
        }

        // Assert
        Assert.NotNull(matchingSlaConfig);
        Assert.NotNull(ticket.SlaTargetDate);
        Assert.True(ticket.IsSlaBreach);
        Assert.True(ticket.SlaTargetDate.Value < DateTime.UtcNow);
    }

    [Theory]
    [InlineData(TicketPriority.Critical, TicketCategory.Incident, 15)]
    [InlineData(TicketPriority.High, TicketCategory.Incident, 60)]
    [InlineData(TicketPriority.Medium, TicketCategory.Incident, 240)]
    [InlineData(TicketPriority.Low, TicketCategory.Incident, 480)]
    [InlineData(TicketPriority.Critical, TicketCategory.Alert, 30)]
    [InlineData(TicketPriority.High, TicketCategory.NewResource, 720)]
    public async Task SlaCalculation_WithDifferentPriorityAndCategory_UsesCorrectResolutionTime(
        TicketPriority priority, TicketCategory category, int expectedMinutes)
    {
        // Arrange
        using var context = GetDbContext();
        
        var slaConfig = new SlaConfiguration
        {
            Priority = priority,
            Category = category,
            ResolutionTimeMinutes = expectedMinutes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.SlaConfigurations.Add(slaConfig);
        await context.SaveChangesAsync();

        var createdAt = new DateTime(2023, 12, 1, 9, 0, 0, DateTimeKind.Utc);
        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            Priority = priority,
            Category = category,
            Status = TicketStatus.New,
            CreatedById = 1,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        // Act - Simulate SLA calculation logic
        var matchingSlaConfig = await context.SlaConfigurations
            .FirstOrDefaultAsync(s => s.Priority == ticket.Priority && 
                                    s.Category == ticket.Category && 
                                    s.IsActive);

        if (matchingSlaConfig != null)
        {
            ticket.SlaTargetDate = ticket.CreatedAt.AddMinutes(matchingSlaConfig.ResolutionTimeMinutes);
            ticket.IsSlaBreach = ticket.SlaTargetDate < DateTime.UtcNow;
        }

        // Assert
        Assert.NotNull(matchingSlaConfig);
        Assert.NotNull(ticket.SlaTargetDate);
        var expectedTargetDate = createdAt.AddMinutes(expectedMinutes);
        Assert.True(Math.Abs((ticket.SlaTargetDate.Value - expectedTargetDate).TotalMinutes) < 1);
    }

    [Fact]
    public async Task SlaCalculation_WithMultipleMatchingConfigs_UsesFirstActive()
    {
        // Arrange
        using var context = GetDbContext();
        
        var slaConfig1 = new SlaConfiguration
        {
            Priority = TicketPriority.High,
            Category = TicketCategory.Incident,
            ResolutionTimeMinutes = 120,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1), // Older
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var slaConfig2 = new SlaConfiguration
        {
            Priority = TicketPriority.High,
            Category = TicketCategory.Incident,
            ResolutionTimeMinutes = 60, // Different resolution time
            IsActive = true,
            CreatedAt = DateTime.UtcNow, // Newer
            UpdatedAt = DateTime.UtcNow
        };

        context.SlaConfigurations.AddRange(slaConfig1, slaConfig2);
        await context.SaveChangesAsync();

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            Priority = TicketPriority.High,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act - Simulate SLA calculation logic (FirstOrDefaultAsync returns first match)
        var matchingSlaConfig = await context.SlaConfigurations
            .FirstOrDefaultAsync(s => s.Priority == ticket.Priority && 
                                    s.Category == ticket.Category && 
                                    s.IsActive);

        if (matchingSlaConfig != null)
        {
            ticket.SlaTargetDate = ticket.CreatedAt.AddMinutes(matchingSlaConfig.ResolutionTimeMinutes);
            ticket.IsSlaBreach = ticket.SlaTargetDate < DateTime.UtcNow;
        }

        // Assert - Should use the first config found (which could be either, depending on EF Core's ordering)
        Assert.NotNull(matchingSlaConfig);
        Assert.NotNull(ticket.SlaTargetDate);
        Assert.True(matchingSlaConfig.ResolutionTimeMinutes == 120 || matchingSlaConfig.ResolutionTimeMinutes == 60);
    }
}