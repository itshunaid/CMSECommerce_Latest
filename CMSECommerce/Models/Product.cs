using CMSECommerce.Infrastructure.Validation;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMSECommerce.Models
{
    public enum ProductStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2
    }

    public class Product
    {
        public int Id { get; set; }

        [Required, MinLength(2, ErrorMessage = "Minimum length is 2")]
        public string Name { get; set; }

        public string Slug { get; set; }

        [Required, MinLength(8, ErrorMessage = "Minimum length is 8")]
        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(8, 2)")]
        public decimal Price { get; set; }

        [Required, Range(1, int.MaxValue, ErrorMessage = "You must choose a category")]
        public int CategoryId { get; set; }

        // Navigation property
        public Category Category { get; set; }

        public string Image { get; set; } = "noimage.png";

        [NotMapped]
        [FileExtension]
        public IFormFile ImageUpload { get; set; }

        [NotMapped]
        public IEnumerable<string> GalleryImages { get; set; }

        [Display(Name = "YouTube Video URL")]
        [Url(ErrorMessage = "Please enter a valid URL")]
        [RegularExpression(@"^(https?://)?(www\.)?(youtube\.com|youtu\.be)/.+$", ErrorMessage = "Only YouTube links are allowed")]
        public string? YoutubeUrl { get; set; }

        // Owner (subscriber) who created the product
        public string OwnerName { get; set; }

        // Product workflow status
        public ProductStatus Status { get; set; } = ProductStatus.Approved;

        // Admin rejection reason (optional)
        public string RejectionReason { get; set; }

        // ✅ New property: visibility for sellers
        public bool IsVisible { get; set; } = true;

        // ✅ New property: stock quantity
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
        public int StockQuantity { get; set; }

        // ✅ Computed property: in stock status
        [NotMapped]
        public bool InStock => StockQuantity > 0;

        public ICollection<Review> Reviews { get; set; } // To display existing reviews

        // New Calculated Properties (Use [NotMapped] if not storing in DB)
        [NotMapped]
        public double AverageRating => Reviews != null && Reviews.Any() ? Reviews.Average(r => r.Rating) : 0;

        [NotMapped]
        public int RatingCount => Reviews?.Count ?? 0;

        // Foreign Key for the User
        public string UserId { get; set; }

        // Navigation property
        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }

        // Foreign Key
        public int StoreId { get; set; }

        // Navigation Property
        [ForeignKey("StoreId")]
        public virtual Store Store { get; set; }
    }

    public class Review
    {
        public int Id { get; set; }

        // Foreign Key to the Product
        public int ProductId { get; set; }
        public Product Product { get; set; } // Navigation property back to Product

        // The User/AppUser ID of the reviewer
        public string UserId { get; set; }
        // public AppUser User { get; set; } // Navigation property to your Identity User model (recommended)

        [Required(ErrorMessage = "Please select a rating.")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; } // 1 to 5

        [Required(ErrorMessage = "Please provide a title.")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; } // Added Title property

        [Required(ErrorMessage = "Please enter your review comment.")]
        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
        public string Comment { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        // Display name of the user (can be pulled from the related User object)
        public string UserName { get; set; }
    }
}
