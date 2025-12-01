using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain.ViewModels
{
    public class VisitReportDto
    {
        public int RequestId { get; set; }
        public DateTime VisitDateTimeStart { get; set; }
        public DateTime VisitDateTimeEnd { get; set; }
        public DateTime? ApprovedDateTimeStart { get; set; }
        public DateTime? ApprovedDateTimeEnd { get; set; }
        public string PurposeOfVisit { get; set; }
        public string EmployeeName { get; set; }
        public string GuestName { get; set; }
        public string Phone { get; set; }
        public string? Email { get; set; }
        public string Status { get; set; }
        public string? Approver { get; set; }
        public bool IsCheckedIn { get; set; }
        public DateTime Timestamp { get; set; }

        public string AdditionalGuests { get; set; }  // NEW
        public string Devices { get; set; }           // NEW
    }


}