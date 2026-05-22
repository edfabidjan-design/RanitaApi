namespace RanitaApi.Models
{
    public class FlashSale
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int? VariantId { get; set; }
        public ProductVariant? Variant { get; set; }
        public Product Product { get; set; } = null!;
        public decimal FlashPrice { get; set; }
        public decimal OriginalPrice { get; set; }
        public int FlashStock { get; set; }
        public int FlashStockSold { get; set; } = 0;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}