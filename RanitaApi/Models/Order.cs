namespace RanitaApi.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public string CustomerAddress { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public int? ClientId { get; set; }
        public Client? Client { get; set; }
        public decimal Total { get; set; }

        public string Status { get; set; } = "En attente";
        public string? RefundMotif { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<OrderItem> Items { get; set; } = new();
    }
}