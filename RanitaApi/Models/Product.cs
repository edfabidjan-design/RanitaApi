using System.Text.Json.Serialization;

namespace RanitaApi.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Price { get; set; }
        public decimal? OldPrice { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; } = true;
        public string Brand { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public string Slug { get; set; } = "";
        public string MetaDescription { get; set; } = "";
        public string Attributes { get; set; } = "{}";
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
        public string Sku { get; set; } = "";
    }
}