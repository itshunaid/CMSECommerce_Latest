using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Models
{
    [Index(nameof(Slug), IsUnique = true)]
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Slug { get; set; }

        // --- Hierarchical Properties ---

        /// <summary>
        /// Nullable ParentId: If null, this is a Top-Level Root Category.
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// Navigation property to the parent category.
        /// </summary>
        [ForeignKey("ParentId")]
        public virtual Category Parent { get; set; }

        /// <summary>
        /// Collection of child sub-categories.
        /// </summary>
        public virtual ICollection<Category> Children { get; set; } = new List<Category>();

        // Optional: Helpful for UI breadcrumbs or SEO
        public int Level { get; set; }
    }
}