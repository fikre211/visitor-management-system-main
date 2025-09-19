using GatePass.MS.Domain;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GatePass.MS.Domain.ViewModels
{
    public class UserActivityReportModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
       
        public List<UserActivityDto> Activities { get; set; } = new List<UserActivityDto>();
       
        public string? SelectedUserId { get; set; }
        public List<SelectListItem> Users { get; set; }
    }

    public class UserActivityDto
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ActivityType { get; set; }
        public string ActivityDescription { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
