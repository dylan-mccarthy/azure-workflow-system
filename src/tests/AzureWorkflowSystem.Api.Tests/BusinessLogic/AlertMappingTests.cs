using AzureWorkflowSystem.Api.DTOs;
using AzureWorkflowSystem.Api.Models;
using Xunit;

namespace AzureWorkflowSystem.Api.Tests.BusinessLogic;

public class AlertMappingTests
{
    // Helper method to test the severity mapping logic (extracted from AlertsController)
    private static TicketPriority MapSeverityToPriority(string severity)
    {
        return severity?.ToLowerInvariant() switch
        {
            "sev0" or "critical" => TicketPriority.Critical,
            "sev1" or "error" => TicketPriority.High,
            "sev2" or "warning" => TicketPriority.Medium,
            "sev3" or "informational" => TicketPriority.Low,
            _ => TicketPriority.Medium
        };
    }

    // Helper method to test alert description building (extracted from AlertsController)
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

    // Helper method to test primary resource extraction (extracted from AlertsController)
    private static string? GetPrimaryResourceId(string[] targetResources)
    {
        return targetResources?.FirstOrDefault();
    }

    [Theory]
    [InlineData("sev0", TicketPriority.Critical)]
    [InlineData("SEV0", TicketPriority.Critical)]
    [InlineData("critical", TicketPriority.Critical)]
    [InlineData("CRITICAL", TicketPriority.Critical)]
    [InlineData("Critical", TicketPriority.Critical)]
    public void MapSeverityToPriority_CriticalSeverities_ReturnsCriticalPriority(string severity, TicketPriority expected)
    {
        // Act
        var result = MapSeverityToPriority(severity);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("sev1", TicketPriority.High)]
    [InlineData("SEV1", TicketPriority.High)]
    [InlineData("error", TicketPriority.High)]
    [InlineData("ERROR", TicketPriority.High)]
    [InlineData("Error", TicketPriority.High)]
    public void MapSeverityToPriority_HighSeverities_ReturnsHighPriority(string severity, TicketPriority expected)
    {
        // Act
        var result = MapSeverityToPriority(severity);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("sev2", TicketPriority.Medium)]
    [InlineData("SEV2", TicketPriority.Medium)]
    [InlineData("warning", TicketPriority.Medium)]
    [InlineData("WARNING", TicketPriority.Medium)]
    [InlineData("Warning", TicketPriority.Medium)]
    public void MapSeverityToPriority_MediumSeverities_ReturnsMediumPriority(string severity, TicketPriority expected)
    {
        // Act
        var result = MapSeverityToPriority(severity);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("sev3", TicketPriority.Low)]
    [InlineData("SEV3", TicketPriority.Low)]
    [InlineData("informational", TicketPriority.Low)]
    [InlineData("INFORMATIONAL", TicketPriority.Low)]
    [InlineData("Informational", TicketPriority.Low)]
    public void MapSeverityToPriority_LowSeverities_ReturnsLowPriority(string severity, TicketPriority expected)
    {
        // Act
        var result = MapSeverityToPriority(severity);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("sev4")]
    [InlineData("random")]
    public void MapSeverityToPriority_UnknownSeverities_ReturnsMediumPriority(string severity)
    {
        // Act
        var result = MapSeverityToPriority(severity);

        // Assert
        Assert.Equal(TicketPriority.Medium, result);
    }

    [Fact]
    public void BuildAlertDescription_WithCompleteAlert_BuildsFullDescription()
    {
        // Arrange
        var alert = new AlertWebhookDto
        {
            Data = new AlertData
            {
                Essentials = new AlertEssentials
                {
                    AlertId = "test-alert-123",
                    AlertRule = "CPU Usage High",
                    Severity = "critical",
                    SignalType = "Metric",
                    MonitorCondition = "Fired",
                    FiredDateTime = new DateTime(2023, 12, 1, 15, 30, 45, DateTimeKind.Utc),
                    Description = "CPU usage exceeded threshold",
                    TargetResource = new[]
                    {
                        "/subscriptions/test-sub/resourceGroups/test-rg/providers/Microsoft.Compute/virtualMachines/test-vm1",
                        "/subscriptions/test-sub/resourceGroups/test-rg/providers/Microsoft.Compute/virtualMachines/test-vm2"
                    }
                },
                AlertContext = new AlertContext
                {
                    Conditions = new[]
                    {
                        new AlertCondition
                        {
                            MetricName = "CPU Percentage",
                            MetricValue = "95.5",
                            MetricUnit = "Percent",
                            Threshold = "80",
                            Operator = "GreaterThan",
                            TimeAggregation = "Average"
                        },
                        new AlertCondition
                        {
                            MetricName = "Memory Usage",
                            MetricValue = "85.2",
                            MetricUnit = "Percent",
                            Threshold = "90",
                            Operator = "LessThan",
                            TimeAggregation = "Maximum"
                        }
                    }
                }
            }
        };

        // Act
        var description = BuildAlertDescription(alert);

        // Assert
        Assert.Contains("Azure Monitor Alert Details:", description);
        Assert.Contains("Alert Rule: CPU Usage High", description);
        Assert.Contains("Alert ID: test-alert-123", description);
        Assert.Contains("Severity: critical", description);
        Assert.Contains("Signal Type: Metric", description);
        Assert.Contains("Monitor Condition: Fired", description);
        Assert.Contains("Fired Date: 2023-12-01 15:30:45 UTC", description);
        Assert.Contains("Description: CPU usage exceeded threshold", description);
        Assert.Contains("Target Resources:", description);
        Assert.Contains("test-vm1", description);
        Assert.Contains("test-vm2", description);
        Assert.Contains("Alert Conditions:", description);
        Assert.Contains("Metric: CPU Percentage", description);
        Assert.Contains("Value: 95.5 Percent", description);
        Assert.Contains("Threshold: 80", description);
        Assert.Contains("Operator: GreaterThan", description);
        Assert.Contains("Time Aggregation: Average", description);
        Assert.Contains("Metric: Memory Usage", description);
        Assert.Contains("Value: 85.2 Percent", description);
        Assert.Contains("Threshold: 90", description);
        Assert.Contains("Operator: LessThan", description);
        Assert.Contains("Time Aggregation: Maximum", description);
    }

