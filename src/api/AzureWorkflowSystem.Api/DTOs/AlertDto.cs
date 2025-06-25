using AzureWorkflowSystem.Api.Models;

namespace AzureWorkflowSystem.Api.DTOs;

/// <summary>
/// DTO for Azure Monitor alert webhook payload
/// Based on Azure Monitor common alert schema
/// </summary>
public class AlertWebhookDto
{
    public string SchemaId { get; set; } = string.Empty;
    public AlertData Data { get; set; } = new();
}

public class AlertData
{
    public AlertEssentials Essentials { get; set; } = new();
    public AlertContext AlertContext { get; set; } = new();
}

public class AlertEssentials
{
    public string AlertId { get; set; } = string.Empty;
    public string AlertRule { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string SignalType { get; set; } = string.Empty;
    public string MonitorCondition { get; set; } = string.Empty;
    public string[] TargetResource { get; set; } = Array.Empty<string>();
    public DateTime FiredDateTime { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class AlertContext
{
    public string ConditionType { get; set; } = string.Empty;
    public AlertCondition[] Conditions { get; set; } = Array.Empty<AlertCondition>();
}

public class AlertCondition
{
    public string MetricName { get; set; } = string.Empty;
    public string MetricUnit { get; set; } = string.Empty;
    public string MetricValue { get; set; } = string.Empty;
    public string Threshold { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string TimeAggregation { get; set; } = string.Empty;
}