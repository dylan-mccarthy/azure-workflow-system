namespace AzureWorkflowSystem.Api.DTOs;

public class AttachmentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
    public int TicketId { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserDto UploadedBy { get; set; } = null!;
}

public class CreateAttachmentDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
}