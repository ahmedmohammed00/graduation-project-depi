using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FastDrop.MVC.Models;
using FastDrop.MVC.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FastDrop.MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly DmsDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(DmsDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signup(SignupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if email exists
            bool employeeExists = await _context.Employees.AnyAsync(e => e.Email == model.Email);
            bool merchantExists = await _context.Merchants.AnyAsync(m => m.Email == model.Email);
            if (employeeExists || merchantExists)
            {
                ModelState.AddModelError(string.Empty, "Email is already registered.");
                return View(model);
            }

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
                if (string.IsNullOrWhiteSpace(model.ShopName))
                {
                    ModelState.AddModelError("ShopName", "Shop Name is required for merchants.");
                    return View(model);
                }

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

            // Auto-login after signup
            var loginModel = new LoginViewModel
            {
                Email = model.Email,
                Password = model.Password
            };
            return await Login(loginModel, null);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var claims = new List<Claim>();
            string? role = null;
            int? userId = null;

            // 1. Check Employee Table
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Email == model.Email && e.Password == model.Password);

            if (employee != null)
            {
                role = "Employee";
                userId = employee.EmployeeId;
                claims.Add(new Claim(ClaimTypes.NameIdentifier, employee.EmployeeId.ToString()));
                claims.Add(new Claim(ClaimTypes.Name, employee.Name));
                claims.Add(new Claim(ClaimTypes.Email, employee.Email));
                claims.Add(new Claim(ClaimTypes.Role, "Employee"));

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
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]!);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddDays(7),
                    Issuer = _configuration["JwtSettings:Issuer"],
                    Audience = _configuration["JwtSettings:Audience"],
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var jwtString = tokenHandler.WriteToken(token);

                Response.Cookies.Append("jwt", jwtString, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7)
                });

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
