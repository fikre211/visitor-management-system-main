using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace GatePass.MS.Domain
{
    public class UserActivityLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public int? CompanyId { get; set; }
        public Company Company { get; set; }
        [Required]
        [StringLength(50)]
        public string ActivityType { get; set; }

        [StringLength(255)]
        public string ActivityDescription { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }

}
