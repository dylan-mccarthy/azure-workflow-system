using AzureWorkflowSystem.Api.Models;
using System.Text.Json;

namespace AzureWorkflowSystem.Api.Services;

public interface INotificationService
{
    Task SendSlaBreachNotification(Ticket ticket, bool isImminentBreach = false);
    Task SendSlaBreachNotifications(IEnumerable<Ticket> tickets, bool isImminentBreach = false);
}

public class NotificationService : INotificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<NotificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendSlaBreachNotification(Ticket ticket, bool isImminentBreach = false)
    {
        await SendSlaBreachNotifications(new[] { ticket }, isImminentBreach);
    }

    public async Task SendSlaBreachNotifications(IEnumerable<Ticket> tickets, bool isImminentBreach = false)
    {
        var teamsWebhookUrl = _configuration["Teams:WebhookUrl"];
        if (string.IsNullOrEmpty(teamsWebhookUrl))
        {
            _logger.LogWarning("Teams webhook URL not configured. Skipping notification.");
            return;
        }

        var ticketList = tickets.ToList();
        if (!ticketList.Any())
        {
            return;
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var message = CreateTeamsMessage(ticketList, isImminentBreach);
            var jsonContent = JsonSerializer.Serialize(message);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(teamsWebhookUrl, content);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully sent SLA breach notification for {Count} tickets", ticketList.Count);
            }
            else
            {
                _logger.LogError("Failed to send Teams notification. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, await response.Content.ReadAsStringAsync());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending Teams notification for SLA breach");
        }
    }

    private object CreateTeamsMessage(IList<Ticket> tickets, bool isImminentBreach)
    {
        var title = isImminentBreach ? "⚠️ SLA Breach Warning" : "🚨 SLA Breach Alert";
        var color = isImminentBreach ? "warning" : "attention";
        var description = isImminentBreach 
            ? "The following tickets are approaching their SLA deadline (≤ 10% time remaining):"
            : "The following tickets have breached their SLA deadline:";

        var facts = tickets.Select(t => new
        {
            name = $"Ticket #{t.Id}",
            value = $"**{t.Title}**\n" +
                   $"Priority: {t.Priority}\n" +
                   $"Category: {t.Category}\n" +
                   $"Assigned to: {t.AssignedTo?.FirstName} {t.AssignedTo?.LastName ?? "Unassigned"}\n" +
                   $"SLA Target: {t.SlaTargetDate:yyyy-MM-dd HH:mm} UTC"
        }).ToArray();

        return new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    content = new
                    {
                        type = "AdaptiveCard",
                        body = new object[]
                        {
                            new
                            {
                                type = "TextBlock",
                                size = "Medium",
                                weight = "Bolder",
                                text = title
                            },
                            new
                            {
                                type = "TextBlock",
                                text = description,
                                wrap = true
                            },
                            new
                            {
                                type = "FactSet",
                                facts = facts
                            }
                        },
                        schema = "http://adaptivecards.io/schemas/adaptive-card.json",
                        version = "1.3"
                    }
                }
            }
        };
    }
}