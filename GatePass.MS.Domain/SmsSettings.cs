using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain
{
    public class SmsSettings
    {
        [Key]
        public int Id { get; set; }
        public string Ip { get; set; }
        public string Port { get; set; }
    }
}
