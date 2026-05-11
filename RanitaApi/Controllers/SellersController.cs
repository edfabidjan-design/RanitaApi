using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.DTOs;
using RanitaApi.Models;
using System.Text.Json;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SellersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SellersController(AppDbContext db)
        {
            _db = db;
        }

        // ── INSCRIPTION VENDEUR ───────────────────────────────────────────
        // POST /api/sellers/register
        // Le client doit être connecté — on passe son ClientId dans le body
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] SellerRegisterDto dto, [FromQuery] int clientId)
        {
            var client = await _db.Clients.FindAsync(clientId);
            if (client == null)
                return NotFound(new { message = "Client introuvable" });

            // Un client ne peut avoir qu'une seule boutique
            var existing = await _db.Sellers.FirstOrDefaultAsync(s => s.ClientId == clientId);
            if (existing != null)
                return BadRequest(new { message = "Vous avez déjà soumis une demande de boutique", status = existing.Status });

            var seller = new Seller
            {
                ClientId = clientId,
                ShopName = dto.ShopName.Trim(),
                ShopDescription = dto.ShopDescription?.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                NationalIdNumber = dto.NationalIdNumber.Trim(),
                PaymentMethod = dto.PaymentMethod,
                PaymentDetails = dto.PaymentDetails,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Sellers.Add(seller);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Demande envoyée, en attente de validation", sellerId = seller.Id });
        }

        // ── STATUT VENDEUR (pour le client) ──────────────────────────────
        // GET /api/sellers/my?clientId=5
        [HttpGet("my")]
        public async Task<IActionResult> GetMySeller([FromQuery] int clientId)
        {
            var seller = await _db.Sellers
                .Include(s => s.Client)
                .Include(s => s.SellerProducts)
                .Include(s => s.Payouts)
                .FirstOrDefaultAsync(s => s.ClientId == clientId);

            if (seller == null)
                return NotFound(new { message = "Aucune boutique trouvée" });

            var dto = MapToDto(seller);
            return Ok(dto);
        }

        // ── PRODUITS DU VENDEUR ───────────────────────────────────────────
        // GET /api/sellers/{sellerId}/products
        [HttpGet("{sellerId}/products")]
        public async Task<IActionResult> GetMyProducts(int sellerId)
        {
            var products = await _db.SellerProducts
                .Where(p => p.SellerId == sellerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var result = products.Select(p => MapProductToDto(p, "")).ToList();
            return Ok(result);
        }

        // ── SOUMETTRE UN PRODUIT ──────────────────────────────────────────
        // POST /api/sellers/{sellerId}/products
        [HttpPost("{sellerId}/products")]
        public async Task<IActionResult> SubmitProduct(int sellerId, [FromBody] SellerProductCreateDto dto)
        {
            var seller = await _db.Sellers.FindAsync(sellerId);
            if (seller == null)
                return NotFound(new { message = "Boutique introuvable" });

            if (seller.Status != "Approved")
                return BadRequest(new { message = "Votre boutique doit être approuvée pour soumettre des produits" });

            var product = new SellerProduct
            {
                SellerId = sellerId,
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Price = dto.Price,
                OldPrice = dto.OldPrice,
                Stock = dto.Stock,
                Category = dto.Category,
                Images = JsonSerializer.Serialize(dto.Images),
                ApprovalStatus = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.SellerProducts.Add(product);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Produit soumis, en attente de validation", productId = product.Id });
        }

        // ── PAYOUTS DU VENDEUR ────────────────────────────────────────────
        // GET /api/sellers/{sellerId}/payouts
        [HttpGet("{sellerId}/payouts")]
        public async Task<IActionResult> GetPayouts(int sellerId)
        {
            var payouts = await _db.SellerPayouts
                .Include(p => p.Seller)
                .Where(p => p.SellerId == sellerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var result = payouts.Select(p => new PayoutDto
            {
                Id = p.Id,
                SellerId = p.SellerId,
                ShopName = p.Seller?.ShopName ?? "",
                OrderId = p.OrderId,
                GrossAmount = p.GrossAmount,
                CommissionAmount = p.CommissionAmount,
                NetAmount = p.NetAmount,
                Status = p.Status,
                TransactionReference = p.TransactionReference,
                CreatedAt = p.CreatedAt,
                PaidAt = p.PaidAt
            }).ToList();

            return Ok(result);
        }

        // ── HELPERS ───────────────────────────────────────────────────────

        private static SellerDto MapToDto(Seller s)
        {
            return new SellerDto
            {
                Id = s.Id,
                ClientId = s.ClientId,
                ClientName = s.Client?.FullName ?? "",
                ClientEmail = s.Client?.Email ?? "",
                ShopName = s.ShopName,
                ShopDescription = s.ShopDescription,
                PhoneNumber = s.PhoneNumber,
                NationalIdNumber = s.NationalIdNumber,
                ShopLogoUrl = s.ShopLogoUrl,
                CommissionRate = s.CommissionRate,
                PaymentMethod = s.PaymentMethod,
                PaymentDetails = s.PaymentDetails,
                Status = s.Status,
                RejectionReason = s.RejectionReason,
                CreatedAt = s.CreatedAt,
                TotalProducts = s.SellerProducts.Count(p => p.ApprovalStatus == "Approved"),
                PendingProducts = s.SellerProducts.Count(p => p.ApprovalStatus == "Pending"),
                TotalEarnings = s.Payouts.Where(p => p.Status == "Paid").Sum(p => p.NetAmount),
                PendingPayouts = s.Payouts.Where(p => p.Status == "Pending").Sum(p => p.NetAmount)
            };
        }

        private static SellerProductDto MapProductToDto(SellerProduct p, string shopName)
        {
            List<string> images;
            try { images = JsonSerializer.Deserialize<List<string>>(p.Images) ?? new(); }
            catch { images = new(); }

            return new SellerProductDto
            {
                Id = p.Id,
                SellerId = p.SellerId,
                ShopName = shopName,
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                OldPrice = p.OldPrice,
                Stock = p.Stock,
                Category = p.Category,
                Images = images,
                ApprovalStatus = p.ApprovalStatus,
                RejectionReason = p.RejectionReason,
                CreatedAt = p.CreatedAt
            };
        }
    }
}
