using System.Collections.Generic;
using System.Threading.Tasks;
using FastDrop.MVC.Models;
using FastDrop.MVC.Security;
using FastDrop.MVC.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FastDrop.MVC.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly DmsDbContext _context;

        public EmployeeService(DmsDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Employee>> GetEmployeesAsync()
        {
            return await _context.Employees.Include(e => e.Role).ToListAsync();
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            return await _context.Employees.Include(e => e.Role).FirstOrDefaultAsync(e => e.EmployeeId == id);
        }

        public async Task CreateEmployeeAsync(Employee employee)
        {
            employee.Password = PasswordHashing.HashNewPassword(employee.Password);
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateEmployeeAsync(EmployeeRoleEditViewModel model)
        {
            var employee = await _context.Employees.FindAsync(model.EmployeeId);
            if (employee == null) return false;
            employee.RoleId = model.RoleId;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task DeleteEmployeeAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<IEnumerable<Role>> GetRolesAsync()
        {
            return await _context.Roles.ToListAsync();
        }
    }
}
