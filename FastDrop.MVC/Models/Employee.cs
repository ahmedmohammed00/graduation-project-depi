using System;
using System.Collections.Generic;

namespace FastDrop.MVC.Models
{
    public class Employee
    {
        public int EmployeeId { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string AccountStatus { get; set; } = "active";
        
        public int? RoleId { get; set; }
        public Role? Role { get; set; }

        public int? SupervisorId { get; set; }
        public Employee? Supervisor { get; set; }
        public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();

        public ICollection<Driver> ManagedDrivers { get; set; } = new List<Driver>();
    }
}
