namespace RanitaApi.Models
{
    public class FlashSaleRequest
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public Seller Seller { get; set; } = null!;
        public int ProductId { get; set; }
        public int OriginalVariantStock { get; set; }
        public Product Product { get; set; } = null!;
        public int? VariantId { get; set; }
        public ProductVariant? Variant { get; set; }
        public decimal FlashPrice { get; set; }
        public decimal OriginalPrice { get; set; }
        public int FlashStock { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}