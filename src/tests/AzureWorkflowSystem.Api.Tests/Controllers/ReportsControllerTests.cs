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

    [Fact]
    public async Task ExportAuditLogs_ReturnsCSVFile()
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
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var auditLog = new AuditLog
        {
            TicketId = ticket.Id,
            Action = "Created",
            Details = "Ticket created",
            UserId = user.Id,
            OldValues = "",
            NewValues = "Status: New",
            CreatedAt = DateTime.UtcNow
        };
        context.AuditLogs.Add(auditLog);
        await context.SaveChangesAsync();

        var controller = new ReportsController(context);

        // Act
        var result = await controller.ExportAuditLogs();

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", fileResult.ContentType);
        Assert.StartsWith("audit_logs_export_", fileResult.FileDownloadName);
        Assert.Contains("ID,TicketID,Action,Details", System.Text.Encoding.UTF8.GetString(fileResult.FileContents));
    }

    [Fact]
    public async Task GetMetrics_WithDateFilters_ReturnsFilteredData()
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

        // Ticket within date range
        var ticketInRange = new Ticket
        {
            Title = "In Range Ticket",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc)
        };

        // Ticket outside date range
        var ticketOutOfRange = new Ticket
        {
            Title = "Out of Range Ticket",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = new DateTime(2024, 2, 15, 0, 0, 0, DateTimeKind.Utc)
        };

        context.Tickets.AddRange(ticketInRange, ticketOutOfRange);
        await context.SaveChangesAsync();

        var controller = new ReportsController(context);

        // Act
        var result = await controller.GetMetrics(
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc)
        );

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var metrics = Assert.IsType<ReportMetricsDto>(okResult.Value);

        Assert.Equal(1, metrics.TotalTickets); // Only the ticket in range
    }

    [Fact]
    public async Task GetMetrics_WithPriorityFilter_ReturnsFilteredData()
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

        var highPriorityTicket = new Ticket
        {
            Title = "High Priority Ticket",
            Priority = TicketPriority.High,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        var mediumPriorityTicket = new Ticket
        {
            Title = "Medium Priority Ticket",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.Tickets.AddRange(highPriorityTicket, mediumPriorityTicket);
        await context.SaveChangesAsync();

        var controller = new ReportsController(context);

        // Act
        var result = await controller.GetMetrics(priority: TicketPriority.High);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var metrics = Assert.IsType<ReportMetricsDto>(okResult.Value);

        Assert.Equal(1, metrics.TotalTickets); // Only high priority ticket
    }

    [Fact]
    public async Task GetMetrics_WithCategoryFilter_ReturnsFilteredData()
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

        var incidentTicket = new Ticket
        {
            Title = "Incident Ticket",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        var requestTicket = new Ticket
        {
            Title = "Request Ticket",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Access,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.Tickets.AddRange(incidentTicket, requestTicket);
        await context.SaveChangesAsync();

        var controller = new ReportsController(context);

        // Act
        var result = await controller.GetMetrics(category: TicketCategory.Incident);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var metrics = Assert.IsType<ReportMetricsDto>(okResult.Value);

        Assert.Equal(1, metrics.TotalTickets); // Only incident ticket
    }

    [Fact]
    public async Task GetMetrics_WithNoTickets_ReturnsZeroValues()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var controller = new ReportsController(context);

        // Act
        var result = await controller.GetMetrics();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var metrics = Assert.IsType<ReportMetricsDto>(okResult.Value);

        Assert.Equal(0, metrics.MttaMinutes);
        Assert.Equal(0, metrics.MttrMinutes);
        Assert.Equal(100, metrics.SlaCompliancePercentage); // Default to 100% when no SLA tickets
        Assert.Equal(0, metrics.TotalTickets);
        Assert.Equal(0, metrics.OpenTickets);
        Assert.Equal(0, metrics.ClosedTickets);
    }

    [Fact]
    public async Task GetTrends_WithNoTickets_ReturnsEmptyTrends()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var controller = new ReportsController(context);

        var fromDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var result = await controller.GetTrends(fromDate, toDate);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var trends = Assert.IsType<List<TicketTrendDto>>(okResult.Value);

        Assert.Equal(3, trends.Count); // 3 days
        Assert.All(trends, trend =>
        {
            Assert.Equal(0, trend.OpenTickets);
            Assert.Equal(0, trend.ClosedTickets);
        });
    }

    [Fact]
    public async Task ExportTickets_WithFilters_ReturnsFilteredCSV()
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

        var highPriorityTicket = new Ticket
        {
            Title = "High Priority Ticket",
            Description = "High priority issue",
            Priority = TicketPriority.High,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        var mediumPriorityTicket = new Ticket
        {
            Title = "Medium Priority Ticket",
            Description = "Medium priority issue",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.Tickets.AddRange(highPriorityTicket, mediumPriorityTicket);
        await context.SaveChangesAsync();

        var controller = new ReportsController(context);

        // Act
        var result = await controller.ExportTickets(priority: TicketPriority.High);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        var csvContent = System.Text.Encoding.UTF8.GetString(fileResult.FileContents);

        Assert.Contains("High Priority Ticket", csvContent);
        Assert.DoesNotContain("Medium Priority Ticket", csvContent);
    }

    [Fact]
    public async Task ExportTickets_WithSpecialCharactersInData_EscapesCorrectly()
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
            Title = "Test \"Quoted\" Title",
            Description = "Description with, comma and \"quotes\"",
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
        var csvContent = System.Text.Encoding.UTF8.GetString(fileResult.FileContents);

        Assert.Contains("\"Test \"\"Quoted\"\" Title\"", csvContent); // Escaped quotes
        Assert.Contains("\"Description with, comma and \"\"quotes\"\"\"", csvContent); // Escaped quotes and commas
    }

    [Fact]
    public async Task ExportAuditLogs_WithTicketIdFilter_ReturnsFilteredLogs()
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

        var ticket1 = new Ticket
        {
            Title = "Ticket 1",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        var ticket2 = new Ticket
        {
            Title = "Ticket 2",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.Tickets.AddRange(ticket1, ticket2);
        await context.SaveChangesAsync();

        var auditLog1 = new AuditLog
        {
            TicketId = ticket1.Id,
            Action = "Created Ticket 1",
            Details = "First ticket created",
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        var auditLog2 = new AuditLog
        {
            TicketId = ticket2.Id,
            Action = "Created Ticket 2",
            Details = "Second ticket created",
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.AuditLogs.AddRange(auditLog1, auditLog2);
        await context.SaveChangesAsync();

        var controller = new ReportsController(context);

        // Act
        var result = await controller.ExportAuditLogs(ticketId: ticket1.Id);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        var csvContent = System.Text.Encoding.UTF8.GetString(fileResult.FileContents);

        Assert.Contains("Created Ticket 1", csvContent);
        Assert.DoesNotContain("Created Ticket 2", csvContent);
    }
}