using Microsoft.AspNetCore.Mvc;
using RanitaApi.Data;
using Microsoft.EntityFrameworkCore;

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
}
public class ApplyPromoDto
{
    public string Code { get; set; } = "";
    public decimal OrderTotal { get; set; }
}