namespace CMSECommerce.Areas.Admin.Services
{
    public interface IUserService
    {
        Task<ServiceResponse> UpdateUserFieldAsync(string userId, string fieldName, string value);
        Task<ServiceResponse> CreateUnlockRequestAsync(string userId);
        Task<ServiceResponse> ProcessUnlockRequestAsync(int requestId, string status, string adminNotes);
        Task<bool> IsUnlockRequestPendingAsync(string userId);
    }
}
