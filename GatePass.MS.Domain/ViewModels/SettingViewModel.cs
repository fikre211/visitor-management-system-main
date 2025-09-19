using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain.ViewModels
{
    public class SettingViewModel
    {
        public string AppName {  get; set; }
        public string MaxLoginAttempts { get; set; }
    }
}
