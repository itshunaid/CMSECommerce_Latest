using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity; // Required for IdentityUser

namespace CMSECommerce.Models
{
    // CMSECommerce.Models/ProductReview.cs

    public class ProductReview
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        // public Product Product { get; set; } // navigation property

        public string UserId { get; set; } // The Foreign Key

        // 🔥 FIX: Add the navigation property here
        // The type should be your custom user class (e.g., AppUser)
        public User User { get; set; } // The navigation property
        // public AppUser User { get; set; } // navigation property

        // Make sure the Comment property is present and NOT decorated with [NotMapped]
        // The [Required] attribute is good, as it matches the 'required' in the HTML.
        [Required(ErrorMessage = "Please write a comment for your review.")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Review must be between 10 and 1000 characters.")]
        public string Comment { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        public DateTime ReviewDate { get; set; } = DateTime.Now;
    }
}