using System.ComponentModel.DataAnnotations;

namespace RanitaApi.DTOs
{
    // ─── INSCRIPTION ──────────────────────────────────────────────────────

    public class SellerRegisterDto
    {
        [Required(ErrorMessage = "Le nom de la boutique est requis")]
        public string ShopName { get; set; } = string.Empty;

        public string? ShopDescription { get; set; }

        [Required(ErrorMessage = "Le téléphone est requis")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le numéro CNI est requis")]
        public string NationalIdNumber { get; set; } = string.Empty;

        public string? PaymentMethod { get; set; }
        public string? PaymentDetails { get; set; }
    }

    // ─── RÉPONSES ────────────────────────────────────────────────────────

    public class SellerDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public string? ShopDescription { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string NationalIdNumber { get; set; } = string.Empty;
        public string? ShopLogoUrl { get; set; }
        public decimal CommissionRate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentDetails { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        // Stats pour le dashboard vendeur
        public int TotalProducts { get; set; }
        public int PendingProducts { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal PendingPayouts { get; set; }
    }

    // ─── VALIDATION ADMIN ────────────────────────────────────────────────

    public class ReviewSellerDto
    {
        [Required]
        public bool Approved { get; set; }

        public string? RejectionReason { get; set; }

        // Optionnel : personnaliser la commission pour ce vendeur
        public decimal? CommissionRate { get; set; }
    }

    // ─── PRODUITS VENDEUR ────────────────────────────────────────────────

    public class SellerProductCreateDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "Le prix doit être positif")]
        public decimal Price { get; set; }

        public decimal? OldPrice { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        public string? Category { get; set; }

        // Liste d'URLs images
        public List<string> Images { get; set; } = new();
    }

    public class SellerProductDto
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public int? ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? OldPrice { get; set; }
        public int Stock { get; set; }
        public string? Category { get; set; }
        public List<string> Images { get; set; } = new();
        public string ApprovalStatus { get; set; } = string.Empty;
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Sku { get; set; }
        public string? Brand { get; set; }
    }

    public class ReviewProductDto
    {
        [Required]
        public bool Approved { get; set; }
        public string? RejectionReason { get; set; }
    }

    // ─── PAYOUTS ─────────────────────────────────────────────────────────

    public class PayoutDto
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public int? OrderId { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal CommissionAmount { get; set; }
        public decimal NetAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? TransactionReference { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
    }

    public class MarkPayoutPaidDto
    {
        [Required]
        public string TransactionReference { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }


    public class SellerProductFormDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ShortDescription { get; set; }
        public string? Price { get; set; }  // ← string au lieu de decimal
        public string? OldPrice { get; set; }  // ← string au lieu de decimal?
        public string? Stock { get; set; }  // ← string au lieu de int
        public string? Category { get; set; }
        public string? Sku { get; set; }
        public string? Brand { get; set; }
        public string? Attributes { get; set; }
        public string? Images { get; set; }
        public string? Variants { get; set; }
        public List<IFormFile>? ImageFiles { get; set; }
    }
