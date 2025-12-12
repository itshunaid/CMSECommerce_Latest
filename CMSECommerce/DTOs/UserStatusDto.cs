using Microsoft.AspNetCore.Identity;

namespace CMSECommerce.DTOs
{
    // In a ViewModels folder or similar location

    public class UserStatusDto
    {
        // Holds the original user data (e.g., Id, Email, UserName)
        public IdentityUser User { get; set; }

        // The property the Razor view needs
        public bool IsOnline { get; set; }
    }
}
