using System;
using System.Collections.Generic;

namespace FastDrop.MVC.DTOs
{
    public class EmployeeDto
    {
        public int EmployeeId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string AccountStatus { get; set; }
        public int? RoleId { get; set; }
        public int? SupervisorId { get; set; }
    }
}
