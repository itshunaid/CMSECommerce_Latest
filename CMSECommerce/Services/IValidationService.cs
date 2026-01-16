using System.Threading.Tasks;

namespace CMSECommerce.Services
{
    public interface IValidationService
    {
        Task<bool> CheckUserNameUniqueAsync(string userName);
        Task<bool> CheckEmailUniqueAsync(string email);
        Task<bool> CheckPhoneUniqueAsync(string phoneNumber);
        Task<bool> ValidateIdentifierAsync(string value, string type);
    }
}
