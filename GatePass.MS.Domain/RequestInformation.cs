using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

using System.Linq;

using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain
{
    public class RequestInformation
    {
        [Key]
        public int Id { get; set; }
        public DateTime VisitDateTimeStart { get; set; }
        public DateTime VisitDateTimeEnd { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;

        public string PurposeOfVisit { get; set; } = "";
        public bool Deleted { get; set; } = false;
        public bool IsIndividual { get; set; } = true;
        public bool IsCheckedIn { get; set; } = false;
        // Navigation properties
        [ForeignKey("EmployeeId")]
        public int? EmployeeId { get; set; }

        public Employee? Employee { get; set; }
        [ForeignKey("DepartmentId")]
        public int? DepartmentId { get; set; }

        public Department? Department { get; set; }

        public int CompanyId { get; set; }
        public Company Company { get; set; }

        [ForeignKey("GuestId")]
        public int? GuestId { get; set; }

        public Guest? Guest { get; set; }

        public string? Feedback { get; set; }

        public DateTime? ApprovedDateTimeStart { get; set; }
        public DateTime? ApprovedDateTimeEnd { get; set; }


        public string Status { get; set; } = "Pending";
        public ApplicationUser? Approver { get; set; }
        [ForeignKey("ApproverId")]
        public string? ApproverId { get; set; }

        // Navigation properties for related entities
        public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
        public ICollection<Device> Devices { get; set; } = new List<Device>(); // List of devices linked to the request
        public ICollection<Guest> AdditionalGuests { get; set; } = new List<Guest>();

        public byte[]? ImageData { get; set; } // To store the image as a byte array

    }
}