using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GatePass.MS.Domain
{
    public class DepartmentViewModel
    {
       
        public int DepartmentId { get; set; }
        public string Name { get; set; }
        public int? ParentDepartmentId { get; set; }
      
    }
}
