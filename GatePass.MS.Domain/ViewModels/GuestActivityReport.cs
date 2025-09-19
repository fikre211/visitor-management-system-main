using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain.ViewModels
{
    public class GuestActivityReport
    {
        public int? GuestId { get; set; }
        public string FirstName { get; set; } // Add this
        public string LastName { get; set; }
        public string Email { get; set; }
        public string ActivityType { get; set; }
        public string ActivityDescription { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
