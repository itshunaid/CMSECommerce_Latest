namespace CMSECommerce.Models
{
    public enum RequestStatus { Pending, Approved, Rejected }
    public class SubscriptionRequest
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int TierId { get; set; }
        public string ItsNumber { get; set; }
        public string ReceiptImagePath { get; set; }
        public RequestStatus Status { get; set; } = RequestStatus.Pending;
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual SubscriptionTier Tier { get; set; }
    }
}
