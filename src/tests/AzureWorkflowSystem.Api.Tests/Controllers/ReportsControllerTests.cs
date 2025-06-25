using AzureWorkflowSystem.Api.Controllers;
using AzureWorkflowSystem.Api.Data;
using AzureWorkflowSystem.Api.DTOs;
using AzureWorkflowSystem.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AzureWorkflowSystem.Api.Tests.Controllers;

public class ReportsControllerTests
{
    private WorkflowDbContext GetInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new WorkflowDbContext(options);
    }

    [Fact]
    public async Task GetMetrics_WithValidData_ReturnsCorrectMtta()
    {
        // Arrange
        using var context = GetInMemoryContext();
        
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Engineer,
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var createdTime = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var assignedTime = new DateTime(2024, 1, 1, 9, 5, 0, DateTimeKind.Utc); // 5 minutes later

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.Assigned,
            CreatedById = user.Id,
            AssignedToId = user.Id,
            CreatedAt = createdTime,
            UpdatedAt = assignedTime
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var controller = new ReportsController(context);

        // Act
        var result = await controller.GetMetrics(createdTime.AddHours(-1), createdTime.AddHours(1));

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var metrics = Assert.IsType<ReportMetricsDto>(okResult.Value);
        
        Assert.Equal(5.0, metrics.MttaMinutes); // 5 minutes to acknowledgment
        Assert.Equal(1, metrics.TotalTickets);
        Assert.Equal(1, metrics.OpenTickets);
        Assert.Equal(0, metrics.ClosedTickets);
    }

    [Fact]
    public async Task GetMetrics_WithResolvedTickets_ReturnsCorrectMttr()
    {
        // Arrange
        using var context = GetInMemoryContext();
        
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Engineer,
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var createdTime = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var resolvedTime = new DateTime(2024, 1, 1, 11, 0, 0, DateTimeKind.Utc); // 2 hours later (120 minutes)

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.Resolved,
            CreatedById = user.Id,
            AssignedToId = user.Id,
            CreatedAt = createdTime,
            UpdatedAt = resolvedTime,
            ResolvedAt = resolvedTime
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var controller = new ReportsController(context);

        // Act
        var result = await controller.GetMetrics(createdTime.AddHours(-1), createdTime.AddHours(3));

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var metrics = Assert.IsType<ReportMetricsDto>(okResult.Value);
        
        Assert.Equal(120.0, metrics.MttrMinutes); // 120 minutes to resolution
        Assert.Equal(1, metrics.TotalTickets);
        Assert.Equal(0, metrics.OpenTickets);
        Assert.Equal(1, metrics.ClosedTickets);
    }

    [Fact]
    public async Task GetMetrics_WithSlaData_ReturnsCorrectSlaCompliance()
    {
        // Arrange
        using var context = GetInMemoryContext();
        
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Engineer,
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var createdTime = new DateTime(2024, 1, 1, 9, 0, 0, DateTimeKind.Utc);
        var slaTargetTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var resolvedTime = new DateTime(2024, 1, 1, 9, 30, 0, DateTimeKind.Utc); // Resolved within SLA

        // SLA compliant ticket
        var ticket1 = new Ticket
        {
            Title = "Test Ticket 1",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.Resolved,
            CreatedById = user.Id,
            CreatedAt = createdTime,
            ResolvedAt = resolvedTime,
            SlaTargetDate = slaTargetTime,
            IsSlaBreach = false
        };

        // SLA breach ticket
        var ticket2 = new Ticket
        {
            Title = "Test Ticket 2",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.Resolved,
            CreatedById = user.Id,
            CreatedAt = createdTime,
            ResolvedAt = createdTime.AddHours(2), // Resolved after SLA
            SlaTargetDate = slaTargetTime,
            IsSlaBreach = true
        };

        context.Tickets.AddRange(ticket1, ticket2);
        await context.SaveChangesAsync();

        var controller = new ReportsController(context);

        // Act
        var result = await controller.GetMetrics(createdTime.AddHours(-1), createdTime.AddHours(3));

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var metrics = Assert.IsType<ReportMetricsDto>(okResult.Value);
        
        Assert.Equal(50.0, metrics.SlaCompliancePercentage); // 1 out of 2 tickets compliant = 50%
        Assert.Equal(2, metrics.TotalTickets);
    }

    [Fact]
    public async Task GetTrends_ReturnsCorrectTrendData()
    {
        // Arrange
        using var context = GetInMemoryContext();
        
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Engineer,
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var day1 = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var day2 = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc);

        // Day 1: 2 open tickets
        var ticket1 = new Ticket
        {
            Title = "Ticket 1",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = day1
        };

        var ticket2 = new Ticket
        {
            Title = "Ticket 2",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.InProgress,
            CreatedById = user.Id,
            CreatedAt = day1
        };

        // Day 2: 1 closed ticket
        var ticket3 = new Ticket
        {
            Title = "Ticket 3",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.Closed,
            CreatedById = user.Id,
            CreatedAt = day2
        };

        context.Tickets.AddRange(ticket1, ticket2, ticket3);
        await context.SaveChangesAsync();

        var controller = new ReportsController(context);

        // Act
        var result = await controller.GetTrends(day1, day2);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var trends = Assert.IsType<List<TicketTrendDto>>(okResult.Value);
        
        Assert.Equal(2, trends.Count);
        
        var day1Trend = trends.First(t => t.Date.Date == day1.Date);
        Assert.Equal(2, day1Trend.OpenTickets);
        Assert.Equal(0, day1Trend.ClosedTickets);
        
        var day2Trend = trends.First(t => t.Date.Date == day2.Date);
        Assert.Equal(0, day2Trend.OpenTickets);
        Assert.Equal(1, day2Trend.ClosedTickets);
    }

    [Fact]
    public async Task ExportTickets_ReturnsCSVFile()
    {
        // Arrange
        using var context = GetInMemoryContext();
        
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Engineer,
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var controller = new ReportsController(context);

        // Act
        var result = await controller.ExportTickets();

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.StartsWith("tickets_export_", fileResult.FileDownloadName);
        Assert.Contains("ID,Title,Description", System.Text.Encoding.UTF8.GetString(fileResult.FileContents));
    }
}