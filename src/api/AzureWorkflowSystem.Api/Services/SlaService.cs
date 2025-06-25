using AzureWorkflowSystem.Api.Data;
using AzureWorkflowSystem.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AzureWorkflowSystem.Api.Services;

public interface ISlaService
{
    Task CalculateSlaTargetDate(Ticket ticket);
    Task<bool> IsSlaBreach(Ticket ticket);
    Task<bool> IsImminentSlaBreach(Ticket ticket, double bufferPercentage = 0.1);
    Task<IEnumerable<Ticket>> GetImminentSlaBreachTickets();
    Task UpdateSlaStatus(Ticket ticket);
}

public class SlaService : ISlaService
{
    private readonly WorkflowDbContext _context;
    private readonly ILogger<SlaService> _logger;

    public SlaService(WorkflowDbContext context, ILogger<SlaService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CalculateSlaTargetDate(Ticket ticket)
    {
        var slaConfig = await _context.SlaConfigurations
            .FirstOrDefaultAsync(s => s.Priority == ticket.Priority && 
                                    s.Category == ticket.Category && 
                                    s.IsActive);

        if (slaConfig != null)
        {
            ticket.SlaTargetDate = ticket.CreatedAt.AddMinutes(slaConfig.ResolutionTimeMinutes);
            await UpdateSlaStatus(ticket);
            
            _logger.LogDebug("Calculated SLA target date for ticket {TicketId}: {SlaTargetDate}", 
                ticket.Id, ticket.SlaTargetDate);
        }
        else
        {
            _logger.LogWarning("No SLA configuration found for Priority: {Priority}, Category: {Category}", 
                ticket.Priority, ticket.Category);
        }
    }

    public async Task<bool> IsSlaBreach(Ticket ticket)
    {
        if (!ticket.SlaTargetDate.HasValue)
        {
            await CalculateSlaTargetDate(ticket);
        }

        return ticket.SlaTargetDate.HasValue && ticket.SlaTargetDate.Value < DateTime.UtcNow;
    }

    public async Task<bool> IsImminentSlaBreach(Ticket ticket, double bufferPercentage = 0.1)
    {
        if (!ticket.SlaTargetDate.HasValue)
        {
            await CalculateSlaTargetDate(ticket);
        }

        if (!ticket.SlaTargetDate.HasValue)
        {
            return false;
        }

        // If already breached, it's not imminent anymore
        if (await IsSlaBreach(ticket))
        {
            return false;
        }

        var totalSlaTime = ticket.SlaTargetDate.Value - ticket.CreatedAt;
        var bufferTime = TimeSpan.FromTicks((long)(totalSlaTime.Ticks * bufferPercentage));
        var remainingTime = ticket.SlaTargetDate.Value - DateTime.UtcNow;

        // Imminent if remaining time is less than or equal to buffer time
        return remainingTime <= bufferTime;
    }

    public async Task<IEnumerable<Ticket>> GetImminentSlaBreachTickets()
    {
        var openTickets = await _context.Tickets
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .Where(t => t.Status != TicketStatus.Resolved && 
                       t.Status != TicketStatus.Closed)
            .ToListAsync();

        var imminentBreachTickets = new List<Ticket>();

        foreach (var ticket in openTickets)
        {
            if (await IsImminentSlaBreach(ticket))
            {
                imminentBreachTickets.Add(ticket);
            }
        }

        return imminentBreachTickets;
    }

    public async Task UpdateSlaStatus(Ticket ticket)
    {
        var wasBreached = ticket.IsSlaBreach;
        ticket.IsSlaBreach = await IsSlaBreach(ticket);

        // Log SLA status change if needed
        if (wasBreached != ticket.IsSlaBreach && ticket.IsSlaBreach)
        {
            _logger.LogWarning("SLA breach detected for ticket {TicketId}. Target: {SlaTargetDate}, Current: {CurrentTime}", 
                ticket.Id, ticket.SlaTargetDate, DateTime.UtcNow);
        }
    }
}