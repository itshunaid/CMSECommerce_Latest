namespace CMSECommerce.Models
{
    public class Address
    {
        public int Id { get; set; }

        // Foreign Key to link to the IdentityUser
        public string UserId { get; set; }
        // Navigation Property for the User (Optional, but good practice)
        // public IdentityUser User { get; set; } 

        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
    }
}