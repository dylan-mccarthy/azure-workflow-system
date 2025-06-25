using AzureWorkflowSystem.Api.Controllers;
using AzureWorkflowSystem.Api.Data;
using AzureWorkflowSystem.Api.DTOs;
using AzureWorkflowSystem.Api.Models;
using AzureWorkflowSystem.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AzureWorkflowSystem.Api.Tests.Controllers;

public class AttachmentsControllerTests
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

    private static AttachmentsController GetController(WorkflowDbContext context)
    {
        var logger = new Mock<ILogger<AttachmentsController>>();
        var blobStorageService = new Mock<IBlobStorageService>();
        return new AttachmentsController(context, logger.Object, blobStorageService.Object);
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

    private static async Task<Ticket> CreateTestTicket(WorkflowDbContext context, User user)
    {
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
        return ticket;
    }

    [Fact]
    public async Task GetTicketAttachments_WithValidTicketId_ReturnsAttachments()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);
        var ticket = await CreateTestTicket(context, user);

        var attachment1 = new Attachment
        {
            FileName = "document1.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            BlobUrl = "https://storage.blob.core.windows.net/attachments/doc1.pdf",
            TicketId = ticket.Id,
            UploadedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        var attachment2 = new Attachment
        {
            FileName = "image1.png",
            ContentType = "image/png",
            FileSizeBytes = 2048,
            BlobUrl = "https://storage.blob.core.windows.net/attachments/img1.png",
            TicketId = ticket.Id,
            UploadedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.Attachments.AddRange(attachment1, attachment2);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        // Act
        var result = await controller.GetTicketAttachments(ticket.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var attachments = Assert.IsAssignableFrom<List<AttachmentDto>>(okResult.Value);
        Assert.Equal(2, attachments.Count);
        Assert.Contains(attachments, a => a.FileName == "document1.pdf");
        Assert.Contains(attachments, a => a.FileName == "image1.png");
        Assert.All(attachments, a => Assert.Equal(ticket.Id, a.TicketId));
    }

    [Fact]
    public async Task GetTicketAttachments_WithInvalidTicketId_ReturnsNotFound()
    {
        // Arrange
        using var context = GetDbContext();
        var controller = GetController(context);

        // Act
        var result = await controller.GetTicketAttachments(999);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Ticket not found", notFoundResult.Value);
    }

    [Fact]
    public async Task GetTicketAttachments_WithNoAttachments_ReturnsEmptyList()
    { 
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);
        var ticket = await CreateTestTicket(context, user);

        var controller = GetController(context);

        // Act
        var result = await controller.GetTicketAttachments(ticket.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var attachments = Assert.IsAssignableFrom<List<AttachmentDto>>(okResult.Value);
        Assert.Empty(attachments);
    }

    [Fact]
    public async Task GetAttachment_WithValidId_ReturnsAttachment()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);
        var ticket = await CreateTestTicket(context, user);

        var attachment = new Attachment
        {
            FileName = "test-file.txt",
            ContentType = "text/plain",
            FileSizeBytes = 512,
            BlobUrl = "https://storage.blob.core.windows.net/attachments/test.txt",
            TicketId = ticket.Id,
            UploadedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.Attachments.Add(attachment);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        // Act
        var result = await controller.GetAttachment(attachment.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var attachmentDto = Assert.IsType<AttachmentDto>(okResult.Value);
        Assert.Equal("test-file.txt", attachmentDto.FileName);
        Assert.Equal("text/plain", attachmentDto.ContentType);
        Assert.Equal(512, attachmentDto.FileSizeBytes);
        Assert.Equal("https://storage.blob.core.windows.net/attachments/test.txt", attachmentDto.BlobUrl);
        Assert.Equal(ticket.Id, attachmentDto.TicketId);
        Assert.NotNull(attachmentDto.UploadedBy);
        Assert.Equal(user.Email, attachmentDto.UploadedBy.Email);
    }

    [Fact]
    public async Task GetAttachment_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = GetDbContext();
        var controller = GetController(context);

        // Act
        var result = await controller.GetAttachment(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateAttachment_WithValidData_CreatesAttachment()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);
        var ticket = await CreateTestTicket(context, user);

        var controller = GetController(context);
        var createAttachmentDto = new CreateAttachmentDto
        {
            FileName = "new-document.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 2048,
            BlobUrl = "https://storage.blob.core.windows.net/attachments/new-doc.pdf"
        };

        // Act
        var result = await controller.CreateAttachment(ticket.Id, createAttachmentDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var attachmentDto = Assert.IsType<AttachmentDto>(createdResult.Value);
        Assert.Equal("new-document.pdf", attachmentDto.FileName);
        Assert.Equal("application/pdf", attachmentDto.ContentType);
        Assert.Equal(2048, attachmentDto.FileSizeBytes);
        Assert.Equal("https://storage.blob.core.windows.net/attachments/new-doc.pdf", attachmentDto.BlobUrl);
        Assert.Equal(ticket.Id, attachmentDto.TicketId);

        // Verify attachment was saved to database
        var savedAttachment = await context.Attachments.FirstOrDefaultAsync(a => a.FileName == "new-document.pdf");
        Assert.NotNull(savedAttachment);
        Assert.Equal(ticket.Id, savedAttachment.TicketId);
        Assert.Equal(1, savedAttachment.UploadedById); // Controller hardcodes to user ID 1
    }

    [Fact]
    public async Task CreateAttachment_WithInvalidTicketId_ReturnsNotFound()
    {
        // Arrange
        using var context = GetDbContext();
        var controller = GetController(context);
        var createAttachmentDto = new CreateAttachmentDto
        {
            FileName = "test.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 1024,
            BlobUrl = "https://storage.blob.core.windows.net/attachments/test.pdf"
        };

        // Act
        var result = await controller.CreateAttachment(999, createAttachmentDto);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Ticket not found", notFoundResult.Value);
    }

    [Fact]
    public async Task CreateAttachment_WithOversizedFile_Returns413StatusCode()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);
        var ticket = await CreateTestTicket(context, user);

        var controller = GetController(context);
        var createAttachmentDto = new CreateAttachmentDto
        {
            FileName = "huge-file.zip",
            ContentType = "application/zip",
            FileSizeBytes = 200 * 1024 * 1024, // 200 MB (exceeds 100 MB limit)
            BlobUrl = "https://storage.blob.core.windows.net/attachments/huge.zip"
        };

        // Act
        var result = await controller.CreateAttachment(ticket.Id, createAttachmentDto);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(413, statusCodeResult.StatusCode);
        Assert.Equal("File size exceeds 100 MB limit", statusCodeResult.Value);

        // Verify no attachment was saved
        var attachmentCount = await context.Attachments.CountAsync();
        Assert.Equal(0, attachmentCount);
    }

    [Fact]
    public async Task CreateAttachment_WithMaxAllowedSize_CreatesAttachment()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);
        var ticket = await CreateTestTicket(context, user);

        var controller = GetController(context);
        var createAttachmentDto = new CreateAttachmentDto
        {
            FileName = "max-size-file.zip",
            ContentType = "application/zip",
            FileSizeBytes = 100 * 1024 * 1024, // Exactly 100 MB
            BlobUrl = "https://storage.blob.core.windows.net/attachments/max-size.zip"
        };

        // Act
        var result = await controller.CreateAttachment(ticket.Id, createAttachmentDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var attachmentDto = Assert.IsType<AttachmentDto>(createdResult.Value);
        Assert.Equal("max-size-file.zip", attachmentDto.FileName);
        Assert.Equal(100 * 1024 * 1024, attachmentDto.FileSizeBytes);

        // Verify attachment was saved to database
        var savedAttachment = await context.Attachments.FirstOrDefaultAsync(a => a.FileName == "max-size-file.zip");
        Assert.NotNull(savedAttachment);
    }

    [Fact]
    public async Task CreateAttachment_SetsCorrectTimestamp()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);
        var ticket = await CreateTestTicket(context, user);

        var controller = GetController(context);
        var createAttachmentDto = new CreateAttachmentDto
        {
            FileName = "timestamp-test.txt",
            ContentType = "text/plain",
            FileSizeBytes = 1024,
            BlobUrl = "https://storage.blob.core.windows.net/attachments/timestamp.txt"
        };

        var beforeCreate = DateTime.UtcNow;

        // Act
        var result = await controller.CreateAttachment(ticket.Id, createAttachmentDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var attachmentDto = Assert.IsType<AttachmentDto>(createdResult.Value);
        
        Assert.True(attachmentDto.CreatedAt >= beforeCreate);
        Assert.True(attachmentDto.CreatedAt <= DateTime.UtcNow);

        // Verify in database
        var savedAttachment = await context.Attachments.FindAsync(attachmentDto.Id);
        Assert.NotNull(savedAttachment);
        Assert.True(savedAttachment.CreatedAt >= beforeCreate);
        Assert.True(savedAttachment.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task DeleteAttachment_WithValidId_DeletesAttachment()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);
        var ticket = await CreateTestTicket(context, user);

        var attachment = new Attachment
        {
            FileName = "delete-me.txt",
            ContentType = "text/plain",
            FileSizeBytes = 512,
            BlobUrl = "https://storage.blob.core.windows.net/attachments/delete-me.txt",
            TicketId = ticket.Id,
            UploadedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.Attachments.Add(attachment);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        // Act
        var result = await controller.DeleteAttachment(attachment.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify attachment was deleted from database
        var deletedAttachment = await context.Attachments.FindAsync(attachment.Id);
        Assert.Null(deletedAttachment);
    }

    [Fact]
    public async Task DeleteAttachment_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        using var context = GetDbContext();
        var controller = GetController(context);

        // Act
        var result = await controller.DeleteAttachment(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetTicketAttachments_OrdersByCreatedAtDescending()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);
        var ticket = await CreateTestTicket(context, user);

        var oldAttachment = new Attachment
        {
            FileName = "old-file.txt",
            ContentType = "text/plain",
            FileSizeBytes = 512,
            BlobUrl = "https://storage.blob.core.windows.net/attachments/old.txt",
            TicketId = ticket.Id,
            UploadedById = user.Id,
            CreatedAt = DateTime.UtcNow.AddHours(-1)
        };

        var newAttachment = new Attachment
        {
            FileName = "new-file.txt",
            ContentType = "text/plain",
            FileSizeBytes = 512,
            BlobUrl = "https://storage.blob.core.windows.net/attachments/new.txt",
            TicketId = ticket.Id,
            UploadedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.Attachments.AddRange(oldAttachment, newAttachment);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        // Act
        var result = await controller.GetTicketAttachments(ticket.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var attachments = Assert.IsAssignableFrom<List<AttachmentDto>>(okResult.Value);
        Assert.Equal(2, attachments.Count);
        
        // Verify ordering (newest first)
        Assert.Equal("new-file.txt", attachments[0].FileName);
        Assert.Equal("old-file.txt", attachments[1].FileName);
        Assert.True(attachments[0].CreatedAt > attachments[1].CreatedAt);
    }

    [Fact]
    public async Task GetTicketAttachments_IncludesUploadedByUser()
    {
        // Arrange
        using var context = GetDbContext();
        var user1 = await CreateTestUser(context, "user1@test.com");
        var user2 = await CreateTestUser(context, "user2@test.com");
        var ticket = await CreateTestTicket(context, user1);

        var attachment1 = new Attachment
        {
            FileName = "file1.txt",
            ContentType = "text/plain",
            FileSizeBytes = 512,
            BlobUrl = "https://storage.blob.core.windows.net/attachments/file1.txt",
            TicketId = ticket.Id,
            UploadedById = user1.Id,
            CreatedAt = DateTime.UtcNow
        };

        var attachment2 = new Attachment
        {
            FileName = "file2.txt",
            ContentType = "text/plain",
            FileSizeBytes = 512,
            BlobUrl = "https://storage.blob.core.windows.net/attachments/file2.txt",
            TicketId = ticket.Id,
            UploadedById = user2.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.Attachments.AddRange(attachment1, attachment2);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        // Act
        var result = await controller.GetTicketAttachments(ticket.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var attachments = Assert.IsAssignableFrom<List<AttachmentDto>>(okResult.Value);
        Assert.Equal(2, attachments.Count);

        var attachment1Dto = attachments.First(a => a.FileName == "file1.txt");
        var attachment2Dto = attachments.First(a => a.FileName == "file2.txt");

        Assert.NotNull(attachment1Dto.UploadedBy);
        Assert.Equal("user1@test.com", attachment1Dto.UploadedBy.Email);
        
        Assert.NotNull(attachment2Dto.UploadedBy);
        Assert.Equal("user2@test.com", attachment2Dto.UploadedBy.Email);
    }

    [Fact]
    public async Task CreateAttachment_CreatesAuditLogEntry()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);
        var ticket = await CreateTestTicket(context, user);

        var controller = GetController(context);
        var createAttachmentDto = new CreateAttachmentDto
        {
            FileName = "audit-test.pdf",
            ContentType = "application/pdf",
            FileSizeBytes = 2048,
            BlobUrl = "https://storage.blob.core.windows.net/attachments/audit-test.pdf"
        };

        // Act
        var result = await controller.CreateAttachment(ticket.Id, createAttachmentDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var attachmentDto = Assert.IsType<AttachmentDto>(createdResult.Value);

        // Verify audit log entry was created
        var auditLog = await context.AuditLogs
            .Where(a => a.Action == "ATTACHMENT_CREATED" && a.TicketId == ticket.Id)
            .FirstOrDefaultAsync();

        Assert.NotNull(auditLog);
        Assert.Equal("ATTACHMENT_CREATED", auditLog.Action);
        Assert.Contains("audit-test.pdf", auditLog.Details);
        Assert.Contains(ticket.Id.ToString(), auditLog.Details);
        Assert.Contains("2048 bytes", auditLog.Details);
        Assert.Equal(ticket.Id, auditLog.TicketId);
        Assert.Equal(1, auditLog.UserId); // Admin user ID
    }

    [Fact]
    public async Task DeleteAttachment_CreatesAuditLogEntry()
    {
        // Arrange
        using var context = GetDbContext();
        var user = await CreateTestUser(context);
        var ticket = await CreateTestTicket(context, user);

        var attachment = new Attachment
        {
            FileName = "delete-audit.txt",
            ContentType = "text/plain",
            FileSizeBytes = 512,
            BlobUrl = "https://storage.blob.core.windows.net/attachments/delete-audit.txt",
            TicketId = ticket.Id,
            UploadedById = user.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.Attachments.Add(attachment);
        await context.SaveChangesAsync();

        var controller = GetController(context);

        // Act
        var result = await controller.DeleteAttachment(attachment.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify audit log entry was created
        var auditLog = await context.AuditLogs
            .Where(a => a.Action == "ATTACHMENT_DELETED" && a.TicketId == ticket.Id)
            .FirstOrDefaultAsync();

        Assert.NotNull(auditLog);
        Assert.Equal("ATTACHMENT_DELETED", auditLog.Action);
        Assert.Contains("delete-audit.txt", auditLog.Details);
        Assert.Contains(ticket.Id.ToString(), auditLog.Details);
        Assert.Equal(ticket.Id, auditLog.TicketId);
        Assert.Equal(1, auditLog.UserId); // Admin user ID
    }
}