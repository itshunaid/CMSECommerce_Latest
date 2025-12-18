using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Areas.Admin.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly DataContext _context;

        public UserService(UserManager<IdentityUser> userManager,
                           RoleManager<IdentityRole> roleManager,
                           DataContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<ServiceResponse> UpdateUserFieldAsync(string userId, string fieldName, string value)
        {
            return fieldName switch
            {
                "Role" => await HandleRoleUpdate(userId, value),
                "IsLockedOut" => await HandleLockoutUpdate(userId, value),
                "Profession" or "IsProfileVisible" or "IsImageApproved" => await HandleProfileUpdate(userId, fieldName, value),
                _ => ServiceResponse.Failure($"Field '{fieldName}' is not supported.")
            };
        }

        private async Task<ServiceResponse> HandleRoleUpdate(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResponse.Failure("User not found", 404);

            var currentRoles = await _userManager.GetRolesAsync(user);

            // Remove existing roles
            if (currentRoles.Any()) await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Add new role
            if (!string.IsNullOrWhiteSpace(roleName))
            {
                if (!await _roleManager.RoleExistsAsync(roleName)) return ServiceResponse.Failure("Invalid Role");
                await _userManager.AddToRoleAsync(user, roleName);
            }

            return ServiceResponse.Success($"Role updated to {roleName}");
        }

        private async Task<ServiceResponse> HandleLockoutUpdate(string userId, string value)
        {
            if (!bool.TryParse(value, out bool isLockedOut))
                return ServiceResponse.Failure("Invalid boolean value");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResponse.Failure("User not found", 404);

            // Architect's Note: Use a transaction to ensure User and Products are updated together
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Update Identity User Lockout
                user.LockoutEnd = isLockedOut ? DateTimeOffset.MaxValue : null;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return ServiceResponse.Failure("Failed to update user security status");
                }

                // 2. Determine target Product Status
                // If locked (Disabled) -> Pending
                // If unlocked (Enabled) -> Approved (Active)
                var newProductStatus = isLockedOut ? ProductStatus.Pending : ProductStatus.Approved;

                // 3. Bulk Update Products 
                // We target both User.Id and User.UserName for maximum data safety
                var products = await _context.Products
                    .Where(p => p.OwnerId == user.Id || p.OwnerId == user.UserName)
                    .ToListAsync();

                if (products.Any())
                {
                    foreach (var product in products)
                    {
                        product.Status = newProductStatus;
                    }
                    _context.Products.UpdateRange(products);
                }

                // 4. Commit all changes
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                string action = isLockedOut ? "Disabled" : "Enabled";
                return ServiceResponse.Success($"User {action} and associated products set to {newProductStatus}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log the error (ensure ILogger is injected in your class)
                // _logger.LogError(ex, "Transaction failed for User Lockout update");
                return ServiceResponse.Failure("A system error occurred during the update transaction.");
            }
        }


        //private async Task<ServiceResponse> HandleLockoutUpdate(string userId, string value)
        //{
        //    if (!bool.TryParse(value, out bool isLockedOut)) return ServiceResponse.Failure("Invalid boolean value");

        //    var user = await _userManager.FindByIdAsync(userId);
        //    if (user == null) return ServiceResponse.Failure("User not found", 404);

        //    user.LockoutEnd = isLockedOut ? DateTimeOffset.MaxValue : null;
        //    var result = await _userManager.UpdateAsync(user);

        //    return result.Succeeded ? ServiceResponse.Success("Lock status updated") : ServiceResponse.Failure("Identity update failed");
        //}

        private async Task<ServiceResponse> HandleProfileUpdate(string userId, string field, string value)
        {
            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null) return ServiceResponse.Failure("Profile not found", 404);

            try
            {
                switch (field)
                {
                    case "Profession": profile.Profession = value; break;
                    case "IsProfileVisible": profile.IsProfileVisible = bool.Parse(value); break;
                    case "IsImageApproved": profile.IsImageApproved = bool.Parse(value); break;
                }
                await _context.SaveChangesAsync();
                return ServiceResponse.Success($"{field} updated successfully");
            }
            catch { return ServiceResponse.Failure("Database update failed"); }
        }
    }
}
