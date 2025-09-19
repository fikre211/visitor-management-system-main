using GatePass.MS.Domain;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GatePass.MS.Domain.ViewModels
{
    public class GuestActivityReportModel
    {
        public string? GuestName { get; set; }
        public string? ApproverName { get; set; }
        public string? InviterName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public List<GuestActivityDto> Activities { get; set; } = new List<GuestActivityDto>();

        public int? SelectedGuestId { get; set; }
        public List<SelectListItem> Guests { get; set; }
    }

    public class GuestActivityDto
    {
        public string GuestId { get; set; }
        public string FirstName { get; set; } // Add this
        public string LastName { get; set; }
        public string Email { get; set; }
        public string ActivityType { get; set; }
        public string ActivityDescription { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
