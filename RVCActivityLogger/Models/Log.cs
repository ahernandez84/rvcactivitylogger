using System;

namespace RVCActivityLogger.Models
{
    public class Log
    {
        public DateTime IncidentDate { get; set; }
        public string Employee { get; set; }
        public string IncidentType { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Money { get; set; }
        public string Vehicle { get; set; }
        public int StartMileage { get; set; }
        public int EndMileage { get; set; }
        public int Fuel { get; set; }

        public Guid RowId { get; set; }
        public Guid IncidentTypeId { get; set; }
        public Guid LocationId { get; set; }
        public Guid EmployeeId { get; set; }
    }
}