    [Fact]
    public void BuildAlertDescription_WithMinimalAlert_BuildsBasicDescription()
    {
        // Arrange
        var alert = new AlertWebhookDto
        {
            Data = new AlertData
            {
                Essentials = new AlertEssentials
                {
                    AlertId = "minimal-alert",
                    AlertRule = "Simple Alert",
                    Severity = "warning",
                    SignalType = "Log",
                    MonitorCondition = "Fired",
                    FiredDateTime = new DateTime(2023, 12, 1, 10, 0, 0, DateTimeKind.Utc),
                    Description = "Simple alert description",
                    TargetResource = null!
                },
                AlertContext = new AlertContext
                {
                    Conditions = null!
                }
            }
        };

        // Act
        var description = BuildAlertDescription(alert);

        // Assert
        Assert.Contains("Azure Monitor Alert Details:", description);
        Assert.Contains("Alert Rule: Simple Alert", description);
        Assert.Contains("Alert ID: minimal-alert", description);
        Assert.Contains("Severity: warning", description);
        Assert.Contains("Signal Type: Log", description);
        Assert.Contains("Monitor Condition: Fired", description);
        Assert.Contains("Fired Date: 2023-12-01 10:00:00 UTC", description);
        Assert.Contains("Description: Simple alert description", description);
        Assert.DoesNotContain("Target Resources:", description);
        Assert.DoesNotContain("Alert Conditions:", description);
    }

    [Fact]
    public void BuildAlertDescription_WithEmptyTargetResourcesArray_DoesNotIncludeTargetResourcesSection()
    {
        // Arrange
        var alert = new AlertWebhookDto
        {
            Data = new AlertData
            {
                Essentials = new AlertEssentials
                {
                    AlertId = "no-resources-alert",
                    AlertRule = "No Resources Alert",
                    Severity = "info",
                    SignalType = "Metric",
                    MonitorCondition = "Fired",
                    FiredDateTime = DateTime.UtcNow,
                    Description = "Alert with no target resources",
                    TargetResource = new string[0] // Empty array
                },
                AlertContext = new AlertContext
                {
                    Conditions = null!
                }
            }
        };

        // Act
        var description = BuildAlertDescription(alert);

        // Assert
        Assert.DoesNotContain("Target Resources:", description);
        Assert.Contains("Alert Rule: No Resources Alert", description);
    }

    [Fact]
    public void BuildAlertDescription_WithEmptyConditionsArray_DoesNotIncludeConditionsSection()
    {
        // Arrange
        var alert = new AlertWebhookDto
        {
            Data = new AlertData
            {
                Essentials = new AlertEssentials
                {
                    AlertId = "no-conditions-alert",
                    AlertRule = "No Conditions Alert",
                    Severity = "warning",
                    SignalType = "Metric",
                    MonitorCondition = "Fired",
                    FiredDateTime = DateTime.UtcNow,
                    Description = "Alert with no conditions",
                    TargetResource = new[] { "/subscriptions/test/resource" }
                },
                AlertContext = new AlertContext
                {
                    Conditions = new AlertCondition[0] // Empty array
                }
            }
        };

        // Act
        var description = BuildAlertDescription(alert);

        // Assert
        Assert.DoesNotContain("Alert Conditions:", description);
        Assert.Contains("Target Resources:", description);
        Assert.Contains("Alert Rule: No Conditions Alert", description);
    }

    [Theory]
    [InlineData(new[] { "/first/resource", "/second/resource" }, "/first/resource")]
    [InlineData(new[] { "/only/resource" }, "/only/resource")]
    [InlineData(new string[0], null)]
    [InlineData(null, null)]
    public void GetPrimaryResourceId_WithVariousInputs_ReturnsCorrectResult(string[] targetResources, string expected)
    {
        // Act
        var result = GetPrimaryResourceId(targetResources);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetPrimaryResourceId_WithComplexResourcePaths_ReturnsFirstResource()
    {
        // Arrange
        var targetResources = new[]
        {
            "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/production-rg/providers/Microsoft.Compute/virtualMachines/web-server-01",
            "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/production-rg/providers/Microsoft.Compute/virtualMachines/web-server-02",
            "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/production-rg/providers/Microsoft.Storage/storageAccounts/prodstorageacct"
        };

        // Act
        var result = GetPrimaryResourceId(targetResources);

        // Assert
        Assert.Equal("/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/production-rg/providers/Microsoft.Compute/virtualMachines/web-server-01", result);
    }
}