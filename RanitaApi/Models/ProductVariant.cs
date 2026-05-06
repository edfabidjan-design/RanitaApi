namespace RanitaApi.Models
{
    public class ProductVariant
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Combination { get; set; } = ""; // "S/Blanc"
        public int Stock { get; set; }
        public decimal? Price { get; set; } // Prix spécifique si différent
    }
}