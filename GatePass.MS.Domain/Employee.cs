using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain
{
    public class Employee
    {
        [Key]
        [Required]
        public int Id { get; set; }
        [Required]
        public string FirstName { get; set; } = "";
        [Required]

        public string LastName { get; set; } = "";
        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = "";



        [Required]
        [RegularExpression(@"^\+?\d{1,4}?[\s-]?\d{7,15}$", ErrorMessage = "Invalid phone number format.")]
        public string Phone { get; set; }

        [Required]
        public string Gender { get; set; }
        public string Address { get; set; }
        [Required]
        [Range(0, 120, ErrorMessage = "Age must be between 0 and 120.")]
        public int Age { get; set; }

        [Required]
        public int DepartmentId { get; set; }
        [Required]
        public int isActive { get; set; }
        public Department Department { get; set; }
        [Required]
        public int DesignationId { get; set; }
        public Designation Designation { get; set; }
        [Required]

     
        ICollection<RequestInformation> RequestInformation { get; }
        public int CompanyId { get; set; }
        public Company Company { get; set; }
        public int? UserId { get; set; }
        public ApplicationUser User { get; set; }

    }
}