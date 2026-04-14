using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMSECommerce.Models
{
    [Table("AsharaAzamEntries")]
    public class AsharaAzamEntry
    {
        [Key]
        [Required]
        [MaxLength(50)]
        public string ITSNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public bool ConsentGiven { get; set; }
        
        [Required]
        [MaxLength(1000)]
        public string ConsentMessage { get; set; } =
    "• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma Mein Maro Business 100% close raakhis.\n" +
    "• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma Mein Job Si Raza Lay-Lais.\n" +
    "• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma Mein Studies Si Raza Lay-Lais.\n" +
    "• Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma Qablal Waqt Majlis Ma Hazir Rahis";
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}

