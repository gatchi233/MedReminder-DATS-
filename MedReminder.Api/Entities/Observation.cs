namespace MedReminder.Api.Entities;

public sealed class Observation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ResidentId { get; set; }

    public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;

    public string Type { get; set; } = "";   // e.g. "BP", "Temp", "Note"
    public string Value { get; set; } = "";  // e.g. "120/80", "37.1", "..."
    public string RecordedBy { get; set; } = ""; // staff name/id later
}