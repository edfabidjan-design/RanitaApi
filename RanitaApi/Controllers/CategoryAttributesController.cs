using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.Models;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/category-attributes")]
    public class CategoryAttributesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoryAttributesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{categoryId}")]
        public async Task<IActionResult> GetByCategoryId(int categoryId)
        {
            var attrs = await _context.CategoryAttributes
                .Where(a => a.CategoryId == categoryId)
                .ToListAsync();
            return Ok(attrs);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CategoryAttribute attr)
        {
            _context.CategoryAttributes.Add(attr);
            await _context.SaveChangesAsync();
            return Ok(attr);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, CategoryAttribute updated)
        {
            var attr = await _context.CategoryAttributes.FindAsync(id);
            if (attr == null) return NotFound();

            attr.AttributeName = updated.AttributeName;
            attr.AttributeType = updated.AttributeType;
            attr.AttributeOptions = updated.AttributeOptions;
            await _context.SaveChangesAsync();
            return Ok(attr);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var attr = await _context.CategoryAttributes.FindAsync(id);
            if (attr == null) return NotFound();

            _context.CategoryAttributes.Remove(attr);
            await _context.SaveChangesAsync();
            return Ok("Supprimé");
        }
    }
}