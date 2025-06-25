using AzureWorkflowSystem.Api.Data;
using AzureWorkflowSystem.Api.DTOs;
using AzureWorkflowSystem.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AzureWorkflowSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly WorkflowDbContext _context;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(WorkflowDbContext context, ILogger<AlertsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Webhook endpoint for Azure Monitor alerts
    /// Creates a ticket automatically from alert payload
    /// </summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = "ApiKey")]
    public async Task<ActionResult> ProcessAlert(AlertWebhookDto alertPayload)
    {
        try
        {
            _logger.LogInformation("Received alert webhook: {AlertId}", alertPayload.Data.Essentials.AlertId);

            // Check if we already have a ticket for this alert
            var existingTicket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.AlertId == alertPayload.Data.Essentials.AlertId);

            if (existingTicket != null)
            {
                _logger.LogInformation("Ticket already exists for alert {AlertId}: Ticket {TicketId}", 
                    alertPayload.Data.Essentials.AlertId, existingTicket.Id);
                
                return Ok(new { ticketId = existingTicket.Id, message = "Ticket already exists for this alert" });
            }

            // Map alert severity to ticket priority
            var priority = MapSeverityToPriority(alertPayload.Data.Essentials.Severity);

            // Get system user for ticket creation
            var systemUser = await _context.Users.FirstAsync(u => u.Id == 1);

            // Create ticket from alert
            var ticket = new Ticket
            {
                Title = $"Alert: {alertPayload.Data.Essentials.AlertRule}",
                Description = BuildAlertDescription(alertPayload),
                Priority = priority,
                Category = TicketCategory.Alert,
                Status = TicketStatus.New,
                AlertId = alertPayload.Data.Essentials.AlertId,
                AzureResourceId = GetPrimaryResourceId(alertPayload.Data.Essentials.TargetResource),
                CreatedById = systemUser.Id,
                CreatedAt = alertPayload.Data.Essentials.FiredDateTime.ToUniversalTime(),
                UpdatedAt = DateTime.UtcNow
            };

            // Calculate SLA target date
            await CalculateSlaTargetDate(ticket);

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            // Log audit entry
            var auditLog = new AuditLog
            {
                Action = "TICKET_CREATED_FROM_ALERT",
                Details = $"Ticket created from Azure Monitor alert: {alertPayload.Data.Essentials.AlertId}",
                TicketId = ticket.Id,
                UserId = systemUser.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created ticket {TicketId} from alert {AlertId}", 
                ticket.Id, alertPayload.Data.Essentials.AlertId);

            return Ok(new { 
                ticketId = ticket.Id, 
                message = "Ticket created successfully from alert",
                alertId = alertPayload.Data.Essentials.AlertId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing alert webhook: {AlertId}", 
                alertPayload.Data?.Essentials?.AlertId ?? "Unknown");
            
            return StatusCode(500, new { message = "Error processing alert" });
        }
    }

    private static TicketPriority MapSeverityToPriority(string severity)
    {
        return severity?.ToLowerInvariant() switch
        {
            "emergency" => TicketPriority.Emergency,
            "sev0" or "critical" => TicketPriority.Critical,
            "sev1" or "error" => TicketPriority.High,
            "sev2" or "warning" => TicketPriority.Medium,
            "sev3" or "informational" => TicketPriority.Low,
            _ => TicketPriority.Medium
        };
    }

    private static string BuildAlertDescription(AlertWebhookDto alert)
    {
        var description = $"Azure Monitor Alert Details:\n\n";
        description += $"Alert Rule: {alert.Data.Essentials.AlertRule}\n";
        description += $"Alert ID: {alert.Data.Essentials.AlertId}\n";
        description += $"Severity: {alert.Data.Essentials.Severity}\n";
        description += $"Signal Type: {alert.Data.Essentials.SignalType}\n";
        description += $"Monitor Condition: {alert.Data.Essentials.MonitorCondition}\n";
        description += $"Fired Date: {alert.Data.Essentials.FiredDateTime:yyyy-MM-dd HH:mm:ss} UTC\n";
        description += $"Description: {alert.Data.Essentials.Description}\n\n";

        if (alert.Data.Essentials.TargetResource?.Length > 0)
        {
            description += "Target Resources:\n";
            foreach (var resource in alert.Data.Essentials.TargetResource)
            {
                description += $"- {resource}\n";
            }
            description += "\n";
        }

        if (alert.Data.AlertContext.Conditions?.Length > 0)
        {
            description += "Alert Conditions:\n";
            foreach (var condition in alert.Data.AlertContext.Conditions)
            {
                description += $"- Metric: {condition.MetricName}\n";
                description += $"  Value: {condition.MetricValue} {condition.MetricUnit}\n";
                description += $"  Threshold: {condition.Threshold}\n";
                description += $"  Operator: {condition.Operator}\n";
                description += $"  Time Aggregation: {condition.TimeAggregation}\n\n";
            }
        }

        return description;
    }

    private static string? GetPrimaryResourceId(string[] targetResources)
    {
        return targetResources?.FirstOrDefault();
    }

    private async Task CalculateSlaTargetDate(Ticket ticket)
    {
        var slaConfig = await _context.SlaConfigurations
            .FirstOrDefaultAsync(s => s.Priority == ticket.Priority && 
                                    s.Category == ticket.Category && 
                                    s.IsActive);

        if (slaConfig != null)
        {
            ticket.SlaTargetDate = ticket.CreatedAt.AddMinutes(slaConfig.ResolutionTimeMinutes);
            ticket.IsSlaBreach = ticket.SlaTargetDate < DateTime.UtcNow;
        }
    }
}