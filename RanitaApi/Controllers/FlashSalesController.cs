using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.Models;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/flash-sales")]
    public class FlashSalesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public FlashSalesController(AppDbContext context) => _context = context;

        // GET actives pour le frontend
        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var now = DateTime.UtcNow;
            var sales = await _context.FlashSales
                .Include(f => f.Product)
                .Where(f => f.IsActive && f.StartDate <= now && f.EndDate >= now
                         && f.FlashStockSold < f.FlashStock)
                .OrderBy(f => f.EndDate)
                .Select(f => new
                {
                    f.Id,
                    f.FlashPrice,
                    f.OriginalPrice,
                    f.FlashStock,
                    f.FlashStockSold,
                    f.StartDate,
                    f.EndDate,
                    f.VariantId,
                    StockLeft = f.FlashStock - f.FlashStockSold,
                    Discount = (int)Math.Round((1 - f.FlashPrice / f.OriginalPrice) * 100),
                    Product = new
                    {
                        f.Product.Id,
                        f.Product.Name,
                        f.Product.ImageUrl,
                        f.Product.Images
                    }
                })
                .ToListAsync();
            return Ok(sales);
        }

        // GET toutes (admin)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var sales = await _context.FlashSales
                .Include(f => f.Product)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new
                {
                    f.Id,
                    f.FlashPrice,
                    f.OriginalPrice,
                    f.FlashStock,
                    f.FlashStockSold,
                    f.StartDate,
                    f.EndDate,
                    f.IsActive,
                    f.CreatedAt,
                    StockLeft = f.FlashStock - f.FlashStockSold,
                    Discount = (int)Math.Round((1 - f.FlashPrice / f.OriginalPrice) * 100),
                    Product = new
                    {
                        f.Product.Id,
                        f.Product.Name,
                        f.Product.ImageUrl,
                        f.Product.Images,
                        f.Product.Stock  // ← stock normal visible
                    }
                })
                .ToListAsync();
            return Ok(sales);
        }

        // POST créer
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FlashSaleDto dto)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == dto.ProductId);
            if (product == null) return NotFound("Produit introuvable");

            if (dto.FlashStock <= 0)
                return BadRequest("Le stock flash doit être supérieur à 0.");

            var hasVariants = product.Variants != null && product.Variants.Any();

            // ✅ Produit avec variante — variante obligatoire
            if (hasVariants && !dto.VariantId.HasValue)
                return BadRequest("Ce produit a des variantes. Veuillez sélectionner une variante.");

            ProductVariant? variant = null;
            if (dto.VariantId.HasValue)
            {
                variant = await _context.ProductVariants.FindAsync(dto.VariantId.Value);
                if (variant == null) return NotFound("Variante introuvable");

                if (dto.FlashStock > variant.Stock)
                    return BadRequest($"Stock variante insuffisant. Disponible : {variant.Stock}");

                variant.Stock -= dto.FlashStock;
            }
            else
            {
                if (dto.FlashStock > product.Stock)
                    return BadRequest($"Stock insuffisant. Disponible : {product.Stock}");
                product.Stock -= dto.FlashStock;
            }

            var flash = new FlashSale
            {
                ProductId = dto.ProductId,
                VariantId = dto.VariantId,
                FlashPrice = dto.FlashPrice,
                OriginalPrice = product.Price,
                FlashStock = dto.FlashStock,
                FlashStockSold = 0,
                StartDate = dto.StartDate.ToUniversalTime(),
                EndDate = dto.EndDate.ToUniversalTime(),
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.FlashSales.Add(flash);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                flash.Id,
                flash.FlashPrice,
                flash.FlashStock,
                flash.VariantId,
                Message = "Flash créé avec succès"
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] FlashSaleDto dto)
        {
            var flash = await _context.FlashSales.FindAsync(id);
            if (flash == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == flash.ProductId);
            if (product == null) return NotFound("Produit introuvable");

            // ✅ Restituer l'ancien stock
            if (flash.VariantId.HasValue)
            {
                var oldVariant = await _context.ProductVariants.FindAsync(flash.VariantId.Value);
                if (oldVariant != null)
                    oldVariant.Stock += flash.FlashStock - flash.FlashStockSold;
            }
            else
            {
                product.Stock += flash.FlashStock - flash.FlashStockSold;
            }

            if (dto.FlashStock <= 0)
                return BadRequest("Le stock flash doit être supérieur à 0.");

            // ✅ Déduire nouveau stock
            if (dto.VariantId.HasValue)
            {
                var newVariant = await _context.ProductVariants.FindAsync(dto.VariantId.Value);
                if (newVariant == null) return NotFound("Variante introuvable");
                if (dto.FlashStock > newVariant.Stock)
                    return BadRequest($"Stock variante insuffisant. Disponible : {newVariant.Stock}");
                newVariant.Stock -= dto.FlashStock;
            }
            else
            {
                if (dto.FlashStock > product.Stock)
                    return BadRequest($"Stock insuffisant. Disponible : {product.Stock}");
                product.Stock -= dto.FlashStock;
            }

            flash.VariantId = dto.VariantId;
            flash.FlashPrice = dto.FlashPrice;
            flash.FlashStock = dto.FlashStock;
            flash.StartDate = dto.StartDate.ToUniversalTime();
            flash.EndDate = dto.EndDate.ToUniversalTime();
            flash.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return Ok(new { flash.Id, flash.FlashPrice, flash.FlashStock, flash.VariantId });
        }

        // DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var flash = await _context.FlashSales.FindAsync(id);
            if (flash == null) return NotFound();

            var stockNonVendu = flash.FlashStock - flash.FlashStockSold;

            if (flash.VariantId.HasValue)
            {
                var variant = await _context.ProductVariants.FindAsync(flash.VariantId.Value);
                if (variant != null) variant.Stock += stockNonVendu;
            }
            else
            {
                var product = await _context.Products.FindAsync(flash.ProductId);
                if (product != null) product.Stock += stockNonVendu;
            }

            _context.FlashSales.Remove(flash);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Flash supprimé", StockRestitue = stockNonVendu });
        }

        public class FlashSaleDto
        {
            public int ProductId { get; set; }
            public int? VariantId { get; set; }
            public decimal FlashPrice { get; set; }
            public int FlashStock { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public bool IsActive { get; set; } = true;
        }
    }
}