using System.ComponentModel.DataAnnotations;
using AzureWorkflowSystem.Api.Models;

namespace AzureWorkflowSystem.Api.DTOs;

public class SlaConfigurationDto
{
    public int Id { get; set; }
    public TicketPriority Priority { get; set; }
    public TicketCategory Category { get; set; }
    public int ResponseTimeMinutes { get; set; }
    public int ResolutionTimeMinutes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateSlaConfigurationDto
{
    [Required]
    public TicketPriority Priority { get; set; }
    
    [Required]
    public TicketCategory Category { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Response time must be greater than 0")]
    public int ResponseTimeMinutes { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Resolution time must be greater than 0")]
    public int ResolutionTimeMinutes { get; set; }
    
    public bool IsActive { get; set; } = true;
}

public class UpdateSlaConfigurationDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Response time must be greater than 0")]
    public int? ResponseTimeMinutes { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Resolution time must be greater than 0")]
    public int? ResolutionTimeMinutes { get; set; }
    
    public bool? IsActive { get; set; }
}