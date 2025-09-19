using Microsoft.AspNetCore.Http;
using System;
using System.ComponentModel.DataAnnotations;

namespace GatePass.MS.Domain.ViewModels
{
    public class RequestInformationViewModel
    {
        
        public string? Title { get; set; }

        [StringLength(40, ErrorMessage = "First name cannot be longer than 40 characters.")]
        [RegularExpression("^[\u1200-\u137F a-zA-Z/.]+$", ErrorMessage = "First name must contain only Amharic or English letters and spaces and slashs.")]
        [Required]
        public string? GuestFirstName { get; set; }
        [StringLength(40, ErrorMessage = "Last name cannot be longer than 40 characters.")]
        [RegularExpression("^[\u1200-\u137Fa-zA-Z./]+$", ErrorMessage = "Last name must contain only Amharic or English letters and spaces and slashs.")]
        public string? GuestLastName { get; set; }

        [StringLength(100, ErrorMessage = "Company name cannot be longer than 100 characters.")]

        public string? CompanyName { get; set; }
        public int? EmployeeId { get; set; }
        //[Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string? GuestEmail { get; set; } = "";

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"\d{10}", ErrorMessage = "Phone number must be exactly 10 digits.")]
        public string GuestPhoneNumber { get; set; }

        [StringLength(50, ErrorMessage = "Company name cannot be longer than 50 characters.")]
        public string PurposeOfVisit { get; set; } = "";
        [Required]
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }
        public int RequestTypeId { get; set; }
        public bool IsIndividual { get; set; } = true;
        public DateTime VisitDateTimeStart { get; set; }
        public DateTime VisitDateTimeEnd { get; set; }

        [Display(Name = "File")]
        public IFormFile? File { get; set; }
        public List<DeviceViewModel> Devices { get; set; } = new List<DeviceViewModel>();
        public List<GuestViewModel> AdditionalGuests { get; set; } = new List<GuestViewModel>();

        public byte[]? ImageData { get; set; } // To store the image as a byte array
        public string? GuestPhotoPath { get; set; }


    }
    public class DeviceViewModel
    {
        public string DeviceName { get; set; }
        public string Identifier { get; set; }
        public string Description { get; set; }
    }
    public class GuestViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

       
        [RegularExpression(@"\d{15}", ErrorMessage = "Phone number must be exactly 15 digits.")]
        public string? Phone { get; set; }
        public string CompanyName { get; set; }
    }
}
