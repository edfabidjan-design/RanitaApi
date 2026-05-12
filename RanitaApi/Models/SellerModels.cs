using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RanitaApi.Models
{
    // ── Statuts (pas d'enum séparé, on utilise des string comme tu fais avec Order.Status) ──

    public class Seller
    {
        public int Id { get; set; }

        // Lien vers Clients (un vendeur est forcément un client existant)
        public int ClientId { get; set; }

        [Required]
        public string ShopName { get; set; } = string.Empty;

        public string? ShopDescription { get; set; }

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string NationalIdNumber { get; set; } = string.Empty;

        public string? ShopLogoUrl { get; set; }

        // Ex: 0.10 = 10% de commission Ranita
        [Column(TypeName = "numeric(5,4)")]
        public decimal CommissionRate { get; set; } = 0.10m;

        // "ORANGE_MONEY" | "MTN_MONEY" | "WAVE" | "BANK"
        public string? PaymentMethod { get; set; }

        // Numéro Mobile Money ou IBAN
        public string? PaymentDetails { get; set; }

        // "Pending" | "Approved" | "Rejected" | "Suspended"
        public string Status { get; set; } = "Pending";

        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Client? Client { get; set; }
        public ICollection<SellerProduct> SellerProducts { get; set; } = new List<SellerProduct>();
        public ICollection<SellerPayout> Payouts { get; set; } = new List<SellerPayout>();
    }

    public class SellerProduct
    {
        public int Id { get; set; }
        public int SellerId { get; set; }

        // Rempli après approbation admin (FK vers ta table Products existante)
        public int? ProductId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal? OldPrice { get; set; }

        public int Stock { get; set; } = 0;

        public string? Category { get; set; }
        public string? Sku { get; set; }
        public string? Brand { get; set; }
        public string? ShortDescription { get; set; }

        // JSON array d'URLs, comme tu fais avec Images sur Product
        public string Images { get; set; } = "[]";

        // "Pending" | "Approved" | "Rejected"
        public string ApprovalStatus { get; set; } = "Pending";

        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Seller? Seller { get; set; }
    }

    public class SellerPayout
    {
        public int Id { get; set; }
        public int SellerId { get; set; }

        // La commande qui a déclenché ce reversement
        public int? OrderId { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal GrossAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal CommissionAmount { get; set; }

        [Column(TypeName = "numeric(18,2)")]
        public decimal NetAmount { get; set; }

        // "Pending" | "Processing" | "Paid" | "Failed"
        public string Status { get; set; } = "Pending";

        // Référence transaction Mobile Money / virement
        public string? TransactionReference { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }

        // Navigation
        public Seller? Seller { get; set; }
    }
}
