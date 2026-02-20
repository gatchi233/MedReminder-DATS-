namespace MedReminder.Api.Entities;

public sealed class Resident
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    public DateOnly? DateOfBirth { get; set; }

    // Keep “Admission / Facility record” aligned to your current baseline.
    public DateOnly? AdmissionDate { get; set; }
    public string RoomNumber { get; set; } = "";
}