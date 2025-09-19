using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain.ViewModels
{
    public class VisitReportModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? ApprovedStartDate { get; set; }
        public DateTime? ApprovedEndDate { get; set; }
        public string? Status { get; set; }
        public string? Approver { get; set; }

        public int? EmployeeId { get; set; }
        public IEnumerable<VisitReportDto> Reports { get; set; }
        public IEnumerable<Employee> Employees { get; set; }
        public DateTime Timestamp { get; set; }

    }

}