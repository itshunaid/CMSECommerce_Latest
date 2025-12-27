using System.ComponentModel.DataAnnotations;

namespace CMSECommerce.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required, MinLength(2, ErrorMessage = "Minimum length is 2")]
        public string Name { get; set; }

        public string Slug { get; set; }

        // Navigation property for related products
        public virtual ICollection<Product> Products { get; set; }
    }
}
