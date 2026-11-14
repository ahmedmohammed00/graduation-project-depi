using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
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
            // Enforce the public registration boundary even outside MVC validation.
            if (model.Role != "merchant")
            {
                throw new ValidationException("Public signup is available for merchants only.");
            }
            if (string.IsNullOrWhiteSpace(model.ShopName))
            {
                throw new ValidationException("Shop Name is required for merchants.");
            }

            var merchant = new Merchant
            {
                ShopName = model.ShopName,
                ContactName = model.FullName,
                Email = model.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Phone = model.Phone,
                Status = "active"
            };

            _context.Merchants.Add(merchant);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Claim>?> AuthenticateUserAsync(LoginViewModel model)
        {
            var claims = new List<Claim>();
            string? role = null;

            // 1. Check Employee Table
            var employee = await _context.Employees
                .Include(e => e.Role)
                .FirstOrDefaultAsync(e => e.Email == model.Email);

            if (employee != null)
            {
                bool isValid = false;
                try
                {
                    isValid = BCrypt.Net.BCrypt.Verify(model.Password, employee.Password);
                }
                catch (BCrypt.Net.SaltParseException)
                {
                    // Fallback to plain text check for legacy passwords
                    if (employee.Password == model.Password)
                    {
                        isValid = true;
                        // Seamless upgrade: Hash and save the password immediately
                        employee.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                        await _context.SaveChangesAsync();
                    }
                }

                if (isValid)
                {
                    var driver = await _context.Drivers
                        .FirstOrDefaultAsync(d => d.EmployeeId == employee.EmployeeId);
                    role = employee.Role?.RoleName;
                    if (string.IsNullOrWhiteSpace(role))
                    {
                        if (driver == null) return null;
                        role = "Driver";
                    }
                    if (role == "Driver" && driver == null) return null;

                    claims.Add(new Claim(ClaimTypes.NameIdentifier, employee.EmployeeId.ToString()));
                    claims.Add(new Claim(ClaimTypes.Name, employee.Name));
                    claims.Add(new Claim(ClaimTypes.Email, employee.Email));
                    claims.Add(new Claim(ClaimTypes.Role, role));

                    // Check if this employee is ALSO a driver
                    if (driver != null)
                    {
                        if (role != "Driver") claims.Add(new Claim(ClaimTypes.Role, "Driver"));
                        // Store DriverId in a custom claim so we can easily look it up
                        claims.Add(new Claim("DriverId", driver.DriverId.ToString()));
                    }
                }
            }

            if (role == null)
            {
                // 2. Check Merchant Table
                var merchant = await _context.Merchants
                    .FirstOrDefaultAsync(m => m.Email == model.Email);

                if (merchant != null)
                {
                    bool isValid = false;
                    try
                    {
                        isValid = BCrypt.Net.BCrypt.Verify(model.Password, merchant.Password);
                    }
                    catch (BCrypt.Net.SaltParseException)
                    {
                        // Fallback to plain text check for legacy passwords
                        if (merchant.Password == model.Password)
                        {
                            isValid = true;
                            // Seamless upgrade: Hash and save the password immediately
                            merchant.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
                            await _context.SaveChangesAsync();
                        }
                    }

                    if (isValid)
                    {
                        role = "Merchant";
                        claims.Add(new Claim(ClaimTypes.NameIdentifier, merchant.MerchantId.ToString()));
                        claims.Add(new Claim(ClaimTypes.Name, merchant.ShopName));
                        claims.Add(new Claim(ClaimTypes.Email, merchant.Email));
                        claims.Add(new Claim(ClaimTypes.Role, "Merchant"));
                        claims.Add(new Claim("MerchantId", merchant.MerchantId.ToString()));
                    }
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
