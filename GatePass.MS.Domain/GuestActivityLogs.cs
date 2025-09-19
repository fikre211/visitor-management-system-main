using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain
{
    public class GuestActivityLogs
    {
        public int Id { get; set; }
        [Required]
        [StringLength(50)]
        public string ActivityType { get; set; }

        [StringLength(255)]
        public string ActivityDescription { get; set; }


        [Required]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [ForeignKey("GuestId")]
        public int? GuestId { get; set; }

        public Guest? Guest { get; set; }

    }
}
