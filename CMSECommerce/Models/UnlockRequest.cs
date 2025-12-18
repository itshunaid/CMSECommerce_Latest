using System.ComponentModel.DataAnnotations;

namespace CMSECommerce.Models
{
    public class UnlockRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string UserName { get; set; }

        public string Email { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        // Status: Pending, Approved, Rejected
        public string Status { get; set; } = "Pending";

        public string AdminNotes { get; set; }
    }
}
