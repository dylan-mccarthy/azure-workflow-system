using System.ComponentModel.DataAnnotations;

namespace AzureWorkflowSystem.Api.Models;

public class Attachment
{
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    [Required]
    public string BlobUrl { get; set; } = string.Empty;

    // Foreign key
    public int TicketId { get; set; }

    // Navigation property
    public Ticket Ticket { get; set; } = null!;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int UploadedById { get; set; }
    public User UploadedBy { get; set; } = null!;
}