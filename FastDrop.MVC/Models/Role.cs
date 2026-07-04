using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class Role
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public string? PermissionsDescription { get; set; }

        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
