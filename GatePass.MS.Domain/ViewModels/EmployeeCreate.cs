using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain.ViewModels
{
    public class EmployeeCreate
    {
        [Required]
        public string FirstName { get; set; } = "";
        [Required]
        public string LastName { get; set; } = "";
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
        [Required]
        public int Phone { get; set; }
        [Required]
        public string Gender { get; set; } = "";
        public string Address { get; set; }
        public int Age { get; set; }

        [Required]
        public int DepartmentId { get; set; }
        public Department Department { get; set; }
        [Required]
        public int DesignationId { get; set; }
        public Designation Designation { get; set; }

    }
}
