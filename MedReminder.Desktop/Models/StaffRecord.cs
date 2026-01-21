using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedReminder.Models
{
    public class StaffRecord
        {
            public string EmployeeId { get; set; } = string.Empty;        // EMP-001
            public string FirstName { get; set; } = string.Empty;         // Sarah
            public string LastName { get; set; } = string.Empty;          // Chen
            public string JobTitle { get; set; } = string.Empty;          // General Manager
            public string Department { get; set; } = string.Empty;        // Administration
            public string EmploymentStatus { get; set; } = string.Empty;  // Full-time / Part-time / Terminated
            public decimal HourlyWage { get; set; }                       // 48.50
            public string ShiftPreference { get; set; } = string.Empty;   // Day / Evening / Night / Any

            public StaffCompliance Compliance { get; set; } = new StaffCompliance();

            // Optional for Milestone 3 demo (won't break existing JSON)
            public string Role { get; set; } = "Staff";                   // Admin / Staff
            public bool IsEnabled { get; set; } = true;                   // Enable/Disable account

            public string FullName => $"{FirstName} {LastName}".Trim();
        }

        public class StaffCompliance
        {
            public string FirstAidExpiry { get; set; } = string.Empty;    // "2027-03-15"
            public bool FoodSafeCertified { get; set; }
        }
    }
