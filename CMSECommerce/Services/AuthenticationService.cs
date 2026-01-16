using CMSECommerce.DTOs;
using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using CMSECommerce.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CMSECommerce.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IUserStatusService _userStatusService;

        public AuthenticationService(
            IUnitOfWork unitOfWork,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IUserStatusService userStatusService)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _signInManager = signInManager;
            _userStatusService = userStatusService;
        }

        public async Task<SignInResult> LoginAsync(string identifier, string password, bool rememberMe)
        {
            IdentityUser user = null;

            if (string.IsNullOrEmpty(identifier))
            {
                throw new ArgumentException("Please enter your username, email, ITS or mobile.");
            }

            // Try email
            if (identifier.Contains("@"))
            {
                user = await _userManager.FindByEmailAsync(identifier);
            }

            // Try ITS (8 digits) via UserProfile
            if (user == null && identifier.All(char.IsDigit) && identifier.Length == 8)
            {
                var profile = await _unitOfWork.Repository<UserProfile>().FirstOrDefaultAsync(p => p.ITSNumber == identifier);
                if (profile != null)
                {
                    user = await _userManager.FindByIdAsync(profile.UserId);
                }
            }

            // Try username
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(identifier);
            }

            // Try phone number
            if (user == null)
            {
                user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == identifier);
            }

            if (user == null)
            {
                throw new Exception("Invalid login attempt.");
            }

            // Check lockout
            if (await _userManager.IsLockedOutAsync(user))
            {
                throw new Exception("Account is locked out.");
            }

            var signInResult = await _signInManager.PasswordSignInAsync(user.UserName, password, rememberMe, lockoutOnFailure: true);

            if (signInResult.Succeeded)
            {
                try
                {
                    var status = await _unitOfWork.Repository<UserStatusTracker>().GetByIdAsync(user.Id);
                    if (status != null)
                    {
                        status.IsOnline = true;
                        status.LastActivity = DateTime.UtcNow;
                        await _unitOfWork.SaveChangesAsync();
                    }
                }
                catch { }
            }

            return signInResult;
        }

        public async Task<IdentityResult> RegisterAsync(RegisterViewModel model)
        {
            if (!model.HasAcceptedTerms)
            {
                throw new Exception("You must agree to the terms.");
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByNameAsync(model.Username);
            if (existingUser != null)
            {
                throw new Exception("Username already exists. Please login instead.");
            }

            // Check uniqueness
            bool isDuplicate = await _userManager.Users.AnyAsync(u =>
                u.UserName == model.Username ||
                u.Email == model.Email ||
                u.PhoneNumber == model.PhoneNumber);

            if (isDuplicate)
            {
                throw new Exception("Username, Email, or Phone Number is already in use.");
            }

            // Create Identity User
            var newUser = new IdentityUser
            {
                UserName = model.Username,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(newUser, model.Password);

            if (result.Succeeded)
            {
                // Save profile
                var userProfile = new UserProfile
                {
                    UserId = newUser.Id,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    ITSNumber = model.ITSNumber,
                    WhatsAppNumber = model.WhatsAppNumber,
                    Store = null,
                    IsProfileVisible = true,
                    IsImageApproved = false
                };

                await _unitOfWork.Repository<UserProfile>().AddAsync(userProfile);
                await _unitOfWork.SaveChangesAsync();

                // Role assignment
                await _userManager.AddToRoleAsync(newUser, "Customer");
            }

            return result;
        }

        public async Task LogoutAsync(string userId)
        {
            await _signInManager.SignOutAsync();

            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    var status = await _unitOfWork.Repository<UserStatusTracker>().GetByIdAsync(userId);
                    if (status != null)
                    {
                        status.IsOnline = false;
                        status.LastActivity = DateTime.UtcNow;
                        await _unitOfWork.SaveChangesAsync();
                    }
                }
                catch { }
            }
        }

        public async Task<bool> VerifyOTPAsync(int registrationId, string otp)
        {
            var profile = await _unitOfWork.Repository<UserProfile>().GetByIdAsync(registrationId);
            if (profile == null)
            {
                throw new Exception("Invalid registration session.");
            }

            // For now, assume OTP is always valid (implement actual OTP logic later)
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
