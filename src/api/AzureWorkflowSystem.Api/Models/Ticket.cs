using System.ComponentModel.DataAnnotations;

namespace AzureWorkflowSystem.Api.Models;

public class Ticket
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public TicketStatus Status { get; set; } = TicketStatus.New;

    [Required]
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    [Required]
    public TicketCategory Category { get; set; }

    public string? AzureResourceId { get; set; }
    public string? AlertId { get; set; }

    // Foreign keys
    public int CreatedById { get; set; }
    public int? AssignedToId { get; set; }

    // Navigation properties
    public User CreatedBy { get; set; } = null!;
    public User? AssignedTo { get; set; }
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    // SLA tracking
    public DateTime? SlaTargetDate { get; set; }
    public bool IsSlaBreach { get; set; } = false;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}

public enum TicketStatus
{
    New = 1,
    Triaged = 2,
    Assigned = 3,
    InProgress = 4,
    Resolved = 5,
    Closed = 6
}

public enum TicketPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
    Emergency = 5
}

public enum TicketCategory
{
    Incident = 1,
    Access = 2,
    NewResource = 3,
    Change = 4,
    Alert = 5
}