namespace MedReminder.Models
{
    public class MedicationOrder
    {
        public int Id { get; set; }
        public int MedicationId { get; set; }
        public int RequestedQuantity { get; set; }

        public MedicationOrderStatus Status { get; set; }

        public DateTime RequestedAt { get; set; }
        public string? RequestedBy { get; set; }

        public DateTime? OrderedAt { get; set; }
        public string? OrderedBy { get; set; }

        public DateTime? ReceivedAt { get; set; }
        public string? ReceivedBy { get; set; }

        public DateTime? CancelledAt { get; set; }
        public string? CancelledBy { get; set; }

        public string? Notes { get; set; }
    }

    public enum MedicationOrderStatus
    {
        Requested,
        Ordered,
        Received,
        Cancelled
    }
}
