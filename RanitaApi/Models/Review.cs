namespace RanitaApi.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int ClientId { get; set; }
        public Client? Client { get; set; }
        public int OrderId { get; set; }
        public int Note { get; set; } // 1 à 5
        public string Commentaire { get; set; } = "";
        public bool Approuve { get; set; } = true; // auto-approuvé
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}