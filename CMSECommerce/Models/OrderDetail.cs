using System.ComponentModel.DataAnnotations.Schema;

namespace CMSECommerce.Models
{

    public enum OrderStatus
    {
        Pending,
        Accepted,
        Processing,
        Shipped,
        Cancelled,
        Returned
    }
    public class OrderDetail
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(8, 2)")]
        public decimal Price { get; set; }
        public string Image { get; set; }
        public int OrderId { get; set; }
        public bool IsProcessed { get; set; } = false;
        public string ProductOwner { get; set; } = "";
        public string Customer { get; set; } = "";
        public string CustomerNumber { get; set; } = "";

        // --- NEW PROPERTIES ---
        public bool IsCancelled { get; set; } = false;
       
        public string? CancelledByRole { get; set; } // Admin, Seller, or User

        public bool IsReturned { get; set; }        // Set to true when user clicks 'Return'
        public string ReturnReason { get; set; }     // Captured from the modal
        public DateTime? ReturnDate { get; set; }

        public virtual Order Order { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public string? SellerNote { get; set; }
        public string? DeliveryImageUrl { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public DateTime? ShippedDate { get; set; }

        public virtual Product Product { get; set; }
    }
}
