using System.ComponentModel.DataAnnotations.Schema;

namespace CMSECommerce.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string PhoneNumber { get; set; }
        public bool Shipped { get; set; } = false;

        [Column(TypeName = "decimal(8, 2)")]
        public decimal GrandTotal { get; set; }

        public DateTime? OrderDate { get; set; } = DateTime.Now;

        public DateTime? ShippedDate { get; set; }
        // --- NEW PROPERTY ---
        public bool IsCancelled { get; set; } = false;
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
