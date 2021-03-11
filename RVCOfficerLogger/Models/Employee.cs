using System;

namespace RVCOfficerLogger.Models
{
    public class Employee
    {
        public string EmployeeID { get; set; }
        public int StatusNum { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Status { get; set; }
        public string FullName { get; set; }
        public Guid RowId { get; set; }

        public string Password { get; set; }
    }
}
