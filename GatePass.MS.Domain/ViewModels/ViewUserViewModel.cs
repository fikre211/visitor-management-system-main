using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain.ViewModels
{
    public class ViewUserViewModel
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string Department {  get; set; }
        public string DepartmentSuperviser { get; set; }
        public List<string> Roles { get; set; } // Store roles as a list of strings
        public bool IsLocked { get; set; }
        public bool IsActive { get; set; }
    }
}
