using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RanitaApi.Data;
using RanitaApi.Models;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;

        private const string CACHE_TREE = "categories:tree";
        private const string CACHE_ALL = "categories:all";
        // 30 minutes — les catégories changent très rarement
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(30);

        public CategoriesController(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // ── GET /api/categories — arbre parent/enfants ✅ ─────────────
        [HttpGet]
        public IActionResult Get()
        {
            if (!_cache.TryGetValue(CACHE_TREE, out List<Category>? cached) || cached == null)
            {
                cached = _context.Categories
                    .Include(c => c.Children)
                    .Where(c => c.ParentId == null)
                    .AsNoTracking()
                    .ToList();

                _cache.Set(CACHE_TREE, cached, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CACHE_DURATION,
                    Size = 1
                });
            }

            return Ok(cached);
        }

        // ── GET /api/categories/all — liste plate ✅ ──────────────────
        [HttpGet("all")]
        public IActionResult GetAll()
        {
            if (!_cache.TryGetValue(CACHE_ALL, out List<Category>? cached) || cached == null)
            {
                cached = _context.Categories.AsNoTracking().ToList();

                _cache.Set(CACHE_ALL, cached, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CACHE_DURATION,
                    Size = 1
                });
            }

            return Ok(cached);
        }

        // ── POST — invalide le cache ✅ ───────────────────────────────
        [HttpPost]
        public IActionResult Post(Category category)
        {
            _context.Categories.Add(category);
            _context.SaveChanges();
            InvalidateCache();
            return Ok(category);
        }

        // ── PUT — invalide le cache ✅ ────────────────────────────────
        [HttpPut("{id}")]
        public IActionResult Put(int id, Category updated)
        {
            var category = _context.Categories.Find(id);
            if (category == null) return NotFound();

            category.Name = updated.Name;
            category.ParentId = updated.ParentId;
            _context.SaveChanges();
            InvalidateCache();

            return Ok(category);
        }

        // ── DELETE — invalide le cache ✅ ─────────────────────────────
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null) return NotFound();

            _context.Categories.Remove(category);
            _context.SaveChanges();
            InvalidateCache();

            return Ok("Supprimé");
        }

        private void InvalidateCache()
        {
            _cache.Remove(CACHE_TREE);
            _cache.Remove(CACHE_ALL);
        }
    }
}