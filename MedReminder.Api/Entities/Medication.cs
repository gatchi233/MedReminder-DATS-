namespace MedReminder.Api.Entities;

public sealed class Medication
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ResidentId { get; set; }

    public string Name { get; set; } = "";
    public string Dosage { get; set; } = "";     // e.g. "5 mg"
    public string Frequency { get; set; } = "";  // e.g. "BID"
    public string Notes { get; set; } = "";

    // You can add StartDate/EndDate later; keep M0 minimal.
}