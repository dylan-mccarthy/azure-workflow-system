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
public class TicketsController : ControllerBase
{
    private readonly WorkflowDbContext _context;
    private readonly ILogger<TicketsController> _logger;
    private readonly ISlaService _slaService;

    public TicketsController(WorkflowDbContext context, ILogger<TicketsController> logger, ISlaService slaService)
    {
        _context = context;
        _logger = logger;
        _slaService = slaService;
    }

    /// <summary>
    /// Get all tickets with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TicketDto>>> GetTickets(
        [FromQuery] TicketStatus? status = null,
        [FromQuery] TicketPriority? priority = null,
        [FromQuery] TicketCategory? category = null,
        [FromQuery] int? assignedToId = null)
    {
        var query = _context.Tickets
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .Include(t => t.Attachments)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (priority.HasValue)
            query = query.Where(t => t.Priority == priority.Value);

        if (category.HasValue)
            query = query.Where(t => t.Category == category.Value);

        if (assignedToId.HasValue)
            query = query.Where(t => t.AssignedToId == assignedToId.Value);

        var ticketEntities = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var ticketDtos = new List<TicketDto>();
        foreach (var ticket in ticketEntities)
        {
            var ticketDto = await MapToTicketDto(ticket);
            ticketDtos.Add(ticketDto);
        }

        return Ok(ticketDtos);
    }

    /// <summary>
    /// Get a specific ticket by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TicketDto>> GetTicket(int id)
    {
        var ticket = await _context.Tickets
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
        {
            return NotFound();
        }

        var ticketDto = await MapToTicketDto(ticket);
        return Ok(ticketDto);
    }

    /// <summary>
    /// Create a new ticket
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TicketDto>> CreateTicket(CreateTicketDto createTicketDto)
    {
        // For now, use the admin user as the creator
        // In a real application, this would come from the authenticated user context
        var createdBy = await _context.Users.FirstAsync(u => u.Id == 1);
        
        var ticket = new Ticket
        {
            Title = createTicketDto.Title,
            Description = createTicketDto.Description,
            Priority = createTicketDto.Priority,
            Category = createTicketDto.Category,
            AzureResourceId = createTicketDto.AzureResourceId,
            AlertId = createTicketDto.AlertId,
            CreatedById = createdBy.Id,
            AssignedToId = createTicketDto.AssignedToId,
            Status = TicketStatus.New,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Calculate SLA target date
        await _slaService.CalculateSlaTargetDate(ticket);

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        // Load the ticket with related data for response
        var createdTicket = await _context.Tickets
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .Include(t => t.Attachments)
            .FirstAsync(t => t.Id == ticket.Id);

        var ticketDto = await MapToTicketDto(createdTicket);
        return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, ticketDto);
    }

    /// <summary>
    /// Update an existing ticket
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTicket(int id, UpdateTicketDto updateTicketDto)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrEmpty(updateTicketDto.Title))
            ticket.Title = updateTicketDto.Title;

        if (updateTicketDto.Description != null)
            ticket.Description = updateTicketDto.Description;

        if (updateTicketDto.Status.HasValue)
        {
            ticket.Status = updateTicketDto.Status.Value;
            
            // Set timestamps based on status
            if (updateTicketDto.Status.Value == TicketStatus.Resolved && ticket.ResolvedAt == null)
                ticket.ResolvedAt = DateTime.UtcNow;
            else if (updateTicketDto.Status.Value == TicketStatus.Closed && ticket.ClosedAt == null)
                ticket.ClosedAt = DateTime.UtcNow;
        }

        if (updateTicketDto.Priority.HasValue)
        {
            ticket.Priority = updateTicketDto.Priority.Value;
            // Recalculate SLA if priority changed
            await _slaService.CalculateSlaTargetDate(ticket);
        }

        if (updateTicketDto.Category.HasValue)
        {
            ticket.Category = updateTicketDto.Category.Value;
            // Recalculate SLA if category changed
            await _slaService.CalculateSlaTargetDate(ticket);
        }

        if (updateTicketDto.AzureResourceId != null)
            ticket.AzureResourceId = updateTicketDto.AzureResourceId;

        if (updateTicketDto.AlertId != null)
            ticket.AlertId = updateTicketDto.AlertId;

        ticket.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await TicketExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    /// <summary>
    /// Assign a ticket to a user
    /// </summary>
    [HttpPut("{id}/assignee")]
    public async Task<IActionResult> AssignTicket(int id, AssignTicketDto assignTicketDto)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket == null)
        {
            return NotFound();
        }

        // Validate assignee exists if provided
        if (assignTicketDto.AssignedToId.HasValue)
        {
            var assignee = await _context.Users.FindAsync(assignTicketDto.AssignedToId.Value);
            if (assignee == null)
            {
                return BadRequest("Assigned user does not exist");
            }
        }

        ticket.AssignedToId = assignTicketDto.AssignedToId;
        ticket.UpdatedAt = DateTime.UtcNow;

        // Update status to Assigned if assigning to someone
        if (assignTicketDto.AssignedToId.HasValue && ticket.Status == TicketStatus.New)
        {
            ticket.Status = TicketStatus.Assigned;
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Delete a ticket
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteTicket(int id)
    {
        var ticket = await _context.Tickets.FindAsync(id);
        if (ticket == null)
        {
            return NotFound();
        }

        _context.Tickets.Remove(ticket);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> TicketExists(int id)
    {
        return await _context.Tickets.AnyAsync(e => e.Id == id);
    }

    private async Task<TicketDto> MapToTicketDto(Ticket ticket)
    {
        var isImminentBreach = await _slaService.IsImminentSlaBreach(ticket);
        var slaRemainingMinutes = ticket.SlaTargetDate.HasValue
            ? (int?)(ticket.SlaTargetDate.Value - DateTime.UtcNow).TotalMinutes
            : null;

        return new TicketDto
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Status = ticket.Status,
            Priority = ticket.Priority,
            Category = ticket.Category,
            AzureResourceId = ticket.AzureResourceId,
            AlertId = ticket.AlertId,
            CreatedBy = new UserDto
            {
                Id = ticket.CreatedBy.Id,
                Email = ticket.CreatedBy.Email,
                FirstName = ticket.CreatedBy.FirstName,
                LastName = ticket.CreatedBy.LastName,
                Role = ticket.CreatedBy.Role,
                IsActive = ticket.CreatedBy.IsActive,
                CreatedAt = ticket.CreatedBy.CreatedAt,
                UpdatedAt = ticket.CreatedBy.UpdatedAt
            },
            AssignedTo = ticket.AssignedTo == null ? null : new UserDto
            {
                Id = ticket.AssignedTo.Id,
                Email = ticket.AssignedTo.Email,
                FirstName = ticket.AssignedTo.FirstName,
                LastName = ticket.AssignedTo.LastName,
                Role = ticket.AssignedTo.Role,
                IsActive = ticket.AssignedTo.IsActive,
                CreatedAt = ticket.AssignedTo.CreatedAt,
                UpdatedAt = ticket.AssignedTo.UpdatedAt
            },
            SlaTargetDate = ticket.SlaTargetDate,
            IsSlaBreach = ticket.IsSlaBreach,
            IsImminentSlaBreach = isImminentBreach,
            SlaRemainingMinutes = slaRemainingMinutes,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt,
            AttachmentCount = ticket.Attachments.Count
        };
    }
}