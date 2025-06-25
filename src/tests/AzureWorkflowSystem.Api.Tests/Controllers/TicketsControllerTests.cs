using AzureWorkflowSystem.Api.Controllers;
using AzureWorkflowSystem.Api.Data;
using AzureWorkflowSystem.Api.DTOs;
using AzureWorkflowSystem.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AzureWorkflowSystem.Api.Tests.Controllers;

public class TicketsControllerTests
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

    private static TicketsController GetController(WorkflowDbContext context)
    {
        var logger = new Mock<ILogger<TicketsController>>();
        return new TicketsController(context, logger.Object);
    }

    private static async Task<User> CreateTestUser(WorkflowDbContext context, string email = "test@test.com")
    {
        var user = new User
        {
            Email = email,
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Engineer,
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
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            ResolutionTimeMinutes = 240, // 4 hours
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.SlaConfigurations.Add(slaConfig);
        await context.SaveChangesAsync();
        return slaConfig;
    }

    [Fact]
    public async Task GetTickets_ReturnsAllTickets()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);

        var ticket1 = new Ticket
        {
            Title = "Ticket 1",
            Description = "Description 1",
            Priority = TicketPriority.High,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var ticket2 = new Ticket  
        {
            Title = "Ticket 2",
            Description = "Description 2",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.NewResource,
            Status = TicketStatus.Assigned,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Tickets.AddRange(ticket1, ticket2);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        // Act
        var result = await controller.GetTickets();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tickets = Assert.IsAssignableFrom<List<TicketDto>>(okResult.Value);
        Assert.Equal(2, tickets.Count);
        Assert.Contains(tickets, t => t.Title == "Ticket 1");
        Assert.Contains(tickets, t => t.Title == "Ticket 2");
    }

    [Fact]
    public async Task GetTickets_WithStatusFilter_ReturnsFilteredTickets()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);

        var newTicket = new Ticket
        {
            Title = "New Ticket",
            Description = "Description",
            Priority = TicketPriority.High,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var assignedTicket = new Ticket  
        {
            Title = "Assigned Ticket",
            Description = "Description",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.NewResource,
            Status = TicketStatus.Assigned,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Tickets.AddRange(newTicket, assignedTicket);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        // Act
        var result = await controller.GetTickets(status: TicketStatus.New);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tickets = Assert.IsAssignableFrom<List<TicketDto>>(okResult.Value);
        Assert.Single(tickets);
        Assert.Equal("New Ticket", tickets[0].Title);
        Assert.Equal(TicketStatus.New, tickets[0].Status);
    }

    [Fact]
    public async Task GetTickets_WithPriorityFilter_ReturnsFilteredTickets()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);

        var highPriorityTicket = new Ticket
        {
            Title = "High Priority Ticket",
            Description = "Description",
            Priority = TicketPriority.High,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var lowPriorityTicket = new Ticket  
        {
            Title = "Low Priority Ticket",
            Description = "Description",
            Priority = TicketPriority.Low,
            Category = TicketCategory.NewResource,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Tickets.AddRange(highPriorityTicket, lowPriorityTicket);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        // Act
        var result = await controller.GetTickets(priority: TicketPriority.High);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var tickets = Assert.IsAssignableFrom<List<TicketDto>>(okResult.Value);
        Assert.Single(tickets);
        Assert.Equal("High Priority Ticket", tickets[0].Title);
        Assert.Equal(TicketPriority.High, tickets[0].Priority);
    }

    [Fact]
    public async Task GetTicket_WithValidId_ReturnsTicket()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        // Act
        var result = await controller.GetTicket(ticket.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var ticketDto = Assert.IsType<TicketDto>(okResult.Value);
        Assert.Equal("Test Ticket", ticketDto.Title);
        Assert.Equal("Test Description", ticketDto.Description);
        Assert.Equal(TicketPriority.Medium, ticketDto.Priority);
        Assert.Equal(TicketCategory.Incident, ticketDto.Category);
        Assert.Equal(TicketStatus.New, ticketDto.Status);
    }

    [Fact]
    public async Task GetTicket_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = GetDbContext();
        var controller = GetController(context);

        // Act
        var result = await controller.GetTicket(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateTicket_WithValidData_CreatesTicket()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);
        var slaConfig = await CreateTestSlaConfiguration(context);

        var controller = GetController(context);
        var createTicketDto = new CreateTicketDto
        {
            Title = "New Ticket",
            Description = "New ticket description",
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident,
            AzureResourceId = "test-resource",
            AlertId = "test-alert"
        };

        // Act
        var result = await controller.CreateTicket(createTicketDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var ticketDto = Assert.IsType<TicketDto>(createdResult.Value);
        Assert.Equal("New Ticket", ticketDto.Title);
        Assert.Equal("New ticket description", ticketDto.Description);
        Assert.Equal(TicketPriority.Emergency, ticketDto.Priority);
        Assert.Equal(TicketCategory.Incident, ticketDto.Category);
        Assert.Equal(TicketStatus.New, ticketDto.Status);
        Assert.Equal("test-resource", ticketDto.AzureResourceId);
        Assert.Equal("test-alert", ticketDto.AlertId);
        Assert.NotNull(ticketDto.SlaTargetDate);

        // Verify ticket was saved to database
        var savedTicket = await context.Tickets.FirstOrDefaultAsync(t => t.Title == "New Ticket");
        Assert.NotNull(savedTicket);
        Assert.Equal("New ticket description", savedTicket.Description);
    }

    [Fact]
    public async Task CreateTicket_WithSlaConfiguration_SetsSlaTargetDate()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);
        var slaConfig = await CreateTestSlaConfiguration(context);

        var controller = GetController(context);
        var createTicketDto = new CreateTicketDto
        {
            Title = "SLA Test Ticket",
            Description = "Testing SLA calculation",
            Priority = TicketPriority.Emergency,
            Category = TicketCategory.Incident
        };

        // Act
        var result = await controller.CreateTicket(createTicketDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var ticketDto = Assert.IsType<TicketDto>(createdResult.Value);
        
        Assert.NotNull(ticketDto.SlaTargetDate);
        // SLA target should be 4 hours (240 minutes) from creation time - allow for small timing differences
        var expectedSlaDate = ticketDto.CreatedAt.AddMinutes(240);
        Assert.True(Math.Abs((ticketDto.SlaTargetDate.Value - expectedSlaDate).TotalSeconds) < 60);
    }

    [Fact]
    public async Task UpdateTicket_WithValidData_UpdatesTicket()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);

        var ticket = new Ticket
        {
            Title = "Original Title",
            Description = "Original Description",
            Priority = TicketPriority.Low,
            Category = TicketCategory.NewResource,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var controller = GetController(context);
        var updateTicketDto = new UpdateTicketDto
        {
            Title = "Updated Title",
            Description = "Updated Description",
            Priority = TicketPriority.High,
            Status = TicketStatus.InProgress
        };

        // Act
        var result = await controller.UpdateTicket(ticket.Id, updateTicketDto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify ticket was updated in database
        var updatedTicket = await context.Tickets.FindAsync(ticket.Id);
        Assert.NotNull(updatedTicket);
        Assert.Equal("Updated Title", updatedTicket.Title);
        Assert.Equal("Updated Description", updatedTicket.Description);
        Assert.Equal(TicketPriority.High, updatedTicket.Priority);
        Assert.Equal(TicketStatus.InProgress, updatedTicket.Status);
        Assert.True(updatedTicket.UpdatedAt >= ticket.CreatedAt); // Use CreatedAt as baseline
    }

    [Fact]
    public async Task UpdateTicket_StatusToResolved_SetsResolvedAt()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Description",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.InProgress,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var controller = GetController(context);
        var updateTicketDto = new UpdateTicketDto
        {
            Status = TicketStatus.Resolved
        };

        // Act
        var result = await controller.UpdateTicket(ticket.Id, updateTicketDto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify ResolvedAt timestamp was set
        var updatedTicket = await context.Tickets.FindAsync(ticket.Id);
        Assert.NotNull(updatedTicket);
        Assert.Equal(TicketStatus.Resolved, updatedTicket.Status);
        Assert.NotNull(updatedTicket.ResolvedAt);
        Assert.True(updatedTicket.ResolvedAt.Value <= DateTime.UtcNow);
    }

    [Fact]
    public async Task UpdateTicket_StatusToClosed_SetsClosedAt()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Description",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.Resolved,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var controller = GetController(context);
        var updateTicketDto = new UpdateTicketDto
        {
            Status = TicketStatus.Closed
        };

        // Act
        var result = await controller.UpdateTicket(ticket.Id, updateTicketDto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify ClosedAt timestamp was set
        var updatedTicket = await context.Tickets.FindAsync(ticket.Id);
        Assert.NotNull(updatedTicket);
        Assert.Equal(TicketStatus.Closed, updatedTicket.Status);
        Assert.NotNull(updatedTicket.ClosedAt);
        Assert.True(updatedTicket.ClosedAt.Value <= DateTime.UtcNow);
    }

    [Fact]
    public async Task AssignTicket_WithValidData_AssignsTicket()
    {
        // Arrange
        using var context = GetDbContext();
        var creator = await CreateTestUser(context, "creator@test.com");
        var assignee = await CreateTestUser(context, "assignee@test.com");

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Description",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = creator.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var controller = GetController(context);
        var assignTicketDto = new AssignTicketDto
        {
            AssignedToId = assignee.Id
        };

        // Act
        var result = await controller.AssignTicket(ticket.Id, assignTicketDto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify ticket was assigned
        var assignedTicket = await context.Tickets.FindAsync(ticket.Id);
        Assert.NotNull(assignedTicket);
        Assert.Equal(assignee.Id, assignedTicket.AssignedToId);
        Assert.Equal(TicketStatus.Assigned, assignedTicket.Status);
        Assert.True(assignedTicket.UpdatedAt >= ticket.CreatedAt); // Use CreatedAt as baseline
    }

    [Fact]
    public async Task AssignTicket_WithInvalidAssignee_ReturnsBadRequest()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Description",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var controller = GetController(context);
        var assignTicketDto = new AssignTicketDto
        {
            AssignedToId = 999 // Non-existent user
        };

        // Act
        var result = await controller.AssignTicket(ticket.Id, assignTicketDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Assigned user does not exist", badRequestResult.Value);
    }

    [Fact]
    public async Task AssignTicket_WithNullAssignee_UnassignsTicket()
    {
        // Arrange
        using var context = GetDbContext();
        var creator = await CreateTestUser(context, "creator@test.com");
        var assignee = await CreateTestUser(context, "assignee@test.com");

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Description",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.Assigned,
            CreatedById = creator.Id,
            AssignedToId = assignee.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var controller = GetController(context);
        var assignTicketDto = new AssignTicketDto
        {
            AssignedToId = null
        };

        // Act
        var result = await controller.AssignTicket(ticket.Id, assignTicketDto);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify ticket was unassigned
        var unassignedTicket = await context.Tickets.FindAsync(ticket.Id);
        Assert.NotNull(unassignedTicket);
        Assert.Null(unassignedTicket.AssignedToId);
    }

    [Fact]
    public async Task DeleteTicket_WithValidId_DeletesTicket()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);

        var ticket = new Ticket
        {
            Title = "Delete Me",
            Description = "Description",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        // Act
        var result = await controller.DeleteTicket(ticket.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify ticket was deleted
        var deletedTicket = await context.Tickets.FindAsync(ticket.Id);
        Assert.Null(deletedTicket);
    }

    [Fact]
    public async Task DeleteTicket_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = GetDbContext();
        var controller = GetController(context);

        // Act
        var result = await controller.DeleteTicket(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}