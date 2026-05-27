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
        try
        {
            var now = DateTime.UtcNow;
            var result = await _db.Database.SqlQueryRaw<ProductPromoCodeDto>(@"
// GetActive() — remplace le SELECT par :
SELECT p.""Id"", p.""Code"", p.""Discount"", p.""EndDate"", p.""ProductId"",
       pr.""Name"" as ""ProductName"", pr.""ImageUrl"" as ""ProductImage"", 
       pr.""Price"" as ""ProductPrice"", p.""Color""

                FROM ""ProductPromoCodes"" p
                JOIN ""Products"" pr ON pr.""Id"" = p.""ProductId""
                WHERE p.""IsActive"" = TRUE
                  AND (p.""StartDate"" IS NULL OR p.""StartDate"" <= {0})
                  AND (p.""EndDate"" IS NULL OR p.""EndDate"" >= {0})
                ORDER BY p.""Id"" DESC
                LIMIT 1
            ", now).ToListAsync();
            return Ok(result.FirstOrDefault());
        }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _db.Database.SqlQueryRaw<ProductPromoCodeListDto>(@"SELECT p.""Id"", p.""Code"", p.""Discount"", p.""StartDate"", p.""EndDate"", p.""IsActive"", p.""ProductId"", pr.""Name"" as ""ProductName"", p.""Color"" FROM ""ProductPromoCodes"" p JOIN ""Products"" pr ON pr.""Id"" = p.""ProductId"" ORDER BY p.""Id"" DESC").ToListAsync();
            return Ok(result);
        }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductPromoCodeInput dto)
    {
        try
        {
            var startDate = dto.StartDate.HasValue ? DateTime.SpecifyKind(dto.StartDate.Value, DateTimeKind.Utc) : (DateTime?)null;
            var endDate = dto.EndDate.HasValue ? DateTime.SpecifyKind(dto.EndDate.Value, DateTimeKind.Utc) : (DateTime?)null;
            await _db.Database.ExecuteSqlRawAsync(@"
    INSERT INTO ""ProductPromoCodes"" (""Code"", ""ProductId"", ""Discount"", ""StartDate"", ""EndDate"", ""IsActive"", ""Color"", ""CreatedAt"")
    VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, NOW())
", dto.Code.ToUpper(), dto.ProductId, dto.Discount, (object?)startDate ?? DBNull.Value, (object?)endDate ?? DBNull.Value, dto.IsActive, (object?)dto.Color ?? DBNull.Value);
            return Ok(new { success = true });
        }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProductPromoCodeInput dto)
    {
        try
        {
            var startDate = dto.StartDate.HasValue ? DateTime.SpecifyKind(dto.StartDate.Value, DateTimeKind.Utc) : (DateTime?)null;
            var endDate = dto.EndDate.HasValue ? DateTime.SpecifyKind(dto.EndDate.Value, DateTimeKind.Utc) : (DateTime?)null;
            await _db.Database.ExecuteSqlRawAsync(@"UPDATE ""ProductPromoCodes"" SET ""Code""={0}, ""ProductId""={1}, ""Discount""={2}, ""StartDate""={3}, ""EndDate""={4}, ""IsActive""={5}, ""Color""={6} WHERE ""Id""={7}",
                dto.Code.ToUpper(), dto.ProductId, dto.Discount, (object?)startDate ?? DBNull.Value, (object?)endDate ?? DBNull.Value, dto.IsActive, (object?)dto.Color ?? DBNull.Value, id);
            return Ok(new { success = true });
        }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _db.Database.ExecuteSqlRawAsync(@"DELETE FROM ""ProductPromoCodes"" WHERE ""Id""={0}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPatch("{id}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        try
        {
            await _db.Database.ExecuteSqlRawAsync(@"UPDATE ""ProductPromoCodes"" SET ""IsActive"" = NOT ""IsActive"" WHERE ""Id""={0}", id);
            return Ok(new { success = true });
        }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ValidatePromoDto dto)
    {
        try
        {
            var now = DateTime.UtcNow;
            var result = await _db.Database.SqlQueryRaw<ProductPromoCodeDto>(@"
                SELECT p.""Id"", p.""Code"", p.""Discount"", p.""EndDate"", p.""ProductId"",
                       pr.""Name"" as ""ProductName"", pr.""ImageUrl"" as ""ProductImage"", pr.""Price"" as ""ProductPrice""
                FROM ""ProductPromoCodes"" p
                JOIN ""Products"" pr ON pr.""Id"" = p.""ProductId""
                WHERE UPPER(p.""Code"") = UPPER({0}) AND p.""IsActive"" = TRUE
                  AND (p.""StartDate"" IS NULL OR p.""StartDate"" <= {1})
                  AND (p.""EndDate"" IS NULL OR p.""EndDate"" >= {1})
            ", dto.Code, now).ToListAsync();
            var code = result.FirstOrDefault();
            if (code == null) return Ok(new { valid = false, message = "Code invalide ou expiré" });
            bool inCart = dto.CartItems.Any(i => i.ProductId == code.ProductId);
            if (!inCart) return Ok(new { valid = false, message = "Ce code est valable uniquement pour " + code.ProductName });
            decimal discountAmount = dto.CartItems.Where(i => i.ProductId == code.ProductId).Sum(i => i.Price * i.Quantity * code.Discount / 100m);
            return Ok(new { valid = true, discount = code.Discount, discountAmount, productId = code.ProductId, productName = code.ProductName });
        }
        catch (Exception ex) { return StatusCode(500, ex.Message); }
    }
}

public class ProductPromoCodeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public int Discount { get; set; }
    public DateTime? EndDate { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string ProductImage { get; set; } = "";
    public decimal ProductPrice { get; set; }
    public string? Color { get; set; }
}
public class ProductPromoCodeListDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public int Discount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
}
public class ProductPromoCodeInput
{
    public string Code { get; set; } = "";
    public int ProductId { get; set; }
    public int Discount { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Color { get; set; }
}
public class ValidatePromoDto { public string Code { get; set; } = ""; public List<CartItemDto> CartItems { get; set; } = new(); }
public class CartItemDto { public int ProductId { get; set; } public decimal Price { get; set; } public int Quantity { get; set; } }