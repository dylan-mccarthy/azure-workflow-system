using System.ComponentModel.DataAnnotations;

namespace AzureWorkflowSystem.Api.Models;

public class SlaConfiguration
{
    public int Id { get; set; }

    [Required]
    public TicketPriority Priority { get; set; }

    [Required]
    public TicketCategory Category { get; set; }

    // SLA times in minutes
    public int ResponseTimeMinutes { get; set; }
    public int ResolutionTimeMinutes { get; set; }

    public bool IsActive { get; set; } = true;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}