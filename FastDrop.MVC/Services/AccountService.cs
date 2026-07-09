using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FastDrop.MVC.Models;
using FastDrop.MVC.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FastDrop.MVC.Services
{
    public class AccountService : IAccountService
    {
        private readonly DmsDbContext _context;

        public AccountService(DmsDbContext context)
        {
            _context = context;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            bool employeeExists = await _context.Employees.AnyAsync(e => e.Email == email);
            bool merchantExists = await _context.Merchants.AnyAsync(m => m.Email == email);
            return employeeExists || merchantExists;
        }

        public async Task CreateUserAsync(SignupViewModel model)
        {
            if (model.Role == "dispatcher" || model.Role == "driver")
            {
                var employee = new Employee
                {
                    Name = model.FullName,
                    Email = model.Email,
                    Password = model.Password,
                    AccountStatus = "active"
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                if (model.Role == "driver")
                {
                    var driver = new Driver
                    {
                        EmployeeId = employee.EmployeeId,
                        Name = model.FullName,
                        PhoneNumber = model.Phone,
                        LicenseNumber = "PENDING-" + Guid.NewGuid().ToString().Substring(0, 8),
                        CurrentStatus = "available"
                    };
                    _context.Drivers.Add(driver);
                    await _context.SaveChangesAsync();
                }
            }
            else if (model.Role == "merchant")
            {
                var merchant = new Merchant
                {
                    ShopName = model.ShopName,
                    ContactName = model.FullName,
                    Email = model.Email,
                    Password = model.Password,
                    Phone = model.Phone,
                    Status = "active"
                };

                _context.Merchants.Add(merchant);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Claim>?> AuthenticateUserAsync(LoginViewModel model)
        {
            var claims = new List<Claim>();
            string? role = null;
            int? userId = null;

            // 1. Check Employee Table
            var employee = await _context.Employees
                .Include(e => e.Role)
                .FirstOrDefaultAsync(e => e.Email == model.Email && e.Password == model.Password);

            if (employee != null)
            {
                role = employee.Role?.RoleName ?? "Employee";
                userId = employee.EmployeeId;
                claims.Add(new Claim(ClaimTypes.NameIdentifier, employee.EmployeeId.ToString()));
                claims.Add(new Claim(ClaimTypes.Name, employee.Name));
                claims.Add(new Claim(ClaimTypes.Email, employee.Email));
                claims.Add(new Claim(ClaimTypes.Role, role));

                // Check if this employee is ALSO a driver
                var driver = await _context.Drivers
                    .FirstOrDefaultAsync(d => d.EmployeeId == employee.EmployeeId);
                
                if (driver != null)
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Driver"));
                    // Store DriverId in a custom claim so we can easily look it up
                    claims.Add(new Claim("DriverId", driver.DriverId.ToString()));
                }
            }
            else
            {
                // 2. Check Merchant Table
                var merchant = await _context.Merchants
                    .FirstOrDefaultAsync(m => m.Email == model.Email && m.Password == model.Password);

                if (merchant != null)
                {
                    role = "Merchant";
                    userId = merchant.MerchantId;
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, merchant.MerchantId.ToString()));
                    claims.Add(new Claim(ClaimTypes.Name, merchant.ShopName));
                    claims.Add(new Claim(ClaimTypes.Email, merchant.Email));
                    claims.Add(new Claim(ClaimTypes.Role, "Merchant"));
                    claims.Add(new Claim("MerchantId", merchant.MerchantId.ToString()));
                }
            }

            if (role != null)
            {
                return claims;
            }

            return null;
        }
    }
}
