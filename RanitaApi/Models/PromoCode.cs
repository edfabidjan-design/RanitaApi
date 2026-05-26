namespace RanitaApi.Models
{
    public class PromoCode
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Type { get; set; } = "percent"; // percent | fixed
        public decimal Value { get; set; }
        public decimal? MinOrder { get; set; }
        public int? MaxUses { get; set; }
        public int UsedCount { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
