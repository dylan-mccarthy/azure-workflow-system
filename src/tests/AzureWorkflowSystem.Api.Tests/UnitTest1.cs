using AzureWorkflowSystem.Api.Data;
using AzureWorkflowSystem.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AzureWorkflowSystem.Api.Tests;

public class DatabaseTests
{
    [Fact]
    public async Task CanCreateUserInDatabase()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new WorkflowDbContext(options);
        await context.Database.EnsureCreatedAsync();

        // Act
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Engineer,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Assert
        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.NotNull(savedUser);
        Assert.Equal("Test", savedUser.FirstName);
        Assert.Equal("User", savedUser.LastName);
        Assert.Equal(UserRole.Engineer, savedUser.Role);
    }

    [Fact]
    public async Task CanCreateTicketWithUser()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new WorkflowDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var user = new User
        {
            Email = "creator@example.com",
            FirstName = "Creator",
            LastName = "User",
            Role = UserRole.Engineer,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "This is a test ticket",
            Priority = TicketPriority.Medium,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        // Assert
        var savedTicket = await context.Tickets
            .Include(t => t.CreatedBy)
            .FirstOrDefaultAsync(t => t.Title == "Test Ticket");
        
        Assert.NotNull(savedTicket);
        Assert.Equal("Test Ticket", savedTicket.Title);
        Assert.Equal(TicketPriority.Medium, savedTicket.Priority);
        Assert.Equal(TicketCategory.Incident, savedTicket.Category);
        Assert.Equal(TicketStatus.New, savedTicket.Status);
        Assert.NotNull(savedTicket.CreatedBy);
        Assert.Equal("Creator", savedTicket.CreatedBy.FirstName);
    }
}