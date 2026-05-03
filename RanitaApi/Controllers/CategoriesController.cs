using Microsoft.AspNetCore.Mvc;
using RanitaApi.Data;
using RanitaApi.Models;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_context.Categories.ToList());
        }

        [HttpPost]
        public IActionResult Post(Category category)
        {
            _context.Categories.Add(category);
            _context.SaveChanges();
            return Ok(category);
        }
    

    [HttpPut("{id}")]
        public IActionResult Put(int id, Category updated)
        {
            var category = _context.Categories.Find(id);
            if (category == null)
                return NotFound();

            category.Name = updated.Name;
            _context.SaveChanges();

            return Ok(category);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null)
                return NotFound();

            _context.Categories.Remove(category);
            _context.SaveChanges();

            return Ok("Supprimé");
        }
    }
}