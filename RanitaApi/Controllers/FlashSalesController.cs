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
                .Select(f => new {
                    f.Id,
                    f.FlashPrice,
                    f.OriginalPrice,
                    f.FlashStock,
                    f.FlashStockSold,
                    f.StartDate,
                    f.EndDate,
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
                .Select(f => new {
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

            // ✅ Stock réel = variantes si elles existent, sinon stock produit
            var hasVariants = product.Variants != null && product.Variants.Any();
            var stockDisponible = hasVariants
                ? product.Variants!.Sum(v => v.Stock)
                : product.Stock;

            if (dto.FlashStock > stockDisponible)
                return BadRequest($"Stock insuffisant. Stock disponible : {stockDisponible}");

            // ✅ Déduire uniquement si pas de variantes
            if (!hasVariants)
                product.Stock -= dto.FlashStock;

            var flash = new FlashSale
            {
                ProductId = dto.ProductId,
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
                flash.StartDate,
                flash.EndDate,
                StockNormalRestant = hasVariants ? stockDisponible - dto.FlashStock : product.Stock,
                Message = $"Flash créé. Stock restant : {(hasVariants ? stockDisponible - dto.FlashStock : product.Stock)}"
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

            var hasVariants = product.Variants != null && product.Variants.Any();

            // ✅ Remettre ancien stock seulement si pas de variantes
            if (!hasVariants)
            {
                var ancienStockNonVendu = flash.FlashStock - flash.FlashStockSold;
                product.Stock += ancienStockNonVendu;
            }

            if (dto.FlashStock <= 0)
                return BadRequest("Le stock flash doit être supérieur à 0.");

            var stockDisponible = hasVariants
                ? product.Variants!.Sum(v => v.Stock)
                : product.Stock;

            if (dto.FlashStock > stockDisponible)
                return BadRequest($"Stock insuffisant. Stock disponible : {stockDisponible}");

            if (!hasVariants)
                product.Stock -= dto.FlashStock;

            flash.FlashPrice = dto.FlashPrice;
            flash.FlashStock = dto.FlashStock;
            flash.StartDate = dto.StartDate.ToUniversalTime();
            flash.EndDate = dto.EndDate.ToUniversalTime();
            flash.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                flash.Id,
                flash.FlashPrice,
                flash.FlashStock,
                flash.FlashStockSold,
                StockNormalRestant = hasVariants ? stockDisponible - dto.FlashStock : product.Stock,
                Message = $"Flash mis à jour."
            });
        }

        // DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var flash = await _context.FlashSales.FindAsync(id);
            if (flash == null) return NotFound();

            var product = await _context.Products.FindAsync(flash.ProductId);
            if (product != null)
            {
                // ✅ Remettre le stock flash non vendu au stock normal
                var stockNonVendu = flash.FlashStock - flash.FlashStockSold;
                product.Stock += stockNonVendu;

                Console.WriteLine($"Flash supprimé — {stockNonVendu} unités restituées au produit #{product.Id}");
            }

            _context.FlashSales.Remove(flash);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Flash supprimé",
                StockRestitue = flash.FlashStock - flash.FlashStockSold,
                StockNormalActuel = product?.Stock
            });
        }
    }

    public class FlashSaleDto
    {
        public int ProductId { get; set; }
        public decimal FlashPrice { get; set; }
        public int FlashStock { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}