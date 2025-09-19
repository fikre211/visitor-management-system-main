using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain
{
    public class Attachment
    {
        [Key]
        [Required]
        public int Id { get; set; }
        public string AttachmentName { get; set; }

        [Required]
        public int RequestInformationId { get; set; }
        public RequestInformation RequestInformation { get; set; }
    }
}
