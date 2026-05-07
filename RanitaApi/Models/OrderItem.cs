using System.Text.Json.Serialization;
namespace RanitaApi.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        [JsonIgnore]
        public Order? Order { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; } = "";

    }
}