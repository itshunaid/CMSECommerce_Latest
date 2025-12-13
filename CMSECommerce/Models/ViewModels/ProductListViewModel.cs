using CMSECommerce.DTOs;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace CMSECommerce.Models.ViewModels
{
    // Note: This is C# code you define in your project, not Razor.
    public class ProductListViewModel
    {
        // 1. Data needed for the main product/filtering section (kept for context)
        public IEnumerable<Product> Products { get; set; }       

        // 2. Data needed for the new sidebar user list
        // Replace 'ApplicationUser' with your actual Identity User class name
        public IEnumerable<UserStatusDto> AllUsers { get; set; }       

        // --- Product-Related Metadata (replacing ViewBags) ---
        public string CategoryName { get; set; }      // Replaces ViewBag.CategoryName
        public string CategorySlug { get; set; }      // Replaces ViewBag.CategorySlug
        public string CurrentSearchTerm { get; set; } // Replaces ViewBag.CurrentSearchTerm
        public string SortOrder { get; set; }         // Replaces ViewBag.SortOrder

        public int PageNumber { get; set; }           // Replaces ViewBag.PageNumber
        public int TotalPages { get; set; }           // Replaces ViewBag.TotalPages
        public int PageRange { get; set; } = 3;       // Replaces ViewBag.PageRange (default 3 is fine)

        // Optional: Only include if the count is needed separately from Products.Count()
        // public int TotalProducts { get; set; }

        // Chat related
        public IdentityUser CurrentUser { get; set; }
    }
}
