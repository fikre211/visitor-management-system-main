using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain
{
    public class Guest
    {
        [Key]
        [Required]
        public int Id { get; set; }
        //if individual firstname and lastname
       
        public string? FirstName { get; set; } = "";
       
        public string? LastName { get; set; } = "";
        //if company company name
       
            public string? CompanyName { get; set; } = "";
            //[Required]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        public string? Email { get; set; } = "";
        [Required(ErrorMessage = "Phone Number is required.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone Number must be 10 digits.")]
        public string Phone { get; set; }
        [ForeignKey("User")]
        public string? UserId { get; set; }
        public ApplicationUser User { get; set; }
        ICollection<RequestInformation> RequestInformation { get; set; }

        public string? GuestPhotoPath { get; set; }







    }
}
