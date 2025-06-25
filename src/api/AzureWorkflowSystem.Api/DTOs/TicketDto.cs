using AzureWorkflowSystem.Api.Models;

namespace AzureWorkflowSystem.Api.DTOs;

public class TicketDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
    public TicketCategory Category { get; set; }
    public string? AzureResourceId { get; set; }
    public string? AlertId { get; set; }
    
    public UserDto CreatedBy { get; set; } = null!;
    public UserDto? AssignedTo { get; set; }
    
    public DateTime? SlaTargetDate { get; set; }
    public bool IsSlaBreach { get; set; }
    public bool IsImminentSlaBreach { get; set; }
    public int? SlaRemainingMinutes { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    
    public int AttachmentCount { get; set; }
}

public class CreateTicketDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public TicketCategory Category { get; set; }
    public string? AzureResourceId { get; set; }
    public string? AlertId { get; set; }
    public int? AssignedToId { get; set; }
}

public class UpdateTicketDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public TicketStatus? Status { get; set; }
    public TicketPriority? Priority { get; set; }
    public TicketCategory? Category { get; set; }
    public string? AzureResourceId { get; set; }
    public string? AlertId { get; set; }
}

public class AssignTicketDto
{
    public int? AssignedToId { get; set; }
}