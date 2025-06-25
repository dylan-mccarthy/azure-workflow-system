using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text;
using AzureWorkflowSystem.Api.Data;
using AzureWorkflowSystem.Api.DTOs;
using AzureWorkflowSystem.Api.Models;

namespace AzureWorkflowSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly WorkflowDbContext _context;

    public ReportsController(WorkflowDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get reporting metrics including MTTA, MTTR, and SLA compliance
    /// </summary>
    [HttpGet("metrics")]
    public async Task<ActionResult<ReportMetricsDto>> GetMetrics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] TicketPriority? priority = null,
        [FromQuery] TicketCategory? category = null)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var query = _context.Tickets
            .Include(t => t.AuditLogs)
            .Where(t => t.CreatedAt >= from && t.CreatedAt <= to);

        if (priority.HasValue)
            query = query.Where(t => t.Priority == priority.Value);

        if (category.HasValue)
            query = query.Where(t => t.Category == category.Value);

        var tickets = await query.ToListAsync();

        // Calculate MTTA (Mean Time To Acknowledgment)
        var acknowledgedTickets = tickets.Where(t => t.AssignedToId.HasValue).ToList();
        var mttaMinutes = acknowledgedTickets.Any() 
            ? acknowledgedTickets.Average(t => (t.UpdatedAt - t.CreatedAt).TotalMinutes)
            : 0;

        // Calculate MTTR (Mean Time To Resolution)
        var resolvedTickets = tickets.Where(t => t.ResolvedAt.HasValue).ToList();
        var mttrMinutes = resolvedTickets.Any()
            ? resolvedTickets.Average(t => (t.ResolvedAt!.Value - t.CreatedAt).TotalMinutes)
            : 0;

        // Calculate SLA Compliance
        var ticketsWithSla = tickets.Where(t => t.SlaTargetDate.HasValue).ToList();
        var slaCompliantTickets = ticketsWithSla.Where(t => 
            !t.IsSlaBreach && (t.ResolvedAt == null || t.ResolvedAt <= t.SlaTargetDate)).ToList();
        var slaCompliancePercentage = ticketsWithSla.Any()
            ? (double)slaCompliantTickets.Count / ticketsWithSla.Count * 100
            : 100;

        var openTickets = tickets.Count(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved);
        var closedTickets = tickets.Count(t => t.Status == TicketStatus.Closed || t.Status == TicketStatus.Resolved);

        return Ok(new ReportMetricsDto
        {
            MttaMinutes = mttaMinutes,
            MttrMinutes = mttrMinutes,
            SlaCompliancePercentage = slaCompliancePercentage,
            TotalTickets = tickets.Count,
            OpenTickets = openTickets,
            ClosedTickets = closedTickets,
            FromDate = from,
            ToDate = to
        });
    }

    /// <summary>
    /// Get ticket trend data for charts
    /// </summary>
    [HttpGet("trends")]
    public async Task<ActionResult<IEnumerable<TicketTrendDto>>> GetTrends(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string groupBy = "day")
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var tickets = await _context.Tickets
            .Where(t => t.CreatedAt >= from && t.CreatedAt <= to)
            .ToListAsync();

        var trends = new List<TicketTrendDto>();
        var current = from.Date;

        while (current <= to.Date)
        {
            var dayTickets = tickets.Where(t => t.CreatedAt.Date == current).ToList();
            trends.Add(new TicketTrendDto
            {
                Date = current,
                OpenTickets = dayTickets.Count(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved),
                ClosedTickets = dayTickets.Count(t => t.Status == TicketStatus.Closed || t.Status == TicketStatus.Resolved)
            });
            current = current.AddDays(1);
        }

        return Ok(trends);
    }

    /// <summary>
    /// Export tickets as CSV
    /// </summary>
    [HttpGet("export/tickets")]
    public async Task<IActionResult> ExportTickets(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] TicketPriority? priority = null,
        [FromQuery] TicketCategory? category = null,
        [FromQuery] TicketStatus? status = null)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var query = _context.Tickets
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .Where(t => t.CreatedAt >= from && t.CreatedAt <= to);

        if (priority.HasValue)
            query = query.Where(t => t.Priority == priority.Value);

        if (category.HasValue)
            query = query.Where(t => t.Category == category.Value);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        var tickets = await query.OrderBy(t => t.CreatedAt).ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("ID,Title,Description,Status,Priority,Category,CreatedBy,AssignedTo,SLA_Target,SLA_Breach,Created,Updated,Resolved,Closed");

        foreach (var ticket in tickets)
        {
            csv.AppendLine($"{ticket.Id}," +
                          $"\"{ticket.Title.Replace("\"", "\"\"")}\","+
                          $"\"{ticket.Description?.Replace("\"", "\"\"") ?? ""}\","+
                          $"{ticket.Status}," +
                          $"{ticket.Priority}," +
                          $"{ticket.Category}," +
                          $"\"{ticket.CreatedBy.FirstName} {ticket.CreatedBy.LastName}\"," +
                          $"\"{(ticket.AssignedTo != null ? $"{ticket.AssignedTo.FirstName} {ticket.AssignedTo.LastName}" : "")}\","+
                          $"{ticket.SlaTargetDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""}," +
                          $"{ticket.IsSlaBreach}," +
                          $"{ticket.CreatedAt:yyyy-MM-dd HH:mm:ss}," +
                          $"{ticket.UpdatedAt:yyyy-MM-dd HH:mm:ss}," +
                          $"{ticket.ResolvedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""}," +
                          $"{ticket.ClosedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""}");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"tickets_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        return File(bytes, "text/csv", fileName);
    }

    /// <summary>
    /// Export audit logs as CSV
    /// </summary>
    [HttpGet("export/audit-logs")]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int? ticketId = null)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var query = _context.AuditLogs
            .Include(a => a.User)
            .Include(a => a.Ticket)
            .Where(a => a.CreatedAt >= from && a.CreatedAt <= to);

        if (ticketId.HasValue)
            query = query.Where(a => a.TicketId == ticketId.Value);

        var auditLogs = await query.OrderBy(a => a.CreatedAt).ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("ID,TicketID,Action,Details,User,OldValues,NewValues,Created");

        foreach (var log in auditLogs)
        {
            csv.AppendLine($"{log.Id}," +
                          $"{log.TicketId ?? 0}," +
                          $"\"{log.Action.Replace("\"", "\"\"")}\","+
                          $"\"{log.Details?.Replace("\"", "\"\"") ?? ""}\","+
                          $"\"{log.User.FirstName} {log.User.LastName}\"," +
                          $"\"{log.OldValues?.Replace("\"", "\"\"") ?? ""}\","+
                          $"\"{log.NewValues?.Replace("\"", "\"\"") ?? ""}\","+
                          $"{log.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"audit_logs_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        return File(bytes, "text/csv", fileName);
    }
}