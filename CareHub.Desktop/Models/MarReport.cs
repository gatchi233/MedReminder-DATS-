namespace CareHub.Desktop.Models;

public class MarReport
{
    public DateTimeOffset FromUtc { get; set; }
    public DateTimeOffset ToUtc { get; set; }
    public MarReportSummary Summary { get; set; } = new();
    public List<MarReportLine> Lines { get; set; } = new();
}

public class MarReportSummary
{
    public int TotalEntries { get; set; }
    public int GivenCount { get; set; }
    public int RefusedCount { get; set; }
    public int MissedCount { get; set; }
    public int HeldCount { get; set; }
    public int NotAvailableCount { get; set; }
}

public class MarReportLine
{
    public Guid Id { get; set; }
    public Guid ResidentId { get; set; }
    public string ResidentName { get; set; } = "";
    public Guid MedicationId { get; set; }
    public string MedicationName { get; set; } = "";
    public string Status { get; set; } = "";
    public decimal DoseQuantity { get; set; }
    public string DoseUnit { get; set; } = "";
    public DateTimeOffset? ScheduledForUtc { get; set; }
    public DateTimeOffset AdministeredAtUtc { get; set; }
    public string? RecordedBy { get; set; }
    public string? Notes { get; set; }
}
