using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain.ViewModels
{
    public class RequestInformationViewDetail : RequestInformationViewModel
    {
        public int Id { get; set; }
        public bool Approve { get; set; }
        public bool Disapprove { get; set; }
        public bool Review { get; set; }
        public string Feedback { get; set; }

        public string Status { get; set; }
        public string CompanyName { get; set; }
        public bool IsIndividual { get; set; }
        public DateTime VisitDateTimeStart { get; set; }
        public DateTime VisitDateTimeEnd { get; set; }
        public DateTime? ApprovedDateTimeStart { get; set; }
        public DateTime? ApprovedDateTimeEnd { get; set; }
        public List<RequestStatus> RequestStatuses { get; set; } 
        public int GuestId { get; set; }
       
        public int? InviterId {  get; set; }
        public string? Approver {  get; set; }
        public bool IsCheckedIn { get; set; }
        public string GuestDestinationDepartment { get; set; }
        public ICollection<Attachment> Attachments { get; set; }= new List<Attachment>();
        public List<Device> Devices { get; set; } = new List<Device>(); // Changed to List<Device>
        public ICollection<Guest> AdditionalGuests { get; set; } = new List<Guest>();

        public string? GuestPhotoPath { get; set; }
        public string? SelectedReason { get; set; }



    }
}
