using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain
{
    public class RequestStatus
    {
        [Key]
        [Required]
        public int Id { get; set; }
        [Required]
        public string RequestStatusName { get; set; } = "Pending";
        [Required]
        public string Feedback { get; set; }
        [Required]
        public int RequestInformationId { get; set; }
        public RequestInformation RequestInformation { get; set; }

        public int ApproverId { get; set; }
        public Employee Approver { get; set; }
    }
}
