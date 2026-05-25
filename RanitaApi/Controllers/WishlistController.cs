using Microsoft.AspNetCore.Mvc;
using RanitaApi.Data;
using RanitaApi.Models;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/wishlist")]
public class WishlistController : ControllerBase
{
    private readonly AppDbContext _context;
    public WishlistController(AppDbContext context) { _context = context; }

    [HttpGet("{clientId}")]
    public async Task<IActionResult> Get(int clientId)
    {
        var items = await _context.Wishlists
            .Where(w => w.ClientId == clientId)
            .Include(w => w.Product)
            .Select(w => new {
                w.Id,
                w.ProductId,
                w.Product.Name,
                w.Product.Price,
                w.Product.ImageUrl
            })
            .ToListAsync();
        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] WishlistDto dto)
    {
        var exists = await _context.Wishlists
            .AnyAsync(w => w.ClientId == dto.ClientId && w.ProductId == dto.ProductId);
        if (exists) return Ok(new { message = "already exists" });

        _context.Wishlists.Add(new Wishlist { ClientId = dto.ClientId, ProductId = dto.ProductId });
        await _context.SaveChangesAsync();
        return Ok(new { message = "added" });
    }

    [HttpDelete("{clientId}/{productId}")]
    public async Task<IActionResult> Remove(int clientId, int productId)
    {
        var item = await _context.Wishlists
            .FirstOrDefaultAsync(w => w.ClientId == clientId && w.ProductId == productId);
        if (item == null) return NotFound();
        _context.Wishlists.Remove(item);
        await _context.SaveChangesAsync();
        return Ok(new { message = "removed" });
    }
}

public class WishlistDto
{
    public int ClientId { get; set; }
    public int ProductId { get; set; }
}