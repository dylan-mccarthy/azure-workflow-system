using AzureWorkflowSystem.Api.Data;
using AzureWorkflowSystem.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AzureWorkflowSystem.Api.Tests.Integration;

public class BasicIntegrationTests
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
    public async Task DatabaseSchema_CanCreateAllEntities()
    {
        // Arrange
        using var context = GetDbContext();

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

        var ticket = new Ticket
        {
            Title = "Test Ticket",
            Description = "Test Description",
            Priority = TicketPriority.High,
            Category = TicketCategory.Incident,
            Status = TicketStatus.New,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        var attachment = new Attachment
        {
            FileName = "test.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            BlobUrl = "https://storage.example.com/test.pdf",
            TicketId = ticket.Id,
            UploadedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.Attachments.Add(attachment);
        await context.SaveChangesAsync();

        var auditLog = new AuditLog
        {
            Action = "TEST_ACTION",
            Details = "Test audit log",
            TicketId = ticket.Id,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.AuditLogs.Add(auditLog);
        await context.SaveChangesAsync();

        var slaConfig = new SlaConfiguration
        {
            Priority = TicketPriority.High,
            Category = TicketCategory.Incident,
            ResolutionTimeMinutes = 120,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.SlaConfigurations.Add(slaConfig);
        await context.SaveChangesAsync();

        // Act & Assert - Verify all entities are created and can be queried
        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.NotNull(savedUser);

        var savedTicket = await context.Tickets
            .Include(t => t.CreatedBy)
            .Include(t => t.Attachments)
            .Include(t => t.AuditLogs)
            .FirstOrDefaultAsync(t => t.Title == "Test Ticket");
        Assert.NotNull(savedTicket);
        Assert.Equal(user.Id, savedTicket.CreatedById);
        Assert.Single(savedTicket.Attachments);
        Assert.Single(savedTicket.AuditLogs);

        var savedAttachment = await context.Attachments
            .Include(a => a.UploadedBy)
            .FirstOrDefaultAsync(a => a.FileName == "test.pdf");
        Assert.NotNull(savedAttachment);
        Assert.Equal(ticket.Id, savedAttachment.TicketId);

        var savedAuditLog = await context.AuditLogs.FirstOrDefaultAsync(a => a.Action == "TEST_ACTION");
        Assert.NotNull(savedAuditLog);

        var savedSlaConfig = await context.SlaConfigurations.FirstOrDefaultAsync(s => s.ResolutionTimeMinutes == 120);
        Assert.NotNull(savedSlaConfig);
        Assert.True(savedSlaConfig.IsActive);
    }

    [Fact]
    public async Task UserRoles_AllEnumValuesWork()
    {
        // Arrange
        using var context = GetDbContext();

        var roles = new[] { UserRole.Viewer, UserRole.Engineer, UserRole.Manager, UserRole.Admin };
        var users = new List<User>();

        foreach (var role in roles)
        {
            var user = new User
            {
                Email = $"{role.ToString().ToLower()}@example.com",
                FirstName = role.ToString(),
                LastName = "User",
                Role = role,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            users.Add(user);
            context.Users.Add(user);
        }

        await context.SaveChangesAsync();

        // Act & Assert
        foreach (var role in roles)
        {
            var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Role == role);
            Assert.NotNull(savedUser);
            Assert.Equal(role, savedUser.Role);
        }
    }

    [Fact]
    public async Task TicketStatuses_AllEnumValuesWork()
    {
        // Arrange
        using var context = GetDbContext();

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

        var statuses = new[] { TicketStatus.New, TicketStatus.Triaged, TicketStatus.Assigned, 
                              TicketStatus.InProgress, TicketStatus.Resolved, TicketStatus.Closed };

        foreach (var status in statuses)
        {
            var ticket = new Ticket
            {
                Title = $"Ticket {status}",
                Description = $"Ticket with status {status}",
                Priority = TicketPriority.Medium,
                Category = TicketCategory.Incident,
                Status = status,
                CreatedById = user.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Tickets.Add(ticket);
        }

        await context.SaveChangesAsync();

        // Act & Assert
        foreach (var status in statuses)
        {
            var savedTicket = await context.Tickets.FirstOrDefaultAsync(t => t.Status == status);
            Assert.NotNull(savedTicket);
            Assert.Equal(status, savedTicket.Status);
        }
    }

    [Fact]
    public async Task TicketPriorities_AllEnumValuesWork()
    {
        // Arrange
        using var context = GetDbContext();

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

        var priorities = new[] { TicketPriority.Low, TicketPriority.Medium, TicketPriority.High, 
                                TicketPriority.Critical, TicketPriority.Emergency };

        foreach (var priority in priorities)
        {
            var ticket = new Ticket
            {
                Title = $"Ticket {priority}",
                Description = $"Ticket with priority {priority}",
                Priority = priority,
                Category = TicketCategory.Incident,
                Status = TicketStatus.New,
                CreatedById = user.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Tickets.Add(ticket);
        }

        await context.SaveChangesAsync();

        // Act & Assert
        foreach (var priority in priorities)
        {
            var savedTicket = await context.Tickets.FirstOrDefaultAsync(t => t.Priority == priority);
            Assert.NotNull(savedTicket);
            Assert.Equal(priority, savedTicket.Priority);
        }
    }

    [Fact]
    public async Task TicketCategories_AllEnumValuesWork()
    {
        // Arrange
        using var context = GetDbContext();

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

        var categories = new[] { TicketCategory.Incident, TicketCategory.Access, TicketCategory.NewResource, 
                                TicketCategory.Change, TicketCategory.Alert };

        foreach (var category in categories)
        {
            var ticket = new Ticket
            {
                Title = $"Ticket {category}",
                Description = $"Ticket with category {category}",
                Priority = TicketPriority.Medium,
                Category = category,
                Status = TicketStatus.New,
                CreatedById = user.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Tickets.Add(ticket);
        }

        await context.SaveChangesAsync();

        // Act & Assert
        foreach (var category in categories)
        {
            var savedTicket = await context.Tickets.FirstOrDefaultAsync(t => t.Category == category);
            Assert.NotNull(savedTicket);
            Assert.Equal(category, savedTicket.Category);
        }
    }
}