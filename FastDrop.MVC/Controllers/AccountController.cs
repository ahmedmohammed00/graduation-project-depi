using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using FastDrop.MVC.Security;
using FastDrop.MVC.Models;
using FastDrop.MVC.ViewModels;
using FastDrop.MVC.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FastDrop.MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly JwtSettings _jwtSettings;
        private readonly IAccountSessionValidator _sessions;

        public AccountController(IAccountService accountService, JwtSettings jwtSettings, IAccountSessionValidator sessions)
        {
            _accountService = accountService;
            _jwtSettings = jwtSettings;
            _sessions = sessions;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Signup()
        {
            return View(new SignupViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Signup(SignupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if email exists
            bool emailExists = await _accountService.EmailExistsAsync(model.Email);
            if (emailExists)
            {
                ModelState.AddModelError(string.Empty, "Email is already registered.");
                return View(model);
            }

            await _accountService.CreateUserAsync(model);

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
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var claims = await _accountService.AuthenticateUserAsync(model);

            if (claims != null)
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var expires = DateTime.UtcNow.AddMinutes(30);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = expires,
                    Issuer = _jwtSettings.Issuer,
                    Audience = _jwtSettings.Audience,
                    SigningCredentials = new SigningCredentials(_jwtSettings.SigningKey, SecurityAlgorithms.HmacSha256Signature)
                };
                var token = tokenHandler.CreateToken(tokenDescriptor);
                var jwtString = tokenHandler.WriteToken(token);

                Response.Cookies.Append("jwt", jwtString, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = expires
                });

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                var roleClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (roleClaim == "SuperAdmin") return RedirectToAction("Index", "AdminPortal");
                if (roleClaim == "Dispatcher") return RedirectToAction("Index", "DispatcherPortal");
                if (roleClaim == "Employee") return RedirectToAction("Index", "Home"); // General employees who don't have a specific portal
                if (roleClaim == "Merchant") return RedirectToAction("Index", "MerchantPortal");
                if (roleClaim == "Driver") return RedirectToAction("Index", "DriverPortal");

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _sessions.RevokeAsync(User, HttpContext.RequestAborted);
            Response.Cookies.Delete("jwt");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
