namespace CMSECommerce.Areas.Admin.Services
{
    public interface IUserService
    {
        Task<ServiceResponse> UpdateUserFieldAsync(string userId, string fieldName, string value);
    }
}
