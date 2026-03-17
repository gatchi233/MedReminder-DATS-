using System.ComponentModel.DataAnnotations;

namespace CareHub.Api.Entities;

public class MedicationOrder
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid MedicationId { get; set; }

    public int RequestedQuantity { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Requested";

    public DateTimeOffset RequestedAt { get; set; }

    [MaxLength(200)]
    public string? RequestedBy { get; set; }

    public DateTimeOffset? OrderedAt { get; set; }

    [MaxLength(200)]
    public string? OrderedBy { get; set; }

    public DateTimeOffset? ReceivedAt { get; set; }

    [MaxLength(200)]
    public string? ReceivedBy { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    [MaxLength(200)]
    public string? CancelledBy { get; set; }

    [MaxLength(200)]
    public string? MedicationName { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTimeOffset? ReceivedExpiryDate { get; set; }

    // Navigation
    public Medication? Medication { get; set; }
}
