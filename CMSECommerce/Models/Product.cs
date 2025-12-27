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

        // Owner (subscriber) who created the product
        public string OwnerName { get; set; }

        // Product workflow status
        public ProductStatus Status { get; set; } = ProductStatus.Approved;

        // Admin rejection reason (optional)
        public string RejectionReason { get; set; }

        // ✅ New property: stock quantity
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
        public int StockQuantity { get; set; }

        // ✅ Computed property: in stock status
        [NotMapped]
        public bool InStock => StockQuantity > 0;

        public ICollection<ProductReview> ProductReviews { get; set; } // To display existing reviews

        // New Calculated Properties (Use [NotMapped] if not storing in DB)
        [NotMapped]
        public double AverageRating => ProductReviews != null && ProductReviews.Any() ? ProductReviews.Average(r => r.Rating) : 0;

        [NotMapped]
        public int RatingCount => ProductReviews?.Count ?? 0;

        // Foreign Key for the User
        public string UserId { get; set; }

        // Navigation property
        [ForeignKey("UserId")]
        public virtual IdentityUser? User { get; set; }
    }
}
