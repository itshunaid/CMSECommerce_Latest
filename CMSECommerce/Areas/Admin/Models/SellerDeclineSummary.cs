namespace CMSECommerce.Areas.Admin.Models
{
    public class SellerDeclineSummary
    {
        public string SellerName { get; set; }
        public int ManualDeclines { get; set; }
        public int AutoDeclines { get; set; }
        public int TotalDeclines { get; set; }
    }
}
