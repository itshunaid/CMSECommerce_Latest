using CMSECommerce.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CMSECommerce.Infrastructure;

namespace CMSECommerce.Services
{
    public class ValidationService(IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager) : IValidationService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly UserManager<IdentityUser> _userManager = userManager;

        public async Task<bool> CheckUserNameUniqueAsync(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return true;
            var normalizedInput = userName.ToUpperInvariant();
            var userProfile = await _unitOfWork.Repository<UserProfile>().Find(up => up.ITSNumber == userName).FirstOrDefaultAsync();
            if (userProfile != null) return false;
            bool isTaken = await _userManager.Users.AnyAsync(u =>
                u.NormalizedUserName == normalizedInput ||
                u.NormalizedEmail == normalizedInput ||
                u.PhoneNumber == userName);
            return !isTaken;
        }

        public async Task<bool> CheckEmailUniqueAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return true;
            var emailAttribute = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
            if (!emailAttribute.IsValid(email)) return false;
            string normalizedEmail = email.ToUpperInvariant();
            bool isEmailTaken = await _userManager.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail);
            return !isEmailTaken;
        }

        public async Task<bool> CheckPhoneUniqueAsync(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber) || phoneNumber.Length != 10 || !long.TryParse(phoneNumber, out _)) return false;
            string normalizedInput = phoneNumber.ToUpperInvariant();
            bool isConflict = await _userManager.Users.AnyAsync(u =>
                u.PhoneNumber == phoneNumber ||
                u.NormalizedUserName == normalizedInput);
            return !isConflict;
        }

        public async Task<bool> ValidateIdentifierAsync(string value, string type)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            bool existsInIdentity = await _userManager.Users.AnyAsync(u =>
                u.UserName == value || u.Email == value || u.PhoneNumber == value);
            bool existsInProfile = await _unitOfWork.Repository<UserProfile>().Find(up =>
                up.ITSNumber == value || up.WhatsAppNumber == value).AnyAsync();
            bool existsInStore = await _unitOfWork.Repository<Store>().Find(s =>
                s.Email == value || s.Contact == value || s.GSTIN == value).AnyAsync();
            return !(existsInIdentity || existsInProfile || existsInStore);
        }
    }
}
