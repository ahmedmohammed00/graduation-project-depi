using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FastDrop.MVC.ViewModels;

public class EmployeeRoleEditViewModel
{
    public int EmployeeId { get; set; }
    public int? RoleId { get; set; }
    [BindNever]
    public string Name { get; set; } = string.Empty;
    [BindNever]
    public string Email { get; set; } = string.Empty;
}
