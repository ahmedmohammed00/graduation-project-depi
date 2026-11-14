using System.Collections.Generic;
using System.Threading.Tasks;
using FastDrop.MVC.Models;
using FastDrop.MVC.ViewModels;

namespace FastDrop.MVC.Services
{
    public interface IEmployeeService
    {
        Task<IEnumerable<Employee>> GetEmployeesAsync();
        Task<Employee?> GetEmployeeByIdAsync(int id);
        Task CreateEmployeeAsync(Employee employee);
        Task<bool> UpdateEmployeeAsync(EmployeeRoleEditViewModel model);
        Task DeleteEmployeeAsync(int id);
        Task<IEnumerable<Role>> GetRolesAsync();
    }
}
