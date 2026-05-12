using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.DTOs;
using RanitaApi.Models;
using System.Text.Json;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/admin/sellers")]
    public class AdminSellersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminSellersController(AppDbContext db)
        {
            _db = db;
        }

        // ── LISTE VENDEURS ────────────────────────────────────────────────
        // GET /api/admin/sellers?status=Pending
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status)
        {
            var query = _db.Sellers
                .Include(s => s.Client)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(s => s.Status == status);

            var sellers = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var result = sellers.Select(s => new
            {
                s.Id,
                s.ClientId,
                ClientName = s.Client?.FullName ?? "",
                ClientEmail = s.Client?.Email ?? "",
                s.ShopName,
                s.ShopDescription,   // ← AJOUTER
                s.PhoneNumber,
                s.NationalIdNumber,
                s.CommissionRate,
                s.PaymentMethod,     // ← AJOUTER
                s.PaymentDetails,    // ← AJOUTER
                s.Status,
                s.RejectionReason,
                s.CreatedAt
            });

            return Ok(result);
        }

        // ── BADGE COMPTEUR (pour la nav admin) ───────────────────────────
        // GET /api/admin/sellers/pending-count
        [HttpGet("pending-count")]
        public async Task<IActionResult> GetPendingCount()
        {
            var count = await _db.Sellers.CountAsync(s => s.Status == "Pending");
            return Ok(new { count });
        }

        // ── DÉTAIL VENDEUR ────────────────────────────────────────────────
        // GET /api/admin/sellers/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOne(int id)
        {
            var seller = await _db.Sellers
                .Include(s => s.Client)
                .Include(s => s.SellerProducts)
                .Include(s => s.Payouts)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (seller == null) return NotFound();

            return Ok(new
            {
                seller.Id,
                seller.ClientId,
                ClientName = seller.Client?.FullName ?? "",
                ClientEmail = seller.Client?.Email ?? "",
                seller.ShopName,
                seller.ShopDescription,
                seller.PhoneNumber,
                seller.NationalIdNumber,
                seller.ShopLogoUrl,
                seller.CommissionRate,
                seller.PaymentMethod,
                seller.PaymentDetails,
                seller.Status,
                seller.RejectionReason,
                seller.CreatedAt,
                TotalProducts = seller.SellerProducts.Count,
                PendingProducts = seller.SellerProducts.Count(p => p.ApprovalStatus == "Pending"),
                TotalEarnings = seller.Payouts.Where(p => p.Status == "Paid").Sum(p => p.NetAmount),
                PendingPayouts = seller.Payouts.Where(p => p.Status == "Pending").Sum(p => p.NetAmount)
            });
        }

        // ── APPROUVER / REFUSER VENDEUR ───────────────────────────────────
        // PUT /api/admin/sellers/{id}/review
        [HttpPut("{id}/review")]
        public async Task<IActionResult> ReviewSeller(int id, [FromBody] ReviewSellerDto dto)
        {
            var seller = await _db.Sellers.FindAsync(id);
            if (seller == null) return NotFound();

            seller.Status = dto.Approved ? "Approved" : "Rejected";
            seller.UpdatedAt = DateTime.UtcNow;

            if (!dto.Approved)
                seller.RejectionReason = dto.RejectionReason;

            if (dto.CommissionRate.HasValue)
                seller.CommissionRate = dto.CommissionRate.Value;

            await _db.SaveChangesAsync();

            var action = dto.Approved ? "approuvée" : "refusée";
            return Ok(new { message = $"Boutique {action}", status = seller.Status });
        }

        // ── SUSPENDRE / RÉACTIVER ─────────────────────────────────────────
        // PUT /api/admin/sellers/{id}/suspend
        [HttpPut("{id}/suspend")]
        public async Task<IActionResult> Suspend(int id)
        {
            var seller = await _db.Sellers.FindAsync(id);
            if (seller == null) return NotFound();

            seller.Status = seller.Status == "Suspended" ? "Approved" : "Suspended";
            seller.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { message = $"Boutique {seller.Status.ToLower()}", status = seller.Status });
        }

        // ══════════════════════════════════════════════════════════════════
        // MODÉRATION PRODUITS
        // ══════════════════════════════════════════════════════════════════

        // GET /api/admin/sellers/products?status=Pending
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts([FromQuery] string? status)
        {
            var query = _db.SellerProducts
                .Include(p => p.Seller)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.ApprovalStatus == status);

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var result = products.Select(p =>
            {
                List<string> images;
                try { images = JsonSerializer.Deserialize<List<string>>(p.Images) ?? new(); }
                catch { images = new(); }

                return new
                {
                    p.Id,
                    p.SellerId,
                    ShopName = p.Seller?.ShopName ?? "",
                    p.ProductId,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.OldPrice,
                    p.Stock,
                    p.Category,
                    Images = images,
                    p.ApprovalStatus,
                    p.RejectionReason,
                    p.CreatedAt
                };
            });

            return Ok(result);
        }

        // Badge produits en attente
        // GET /api/admin/sellers/products/pending-count
        [HttpGet("products/pending-count")]
        public async Task<IActionResult> GetProductsPendingCount()
        {
            var count = await _db.SellerProducts.CountAsync(p => p.ApprovalStatus == "Pending");
            return Ok(new { count });
        }

        // ── APPROUVER / REFUSER UN PRODUIT ────────────────────────────────
        // PUT /api/admin/sellers/products/{id}/review
        [HttpPut("products/{id}/review")]
        public async Task<IActionResult> ReviewProduct(int id, [FromBody] ReviewProductDto dto)
        {
            var product = await _db.SellerProducts
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            product.ApprovalStatus = dto.Approved ? "Approved" : "Rejected";
            product.UpdatedAt = DateTime.UtcNow;

            if (!dto.Approved)
                product.RejectionReason = dto.RejectionReason;

            // Si approuvé : créer le produit dans la table Products principale
            if (dto.Approved && product.ProductId == null && product.Seller != null)
            {
                var newProduct = new Product
                {
                    Name = product.Name,
                    Description = product.Description ?? "",
                    ShortDescription = "",
                    Price = product.Price,
                    OldPrice = product.OldPrice,
                    Stock = product.Stock,
                    Images = product.Images,
                    ImageUrl = "",
                    IsActive = true,
                    Brand = product.Seller.ShopName,
                    Slug = GenerateSlug(product.Name),
                    MetaDescription = "",
                    Sku = $"SELL-{product.SellerId}-{product.Id}",
                };

                _db.Products.Add(newProduct);
                await _db.SaveChangesAsync();

                product.ProductId = newProduct.Id;
            }

            await _db.SaveChangesAsync();

            var action = dto.Approved ? "approuvé et publié" : "refusé";
            return Ok(new { message = $"Produit {action}", productId = product.ProductId });
        }

        // ══════════════════════════════════════════════════════════════════
        // PAYOUTS
        // ══════════════════════════════════════════════════════════════════

        // GET /api/admin/sellers/payouts?status=Pending
        [HttpGet("payouts")]
        public async Task<IActionResult> GetPayouts([FromQuery] string? status)
        {
            var query = _db.SellerPayouts
                .Include(p => p.Seller)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            var payouts = await query
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
            });

            return Ok(result);
        }

        // ── MARQUER UN PAYOUT PAYÉ ────────────────────────────────────────
        // PUT /api/admin/sellers/payouts/{id}/paid
        [HttpPut("payouts/{id}/paid")]
        public async Task<IActionResult> MarkPaid(int id, [FromBody] MarkPayoutPaidDto dto)
        {
            var payout = await _db.SellerPayouts.FindAsync(id);
            if (payout == null) return NotFound();

            payout.Status = "Paid";
            payout.TransactionReference = dto.TransactionReference;
            payout.Notes = dto.Notes;
            payout.PaidAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new { message = "Payout marqué comme payé" });
        }

        // ══════════════════════════════════════════════════════════════════
        // DÉCLENCHEMENT PAYOUT (à appeler quand commande = "Livré")
        // ══════════════════════════════════════════════════════════════════

        // POST /api/admin/sellers/trigger-payout/{orderId}
        [HttpPost("trigger-payout/{orderId}")]
        public async Task<IActionResult> TriggerPayout(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound(new { message = "Commande introuvable" });

            // Pour chaque item : trouver si le produit appartient à un vendeur
            foreach (var item in order.Items)
            {
                var sellerProduct = await _db.SellerProducts
                    .Include(sp => sp.Seller)
                    .FirstOrDefaultAsync(sp => sp.ProductId == item.ProductId && sp.ApprovalStatus == "Approved");

                if (sellerProduct?.Seller == null) continue;

                // Éviter les doublons
                var alreadyExists = await _db.SellerPayouts
                    .AnyAsync(p => p.OrderId == orderId && p.SellerId == sellerProduct.SellerId);
                if (alreadyExists) continue;

                var gross = item.Price * item.Quantity;
                var commission = Math.Round(gross * sellerProduct.Seller.CommissionRate, 2);
                var net = gross - commission;

                _db.SellerPayouts.Add(new SellerPayout
                {
                    SellerId = sellerProduct.SellerId,
                    OrderId = orderId,
                    GrossAmount = gross,
                    CommissionAmount = commission,
                    NetAmount = net,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "Payouts créés pour cette commande" });
        }

        // ── HELPER SLUG ───────────────────────────────────────────────────
        private static string GenerateSlug(string name)
        {
            return name.ToLower()
                .Replace(" ", "-")
                .Replace("é", "e").Replace("è", "e").Replace("ê", "e")
                .Replace("à", "a").Replace("â", "a")
                .Replace("ô", "o").Replace("ù", "u").Replace("û", "u")
                .Replace("ç", "c")
                + "-" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }
}
