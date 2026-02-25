namespace MedReminder.Models
{
    public sealed class Observation
    {
        public Guid Id { get; set; }
        public Guid ResidentId { get; set; }

        // (Optional improvement later) This is denormalized; can be removed once DB exists.
        public string ResidentName { get; set; } = "";

        // Aligned with API model
        public DateTime RecordedAt { get; set; }
        public string Type { get; set; } = "";   // e.g. "BP", "Temp", "Note"
        public string Value { get; set; } = "";  // e.g. "120/80", "37.1", "..."
        public string RecordedBy { get; set; } = "";
    }
}
