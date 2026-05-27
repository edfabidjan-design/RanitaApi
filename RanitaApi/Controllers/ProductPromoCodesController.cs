using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.Models;

[ApiController]
[Route("api/product-promo-codes")]
public class ProductPromoCodesController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProductPromoCodesController(AppDbContext db) { _db = db; }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var now = DateTime.UtcNow;
        var code = await _db.ProductPromoCodes
            .Include(p => p.Product)
            .Where(p => p.IsActive
                && (p.StartDate == null || p.StartDate <= now)
                && (p.EndDate == null || p.EndDate >= now))
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();
        if (code == null) return Ok(null);
        return Ok(new
        {
            code.Id,
            code.Code,
            code.Discount,
            code.EndDate,
            code.ProductId,
            ProductName = code.Product.Name,
            ProductImage = code.Product.ImageUrl,
            ProductPrice = code.Product.Price
        });
    }

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ValidatePromoDto dto)
    {
        var now = DateTime.UtcNow;
        var code = await _db.ProductPromoCodes
            .Include(p => p.Product)
            .FirstOrDefaultAsync(p =>
                p.Code.ToUpper() == dto.Code.ToUpper()
                && p.IsActive
                && (p.StartDate == null || p.StartDate <= now)
                && (p.EndDate == null || p.EndDate >= now));
        if (code == null) return Ok(new { valid = false, message = "Code invalide ou expiré" });
        bool inCart = dto.CartItems.Any(i => i.ProductId == code.ProductId);
        if (!inCart) return Ok(new { valid = false, message = "Ce code est valable uniquement pour " + code.Product.Name });
        decimal discountAmount = dto.CartItems
            .Where(i => i.ProductId == code.ProductId)
            .Sum(i => i.Price * i.Quantity * code.Discount / 100m);
        return Ok(new { valid = true, discount = code.Discount, discountAmount, productId = code.ProductId, productName = code.Product.Name });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductPromoCode dto)
    {
        try
        {
            var code = new ProductPromoCode
            {
                Code = dto.Code.ToUpper(),
                ProductId = dto.ProductId,
                Discount = dto.Discount,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            _db.ProductPromoCodes.Add(code);
            await _db.SaveChangesAsync();
            return Ok(new { success = true, id = code.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var codes = await _db.ProductPromoCodes
                .Include(p => p.Product)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
            return Ok(codes.Select(c => new {
                c.Id,
                c.Code,
                c.Discount,
                c.StartDate,
                c.EndDate,
                c.IsActive,
                c.ProductId,
                ProductName = c.Product != null ? c.Product.Name : "—"
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductPromoCode dto)
    {
        try
        {
            var code = await _db.ProductPromoCodes.FindAsync(id);
            if (code == null) return NotFound();
            code.Code = dto.Code.ToUpper();
            code.ProductId = dto.ProductId;
            code.Discount = dto.Discount;
            code.StartDate = dto.StartDate;
            code.EndDate = dto.EndDate;
            code.IsActive = dto.IsActive;
            await _db.SaveChangesAsync();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.ProductPromoCodes.FindAsync(id);
        if (c == null) return NotFound();
        _db.ProductPromoCodes.Remove(c);
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpPatch("{id}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var c = await _db.ProductPromoCodes.FindAsync(id);
        if (c == null) return NotFound();
        c.IsActive = !c.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}

public class ValidatePromoDto
{
    public string Code { get; set; } = "";
    public List<CartItemDto> CartItems { get; set; } = new();
}
public class CartItemDto
{
    public int ProductId { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}