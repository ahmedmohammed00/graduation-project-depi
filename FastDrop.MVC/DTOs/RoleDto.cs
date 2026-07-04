using System;
using System.Collections.Generic;

namespace FastDrop.MVC.DTOs
{
    public class RoleDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string? PermissionsDescription { get; set; }
    }
}
