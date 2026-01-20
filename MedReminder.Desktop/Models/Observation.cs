using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedReminder.Models
{
    public class Observation
    {
        public int Id { get; set; }
        public int ResidentId { get; set; }

        public DateTime RecordedAt { get; set; } = DateTime.Now;

        public int? Systolic { get; set; }
        public int? Diastolic { get; set; }
        public double? TemperatureC { get; set; }

        public int? HeartRate { get; set; }  // optional
        public int? SpO2 { get; set; }       // optional

        public string? NurseNote { get; set; }
        public string? RecordedBy { get; set; }
    }
}

