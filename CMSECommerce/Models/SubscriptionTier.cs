namespace CMSECommerce.Models
{
    public class SubscriptionTier
    {
        public int Id { get; set; }
        public string Name { get; set; } // Basic, Intermediate, Premium
        public decimal Price { get; set; }
        public int DurationMonths { get; set; }
        public int ProductLimit { get; set; }
    }
}
