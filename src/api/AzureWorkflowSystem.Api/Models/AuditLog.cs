using System.ComponentModel.DataAnnotations;

namespace AzureWorkflowSystem.Api.Models;

public class AuditLog
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;
    
    public string? Details { get; set; }
    
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    
    // Foreign keys
    public int? TicketId { get; set; }
    public int UserId { get; set; }
    
    // Navigation properties
    public Ticket? Ticket { get; set; }
    public User User { get; set; } = null!;
    
    // Timestamp
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}