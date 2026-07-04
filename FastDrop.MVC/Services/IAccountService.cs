using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using FastDrop.MVC.ViewModels;

namespace FastDrop.MVC.Services
{
    public interface IAccountService
    {
        Task<bool> EmailExistsAsync(string email);
        Task CreateUserAsync(SignupViewModel model);
        Task<List<Claim>?> AuthenticateUserAsync(LoginViewModel model);
    }
}
