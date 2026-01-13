namespace MedReminder.Models
{
    public class Resident
    {
        public int Id { get; set; }

        // Basic Information
        public string Name { get; set; }=string.Empty;
        public string? SIN { get; set; }
        public string DOB { get; set; } = string.Empty;

        // Emergency contact
        public string EmergencyContactName1 { get; set; } = string.Empty;
        public string EmergencyContactPhone1 { get; set; } = string.Empty;
        public string EmergencyRelationship1 { get; set; } = string.Empty;
        public string? EmergencyContactName2 { get; set; }
        public string? EmergencyContactPhone2 { get; set; }
        public string? EmergencyRelationship2 { get; set; }


        // Doctor information
        public string DoctorName { get; set; } = string.Empty;
        public string DoctorContact { get; set; } = string.Empty;

        // Allergies & Remarks
        public string AllergyItems { get; set; } = string.Empty;   // e.g. "Penicillin, Nuts"
        public string? Remarks { get; set; }
    }
}
