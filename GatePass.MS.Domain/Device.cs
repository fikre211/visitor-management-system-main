using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain
{
    public class Device
    {
        [Key]
        public int DeviceId { get; set; }
        public string DeviceName { get; set; } // e.g., Car, Gun, etc.
        public string Identifier { get; set; } // Optional: To track devices uniquely
        public string? Description { get; set; } // Details or additional info about the device
        public int RequestInformationId { get; set; } // Foreign key to link with request info
        public bool? IsGranted { get; set; }
        public bool? IsDenied { get; set; }

        public RequestInformation RequestInformation { get; set; } // Navigation property
    }
}