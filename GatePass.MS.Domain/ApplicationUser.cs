using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain
{
    public class ApplicationUser : IdentityUser
    {

        public int? EmployeeId { get; set; } 
        public Employee Employee { get; set; }
        public int? CompanyId { get; set; }
        public Company Company { get; set; }
        public int? GuestId { get; set; }
        public Guest Guest { get; set; }
        public bool IsLocked { get; set; } = false;  // Default to not locked
        public bool IsActive { get; set; } = true;   // Default to active

        // Timestamps for when the user was created and last modified
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public string? UserPhotoPath { get; set; }

        

    }
}
