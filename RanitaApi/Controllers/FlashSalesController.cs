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
                        f.Product.Images
                    }
                })
                .ToListAsync();
            return Ok(sales);
        }

        // POST créer
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FlashSaleDto dto)
        {
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null) return NotFound("Produit introuvable");

            var flash = new FlashSale
            {
                ProductId = dto.ProductId,
                FlashPrice = dto.FlashPrice,
                OriginalPrice = product.Price,
                FlashStock = dto.FlashStock,
                StartDate = dto.StartDate.ToUniversalTime(),
                EndDate = dto.EndDate.ToUniversalTime(),
                IsActive = dto.IsActive
            };
            _context.FlashSales.Add(flash);
            await _context.SaveChangesAsync();
            return Ok(flash);
        }

        // PUT modifier
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] FlashSaleDto dto)
        {
            var flash = await _context.FlashSales.FindAsync(id);
            if (flash == null) return NotFound();
            flash.FlashPrice = dto.FlashPrice;
            flash.FlashStock = dto.FlashStock;
            flash.StartDate = dto.StartDate.ToUniversalTime();
            flash.EndDate = dto.EndDate.ToUniversalTime();
            flash.IsActive = dto.IsActive;
            await _context.SaveChangesAsync();
            return Ok(flash);
        }

        // DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var flash = await _context.FlashSales.FindAsync(id);
            if (flash == null) return NotFound();
            _context.FlashSales.Remove(flash);
            await _context.SaveChangesAsync();
            return Ok();
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