using AzureWorkflowSystem.Api.Services;

namespace AzureWorkflowSystem.Api.BackgroundServices;

public class SlaMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SlaMonitoringService> _logger;
    private readonly IConfiguration _configuration;

    public SlaMonitoringService(
        IServiceProvider serviceProvider,
        ILogger<SlaMonitoringService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SLA Monitoring Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckSlaBreaches();
                
                // Check every 15 minutes
                var checkInterval = _configuration.GetValue<int>("SlaMonitoring:CheckIntervalMinutes", 15);
                await Task.Delay(TimeSpan.FromMinutes(checkInterval), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while monitoring SLA breaches");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes before retrying
            }
        }
    }

    private async Task CheckSlaBreaches()
    {
        using var scope = _serviceProvider.CreateScope();
        var slaService = scope.ServiceProvider.GetRequiredService<ISlaService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        try
        {
            var imminentBreachTickets = await slaService.GetImminentSlaBreachTickets();
            var ticketList = imminentBreachTickets.ToList();

            if (ticketList.Any())
            {
                _logger.LogInformation("Found {Count} tickets with imminent SLA breaches", ticketList.Count);
                await notificationService.SendSlaBreachNotifications(ticketList, isImminentBreach: true);
            }
            else
            {
                _logger.LogDebug("No imminent SLA breaches found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking SLA breaches");
        }
    }
}