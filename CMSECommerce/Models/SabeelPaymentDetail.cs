using System.ComponentModel.DataAnnotations;

namespace CMSECommerce.Models
{
    public class SabeelPaymentDetail
    {
        // Must have a public parameterless constructor
        public SabeelPaymentDetail() { }

        public int Id { get; set; }

        // Properties MUST have { get; set; }
        public string FullName { get; set; }
        public string ITSNumber { get; set; }
        public decimal Amount { get; set; }
        public string UTRNumber { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsVerified { get; set; }
    }
}
