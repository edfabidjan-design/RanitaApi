using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.Models;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/flash-requests")]
    public class FlashSaleRequestsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public FlashSaleRequestsController(AppDbContext context) => _context = context;

        // GET toutes (admin)
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status)
        {
            var query = _context.FlashSaleRequests
                .Include(f => f.Seller)
                .Include(f => f.Product)
                .Include(f => f.Variant)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(f => f.Status == status);

            var result = await query
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new {
                    f.Id,
                    f.FlashPrice,
                    f.OriginalPrice,
                    f.FlashStock,
                    f.StartDate,
                    f.EndDate,
                    f.Status,
                    f.RejectionReason,
                    f.CreatedAt,
                    Discount = (int)Math.Round((1 - f.FlashPrice / f.OriginalPrice) * 100),
                    Seller = new { f.Seller.Id, f.Seller.ShopName },
                    Product = new { f.Product.Id, f.Product.Name, f.Product.ImageUrl },
                    Variant = f.Variant == null ? null : new { f.Variant.Id, f.Variant.Combination, f.Variant.Stock },
                    OriginalVariantStock = f.OriginalVariantStock
                })
                .ToListAsync();

            return Ok(result);
        }

        // GET par vendeur
        [HttpGet("seller/{sellerId}")]
        public async Task<IActionResult> GetBySeller(int sellerId)
        {
            var result = await _context.FlashSaleRequests
                .Include(f => f.Product)
                .Include(f => f.Variant)
                .Where(f => f.SellerId == sellerId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new {
                    f.Id,
                    f.FlashPrice,
                    f.OriginalPrice,
                    f.FlashStock,
                    f.StartDate,
                    f.EndDate,
                    f.Status,
                    f.RejectionReason,
                    f.CreatedAt,
                    Discount = (int)Math.Round((1 - f.FlashPrice / f.OriginalPrice) * 100),
                    Product = new { f.Product.Id, f.Product.Name, f.Product.ImageUrl },
                    Variant = f.Variant == null ? null : new { f.Variant.Id, f.Variant.Combination }
                })
                .ToListAsync();

            return Ok(result);
        }

        // POST créer demande (vendeur)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FlashSaleRequestDto dto)
        {
            var seller = await _context.Sellers.FindAsync(dto.SellerId);
            if (seller == null) return NotFound("Vendeur introuvable");

            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId);
            if (product == null) return NotFound("Produit introuvable");

            // Vérifier que le produit appartient au vendeur
            var sellerProduct = await _context.SellerProducts
                .FirstOrDefaultAsync(sp => sp.ProductId == dto.ProductId
                    && sp.SellerId == dto.SellerId
                    && sp.ApprovalStatus == "Approved");
            if (sellerProduct == null)
                return BadRequest("Ce produit ne vous appartient pas ou n'est pas approuvé.");

            // Vérifier règles admin
            var maxDuration = await _context.SiteSettings
                .FirstOrDefaultAsync(s => s.Key == "flash_max_duration_hours");
            var minDiscount = await _context.SiteSettings
                .FirstOrDefaultAsync(s => s.Key == "flash_min_discount_pct");

            var maxHours = int.TryParse(maxDuration?.Value, out var mh) ? mh : 48;
            var minPct = int.TryParse(minDiscount?.Value, out var mp) ? mp : 10;

            // Vérifier durée
            var duration = (dto.EndDate - dto.StartDate).TotalHours;
            if (duration > maxHours)
                return BadRequest($"Durée maximale autorisée : {maxHours}h. Votre flash dure {Math.Round(duration)}h.");

            // Vérifier remise minimale
            var discount = (1 - dto.FlashPrice / product.Price) * 100;
            if (discount < minPct)
                return BadRequest($"Remise minimale requise : {minPct}%. Votre remise est de {Math.Round(discount)}%.");

            // Vérifier stock variante
            if (dto.VariantId.HasValue)
            {
                var variant = await _context.ProductVariants.FindAsync(dto.VariantId.Value);
                if (variant == null) return NotFound("Variante introuvable");
                if (dto.FlashStock > variant.Stock)
                    return BadRequest($"Stock variante insuffisant. Disponible : {variant.Stock}");
            }
            else
            {
                var hasVariants = product.Variants != null && product.Variants.Any();
                if (hasVariants)
                    return BadRequest("Ce produit a des variantes. Veuillez sélectionner une variante.");
                if (dto.FlashStock > product.Stock)
                    return BadRequest($"Stock insuffisant. Disponible : {product.Stock}");
            }

            var request = new FlashSaleRequest
            {
                SellerId = dto.SellerId,
                ProductId = dto.ProductId,
                VariantId = dto.VariantId,
                FlashPrice = dto.FlashPrice,
                OriginalPrice = product.Price,
                FlashStock = dto.FlashStock,
                StartDate = dto.StartDate.ToUniversalTime(),
                EndDate = dto.EndDate.ToUniversalTime(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow

                OriginalVariantStock = dto.VariantId.HasValue
    ? (await _context.ProductVariants.FindAsync(dto.VariantId.Value))?.Stock ?? 0
    : product.Stock,
            };

            _context.FlashSaleRequests.Add(request);
            await _context.SaveChangesAsync();

            return Ok(new { request.Id, request.Status, Message = "Demande envoyée — en attente de validation admin." });
        }

        // PUT approuver (admin)
        // Dans Approve(), remplace les dates fixes par les dates de la période :
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(int id)
        {
            var request = await _context.FlashSaleRequests
                .Include(f => f.Product).ThenInclude(p => p.Variants)
                .FirstOrDefaultAsync(f => f.Id == id);
            if (request == null) return NotFound();
            if (request.Status != "Pending") return BadRequest("Demande déjà traitée.");

            // ✅ Lire la période flash depuis les settings
            var periodStart = await _context.SiteSettings
                .FirstOrDefaultAsync(s => s.Key == "flash_period_start");
            var periodEnd = await _context.SiteSettings
                .FirstOrDefaultAsync(s => s.Key == "flash_period_end");

            DateTime startDate, endDate;

            if (!string.IsNullOrEmpty(periodStart?.Value) &&
                !string.IsNullOrEmpty(periodEnd?.Value) &&
                DateTime.TryParse(periodStart.Value, out var ps) &&
                DateTime.TryParse(periodEnd.Value, out var pe))
            {
                startDate = DateTime.SpecifyKind(ps, DateTimeKind.Utc);
                endDate = DateTime.SpecifyKind(pe, DateTimeKind.Utc);
            }
            else
            {
                // ✅ Fallback : dates de la demande vendeur
                startDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc);
                endDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc);
            }

            // Déduire stock
            if (request.VariantId.HasValue)
            {
                var variant = await _context.ProductVariants.FindAsync(request.VariantId.Value);
                if (variant == null) return NotFound("Variante introuvable");
                if (request.FlashStock > variant.Stock)
                    return BadRequest($"Stock variante insuffisant. Disponible : {variant.Stock}");
                variant.Stock -= request.FlashStock;
            }
            else
            {
                if (request.FlashStock > request.Product.Stock)
                    return BadRequest($"Stock insuffisant. Disponible : {request.Product.Stock}");
                request.Product.Stock -= request.FlashStock;
            }

            var flash = new FlashSale
            {
                ProductId = request.ProductId,
                VariantId = request.VariantId,
                FlashPrice = request.FlashPrice,
                OriginalPrice = request.OriginalPrice,
                FlashStock = request.FlashStock,
                FlashStockSold = 0,
                StartDate = startDate,
                EndDate = endDate,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.FlashSales.Add(flash);
            request.Status = "Approved";
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Flash approuvé et publié.", FlashId = flash.Id });
        }

        // PUT rejeter (admin)
        [HttpPut("{id}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectDto dto)
        {
            var request = await _context.FlashSaleRequests.FindAsync(id);
            if (request == null) return NotFound();
            if (request.Status != "Pending") return BadRequest("Demande déjà traitée.");

            request.Status = "Rejected";
            request.RejectionReason = dto.Reason;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Demande rejetée." });
        }

        // DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var request = await _context.FlashSaleRequests.FindAsync(id);
            if (request == null) return NotFound();
            if (request.Status == "Approved")
                return BadRequest("Impossible de supprimer une demande approuvée.");
            _context.FlashSaleRequests.Remove(request);
            await _context.SaveChangesAsync();
            return Ok(new { Message = "Demande supprimée." });
        }

        // PUT modifier (vendeur)
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] FlashSaleRequestDto dto)
        {
            var request = await _context.FlashSaleRequests.FindAsync(id);
            if (request == null) return NotFound();
            if (request.Status == "Approved")
                return BadRequest("Impossible de modifier une demande approuvée.");

            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null) return NotFound("Produit introuvable");

            // Revérifier les règles
            var minDiscount = await _context.SiteSettings
                .FirstOrDefaultAsync(s => s.Key == "flash_min_discount_pct");
            var minPct = int.TryParse(minDiscount?.Value, out var mp) ? mp : 10;
            var discount = (1 - dto.FlashPrice / product.Price) * 100;
            if (discount < minPct)
                return BadRequest($"Remise minimale requise : {minPct}%.");

            request.FlashPrice = dto.FlashPrice;
            request.FlashStock = dto.FlashStock;
            request.VariantId = dto.VariantId;
            request.StartDate = dto.StartDate.ToUniversalTime();
            request.EndDate = dto.EndDate.ToUniversalTime();
            request.Status = "Pending"; // Remet en attente
            request.RejectionReason = null;

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Demande modifiée — en attente de validation." });
        }

        public class FlashSaleRequestDto
        {
            public int SellerId { get; set; }
            public int ProductId { get; set; }
            public int? VariantId { get; set; }
            public decimal FlashPrice { get; set; }
            public int FlashStock { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }

        public class RejectDto
        {
            public string Reason { get; set; } = "";
        }
    }
}