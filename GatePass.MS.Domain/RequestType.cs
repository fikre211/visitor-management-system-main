using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain
{
    public class RequestType
    {
        [Key]
        public int Id { get; set; }

        public string RequestTypeName { get; set; }

        public ICollection<RequestInformation> RequestInformations { get; set; }

    }
}
