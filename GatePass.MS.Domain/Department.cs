using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GatePass.MS.Domain
{
    public class Department
    {
        [Key]
        public int Id { get; set; }

        public string? Name { get; set; } // Nullable for safety
        public int CompanyId { get; set; }
        public Company Company { get; set; }
        public int? ParentDepartmentId { get; set; }
        public Department? ParentDepartment { get; set; } // Nullable for safety

        public ICollection<Employee> Employees { get; set; } = new List<Employee>(); // Initialized
        public ICollection<Department> ChildDepartments { get; set; } = new List<Department>(); // Initialized
    }
}
