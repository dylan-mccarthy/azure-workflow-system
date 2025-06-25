using AzureWorkflowSystem.Api.Data;
using AzureWorkflowSystem.Api.DTOs;
using AzureWorkflowSystem.Api.Models;
using AzureWorkflowSystem.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AzureWorkflowSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttachmentsController : ControllerBase
{
    private readonly WorkflowDbContext _context;
    private readonly ILogger<AttachmentsController> _logger;
    private readonly IBlobStorageService _blobStorageService;

    public AttachmentsController(WorkflowDbContext context, ILogger<AttachmentsController> logger, IBlobStorageService blobStorageService)
    {
        _context = context;
        _logger = logger;
        _blobStorageService = blobStorageService;
    }

    /// <summary>
    /// Get all attachments for a specific ticket
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AttachmentDto>>> GetTicketAttachments([FromQuery] int ticketId)
    {
        // Verify ticket exists
        if (!await _context.Tickets.AnyAsync(t => t.Id == ticketId))
        {
            return NotFound("Ticket not found");
        }

        var attachments = await _context.Attachments
            .Include(a => a.UploadedBy)
            .Where(a => a.TicketId == ticketId)
            .Select(a => new AttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                ContentType = a.ContentType,
                FileSizeBytes = a.FileSizeBytes,
                BlobUrl = a.BlobUrl,
                TicketId = a.TicketId,
                CreatedAt = a.CreatedAt,
                UploadedBy = new UserDto
                {
                    Id = a.UploadedBy.Id,
                    Email = a.UploadedBy.Email,
                    FirstName = a.UploadedBy.FirstName,
                    LastName = a.UploadedBy.LastName,
                    Role = a.UploadedBy.Role,
                    IsActive = a.UploadedBy.IsActive,
                    CreatedAt = a.UploadedBy.CreatedAt,
                    UpdatedAt = a.UploadedBy.UpdatedAt
                }
            })
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return Ok(attachments);
    }

    /// <summary>
    /// Get a specific attachment by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AttachmentDto>> GetAttachment(int id)
    {
        var attachment = await _context.Attachments
            .Include(a => a.UploadedBy)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attachment == null)
        {
            return NotFound();
        }

        var attachmentDto = new AttachmentDto
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            FileSizeBytes = attachment.FileSizeBytes,
            BlobUrl = attachment.BlobUrl,
            TicketId = attachment.TicketId,
            CreatedAt = attachment.CreatedAt,
            UploadedBy = new UserDto
            {
                Id = attachment.UploadedBy.Id,
                Email = attachment.UploadedBy.Email,
                FirstName = attachment.UploadedBy.FirstName,
                LastName = attachment.UploadedBy.LastName,
                Role = attachment.UploadedBy.Role,
                IsActive = attachment.UploadedBy.IsActive,
                CreatedAt = attachment.UploadedBy.CreatedAt,
                UpdatedAt = attachment.UploadedBy.UpdatedAt
            }
        };

        return Ok(attachmentDto);
    }

    /// <summary>
    /// Download a specific attachment file
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadAttachment(int id)
    {
        var attachment = await _context.Attachments.FindAsync(id);
        if (attachment == null)
        {
            return NotFound();
        }

        try
        {
            var fileStream = await _blobStorageService.DownloadFileAsync(attachment.BlobUrl);

            return File(fileStream, attachment.ContentType, attachment.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download attachment {AttachmentId}", id);
            return StatusCode(500, "Failed to download file");
        }
    }

    /// <summary>
    /// Upload a file attachment to a ticket
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<AttachmentDto>> UploadAttachment(
        [FromQuery] int ticketId,
        IFormFile file)
    {
        // Verify ticket exists
        if (!await _context.Tickets.AnyAsync(t => t.Id == ticketId))
        {
            return NotFound("Ticket not found");
        }

        // Validate file is provided
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided");
        }

        // Validate file size (100 MB limit)
        const long maxFileSizeBytes = 100 * 1024 * 1024; // 100 MB
        if (file.Length > maxFileSizeBytes)
        {
            return StatusCode(413, "File size exceeds 100 MB limit");
        }

        // Validate MIME type is provided
        if (string.IsNullOrEmpty(file.ContentType))
        {
            return BadRequest("File content type is required");
        }

        try
        {
            // Upload file to blob storage
            using var stream = file.OpenReadStream();
            var blobUrl = await _blobStorageService.UploadFileAsync(stream, file.FileName, file.ContentType);

            // For now, use the admin user as the uploader
            // In a real application, this would come from the authenticated user context
            var uploadedBy = await _context.Users.FirstAsync(u => u.Id == 1);

            var attachment = new Attachment
            {
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                BlobUrl = blobUrl,
                TicketId = ticketId,
                UploadedById = uploadedBy.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync();

            // Log audit entry for attachment creation
            var auditLog = new AuditLog
            {
                Action = "ATTACHMENT_UPLOADED",
                Details = $"File '{file.FileName}' uploaded to ticket {ticketId} ({file.Length} bytes)",
                TicketId = ticketId,
                UserId = uploadedBy.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            // Load the attachment with related data for response
            var createdAttachment = await _context.Attachments
                .Include(a => a.UploadedBy)
                .FirstAsync(a => a.Id == attachment.Id);

            var attachmentDto = new AttachmentDto
            {
                Id = createdAttachment.Id,
                FileName = createdAttachment.FileName,
                ContentType = createdAttachment.ContentType,
                FileSizeBytes = createdAttachment.FileSizeBytes,
                BlobUrl = createdAttachment.BlobUrl,
                TicketId = createdAttachment.TicketId,
                CreatedAt = createdAttachment.CreatedAt,
                UploadedBy = new UserDto
                {
                    Id = createdAttachment.UploadedBy.Id,
                    Email = createdAttachment.UploadedBy.Email,
                    FirstName = createdAttachment.UploadedBy.FirstName,
                    LastName = createdAttachment.UploadedBy.LastName,
                    Role = createdAttachment.UploadedBy.Role,
                    IsActive = createdAttachment.UploadedBy.IsActive,
                    CreatedAt = createdAttachment.UploadedBy.CreatedAt,
                    UpdatedAt = createdAttachment.UploadedBy.UpdatedAt
                }
            };

            return CreatedAtAction(nameof(GetAttachment), new { id = attachment.Id }, attachmentDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload attachment {FileName} for ticket {TicketId}", file.FileName, ticketId);
            return StatusCode(500, "Failed to upload file");
        }
    }

    /// <summary>
    /// Create a new attachment for a ticket (for existing blob storage files)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AttachmentDto>> CreateAttachment(
        [FromQuery] int ticketId,
        CreateAttachmentDto createAttachmentDto)
    {
        // Verify ticket exists
        if (!await _context.Tickets.AnyAsync(t => t.Id == ticketId))
        {
            return NotFound("Ticket not found");
        }

        // Validate file size (100 MB limit)
        const long maxFileSizeBytes = 100 * 1024 * 1024; // 100 MB
        if (createAttachmentDto.FileSizeBytes > maxFileSizeBytes)
        {
            return StatusCode(413, "File size exceeds 100 MB limit");
        }

        // For now, use the admin user as the uploader
        // In a real application, this would come from the authenticated user context
        var uploadedBy = await _context.Users.FirstAsync(u => u.Id == 1);

        var attachment = new Attachment
        {
            FileName = createAttachmentDto.FileName,
            ContentType = createAttachmentDto.ContentType,
            FileSizeBytes = createAttachmentDto.FileSizeBytes,
            BlobUrl = createAttachmentDto.BlobUrl,
            TicketId = ticketId,
            UploadedById = uploadedBy.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.Attachments.Add(attachment);
        await _context.SaveChangesAsync();

        // Log audit entry for attachment creation
        var auditLog = new AuditLog
        {
            Action = "ATTACHMENT_CREATED",
            Details = $"Attachment '{createAttachmentDto.FileName}' uploaded to ticket {ticketId} ({createAttachmentDto.FileSizeBytes} bytes)",
            TicketId = ticketId,
            UserId = uploadedBy.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        // Load the attachment with related data for response
        var createdAttachment = await _context.Attachments
            .Include(a => a.UploadedBy)
            .FirstAsync(a => a.Id == attachment.Id);

        var attachmentDto = new AttachmentDto
        {
            Id = createdAttachment.Id,
            FileName = createdAttachment.FileName,
            ContentType = createdAttachment.ContentType,
            FileSizeBytes = createdAttachment.FileSizeBytes,
            BlobUrl = createdAttachment.BlobUrl,
            TicketId = createdAttachment.TicketId,
            CreatedAt = createdAttachment.CreatedAt,
            UploadedBy = new UserDto
            {
                Id = createdAttachment.UploadedBy.Id,
                Email = createdAttachment.UploadedBy.Email,
                FirstName = createdAttachment.UploadedBy.FirstName,
                LastName = createdAttachment.UploadedBy.LastName,
                Role = createdAttachment.UploadedBy.Role,
                IsActive = createdAttachment.UploadedBy.IsActive,
                CreatedAt = createdAttachment.UploadedBy.CreatedAt,
                UpdatedAt = createdAttachment.UploadedBy.UpdatedAt
            }
        };

        return CreatedAtAction(nameof(GetAttachment), new { id = attachment.Id }, attachmentDto);
    }

    /// <summary>
    /// Delete an attachment
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAttachment(int id)
    {
        var attachment = await _context.Attachments.FindAsync(id);
        if (attachment == null)
        {
            return NotFound();
        }

        // Store attachment info for audit log before deletion
        var fileName = attachment.FileName;
        var ticketId = attachment.TicketId;

        _context.Attachments.Remove(attachment);
        await _context.SaveChangesAsync();

        // Log audit entry for attachment deletion
        // For now, use the admin user as the deleter (in real app, get from auth context)
        var deletedBy = await _context.Users.FirstAsync(u => u.Id == 1);
        var auditLog = new AuditLog
        {
            Action = "ATTACHMENT_DELETED",
            Details = $"Attachment '{fileName}' deleted from ticket {ticketId}",
            TicketId = ticketId,
            UserId = deletedBy.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}