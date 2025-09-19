using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain.ViewModels
{
    public class UserActivityReport
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ActivityType { get; set; }
        public string ActivityDescription { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
