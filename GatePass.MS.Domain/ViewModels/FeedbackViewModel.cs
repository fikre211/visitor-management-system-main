using System.ComponentModel.DataAnnotations;

namespace GatePass.MS.Domain.ViewModels
{
    public class FeedbackViewModel
    {
        public int RequestId { get; set; } // To link feedback to the request
        public string? GuestName { get; set; } // To pre-populate for the user
        public string? GuestEmail { get; set; } // To pre-populate for the user

        [StringLength(100)]
        public string? Name { get; set; } = ""; // Name provided by the user (can be different from GuestName)

        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        [StringLength(255)]
        public string? Email { get; set; } = ""; // Email provided by the user

        [Required(ErrorMessage = "Please select a rating.")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; } = 0; // The star rating

        [StringLength(1000, ErrorMessage = "Comments cannot exceed 1000 characters.")]
        public string? Comment { get; set; } = "";
    }
}