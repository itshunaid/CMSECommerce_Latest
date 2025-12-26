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

        public async Task<ServiceResponse> CreateUnlockRequestAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return ServiceResponse.Failure("User not found.");

            // Architect's Note: Check if a pending request already exists to prevent spam
            var existingRequest = await _context.UnlockRequests
                .AnyAsync(r => r.UserId == userId && r.Status == "Pending");

            if (existingRequest)
            {
                return ServiceResponse.Failure("You already have a pending request. Please wait for admin review.");
            }

            var newRequest = new UnlockRequest
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Status = "Pending",
                RequestDate = DateTime.UtcNow
            };

            await _context.UnlockRequests.AddAsync(newRequest);
            await _context.SaveChangesAsync();

            return ServiceResponse.Success("Your request has been submitted successfully.");
        }
        public async Task<ServiceResponse> ProcessUnlockRequestAsync(int requestId, string status, string adminNotes)
        {
            // 1. Fetch the specific request being processed
            var request = await _context.UnlockRequests.FindAsync(requestId);
            if (request == null) return ServiceResponse.Failure("Request not found.");

            if (status == "Approved")
            {
                // 2. We first update the Admin Notes so they are available 
                // when HandleLockoutUpdate saves changes inside its transaction.
                request.AdminNotes = adminNotes ?? "Approved by Admin";

                // 3. HandleLockoutUpdate handles the Transaction, User Lockout, 
                // Product Statuses, and sets the Request Status to "Approved".
                var result = await HandleLockoutUpdate(request.UserId, "false");

                if (!result.Succeeded) return result;
            }
            else
            {
                // 4. If Denied, we don't call HandleLockoutUpdate (since user stays locked).
                // We update the status and notes directly here.
                request.Status = "Denied";
                request.AdminNotes = adminNotes ?? "Denied by Admin";
                request.RequestDate = DateTime.Now; // Using this as the 'Processed' timestamp

                await _context.SaveChangesAsync();
            }

            return ServiceResponse.Success($"Request has been {status.ToLower()} successfully.");
        }

        public async Task<bool> IsUnlockRequestPendingAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;

            // We only care if there is a 'Pending' request. 
            // .AnyAsync is faster than .Where because it stops at the first match.
            return await _context.UnlockRequests
                .AnyAsync(r => r.UserId == userId && r.Status == "Pending" && r.Status == "Deny");
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

                // 2. Determine target statuses
                var newProductStatus = isLockedOut ? ProductStatus.Pending : ProductStatus.Approved;

                // --- NEW LOGIC: Update the UnlockRequest Status to Approved ---
                if (!isLockedOut) // Only if we are enabling/unlocking
                {
                    var unlockRequest = await _context.UnlockRequests
                        .Where(ur => ur.UserId == user.Id && ur.Status == "Pending")
                        .OrderByDescending(ur => ur.RequestDate)
                        .FirstOrDefaultAsync();

                    if (unlockRequest != null)
                    {
                        unlockRequest.Status = "Approved";
                        unlockRequest.RequestDate = DateTime.Now;
                        _context.UnlockRequests.Update(unlockRequest);
                    }
                }

                // 3. Bulk Update Products 
                var products = await _context.Products
                    .Where(p => p.OwnerName == user.Id || p.OwnerName == user.UserName)
                    .ToListAsync();

                if (products.Any())
                {
                    foreach (var product in products)
                    {
                        product.Status = newProductStatus;
                    }
                    _context.Products.UpdateRange(products);
                }

                // 4. Commit all changes (User update, UnlockRequest update, and Product updates)
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                string action = isLockedOut ? "Disabled" : "Enabled";
                return ServiceResponse.Success($"User {action}, associated products set to {newProductStatus}, and request set to Approved.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
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
