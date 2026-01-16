using CMSECommerce.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace CMSECommerce.Services
{
    public interface IAuthenticationService
    {
        Task<SignInResult> LoginAsync(string identifier, string password, bool rememberMe);
        Task<IdentityResult> RegisterAsync(RegisterViewModel model);
        Task LogoutAsync(string userId);
        Task<bool> VerifyOTPAsync(int registrationId, string otp);
    }
}
