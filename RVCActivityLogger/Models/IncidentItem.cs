using System;

namespace RVCActivityLogger.Models
{
    public class IncidentItem
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }

        public Guid RowId { get; set; }
        public int StatusNum { get; set; }
    }
}
