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
        [MaxLength(200)]
        public string JammatName { get; set; } = string.Empty;
        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required]
        public bool ConsentGiven { get; set; }

        [Required]
        [MaxLength(1000)]
        public string ConsentMessage { get; set; } =
        "Mein Em Azam Karoon Choon Ke Ashara Mubaraka Ma Mein:\n" + "" +
        "Maro Business 100% close raakhis.\n" +
        "Job Si Raza Lay-Lais.\n" +
        "Studies Si Raza Lay-Lais.\n" +
        "Qablal Waqt Waaz ni Majalis Ma Hazir Rahis.";
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}

