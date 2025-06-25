namespace AzureWorkflowSystem.Api.DTOs;

public class ReportMetricsDto
{
    public double MttaMinutes { get; set; }
    public double MttrMinutes { get; set; }
    public double SlaCompliancePercentage { get; set; }
    public int TotalTickets { get; set; }
    public int OpenTickets { get; set; }
    public int ClosedTickets { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}

public class TicketTrendDto
{
    public DateTime Date { get; set; }
    public int OpenTickets { get; set; }
    public int ClosedTickets { get; set; }
}

public class ReportFiltersDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Priority { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
}