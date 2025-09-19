using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain.ViewModels
{
    public class InviteWrapperViewModel
    {
        public IFormFile GuestPhoto { get; set; }
        public string CapturedImageData { get; set; }

        public List<RequestInformationViewModel> Requests { get; set; } = new();
    }

}
