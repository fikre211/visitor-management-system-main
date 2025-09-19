using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain
{
    public class Company
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^\+?\d{1,4}?[\s-]?\d{7,15}$", ErrorMessage = "Invalid phone number format.")]
        public string Phone { get; set; }
        public string? website { get; set; }
        public ICollection<Department> Departments { get; set; } = new List<Department>(); // Initialized
        public ICollection<Designation> Designations { get; set; } = new List<Designation>(); // Initialized
        public ICollection<Employee> Employees { get; set; } = new List<Employee>(); // Initialized
        public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>(); // Initialized

        public ICollection<RequestInformation> RequestInformations { get; set; } = new List<RequestInformation>(); // Initialized

        public string? LogoPath { get; set; }

    }
}