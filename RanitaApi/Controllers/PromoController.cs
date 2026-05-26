using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.Models;

[ApiController]
[Route("api/promo")]
public class PromoController : ControllerBase
{
    private readonly AppDbContext _db;
    public PromoController(AppDbContext db) { _db = db; }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] ApplyPromoDto dto)
    {
        var code = await _db.PromoCodes.FirstOrDefaultAsync(p =>
            p.Code.ToUpper() == dto.Code.ToUpper() && p.IsActive);
        if (code == null) return BadRequest(new { message = "Code promo invalide" });
        if (code.ExpiresAt.HasValue && code.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { message = "Code promo expiré" });
        if (code.MaxUses.HasValue && code.UsedCount >= code.MaxUses)
            return BadRequest(new { message = "Code promo épuisé" });
        if (code.MinOrder.HasValue && dto.OrderTotal < code.MinOrder)
            return BadRequest(new { message = $"Commande minimum : {code.MinOrder:N0} FCFA" });
        var discount = code.Type == "percent"
            ? Math.Round(dto.OrderTotal * code.Value / 100, 0)
            : Math.Min(code.Value, dto.OrderTotal);
        return Ok(new { discount, type = code.Type, value = code.Value, codeId = code.Id });
    }


    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _db.PromoCodes.OrderByDescending(p => p.Id).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PromoCode dto)
    {
        dto.UsedCount = 0;
        _db.PromoCodes.Add(dto); await _db.SaveChangesAsync(); return Ok(dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PromoCode dto)
    {
        var p = await _db.PromoCodes.FindAsync(id); if (p == null) return NotFound();
        p.Code = dto.Code; p.Type = dto.Type; p.Value = dto.Value;
        p.MinOrder = dto.MinOrder; p.MaxUses = dto.MaxUses;
        p.ExpiresAt = dto.ExpiresAt; p.IsActive = dto.IsActive;
        await _db.SaveChangesAsync(); return Ok(p);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.PromoCodes.FindAsync(id); if (p == null) return NotFound();
        _db.PromoCodes.Remove(p); await _db.SaveChangesAsync(); return Ok();
    }
}
public class ApplyPromoDto
{
    public string Code { get; set; } = "";
    public decimal OrderTotal { get; set; }
}