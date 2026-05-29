using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RanitaApi.Data;
using RanitaApi.Models;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/site-settings")]
    public class SiteSettingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;

        private const string CACHE_KEY = "site-settings:all";
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(10);

        public SiteSettingsController(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // ── GET /api/site-settings ✅ — cache 10 minutes ──────────────
        // Appelé à chaque chargement de page (index, products, checkout...)
        // Sans cache : ~400ms / requête
        // Avec cache  : ~2ms  / requête
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (!_cache.TryGetValue(CACHE_KEY, out Dictionary<string, string>? cached) || cached == null)
            {
                var settings = await _context.SiteSettings
                    .AsNoTracking()
                    .ToListAsync();

                cached = settings.ToDictionary(s => s.Key, s => s.Value);

                _cache.Set(CACHE_KEY, cached, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CACHE_DURATION,
                    SlidingExpiration = TimeSpan.FromMinutes(5),
                    Size = 1
                });
            }

            return Ok(cached);
        }

        // ── PUT /api/site-settings ✅ — invalide le cache ─────────────
        // Quand l'admin modifie un setting, le cache est vidé
        // La prochaine requête GET rechargera les nouvelles valeurs
        [HttpPut]
        public async Task<IActionResult> UpdateAll([FromBody] Dictionary<string, string> updates)
        {
            foreach (var (key, value) in updates)
            {
                var existing = await _context.SiteSettings
                    .FirstOrDefaultAsync(s => s.Key == key);

                if (existing != null)
                    existing.Value = value;
                else
                    _context.SiteSettings.Add(new SiteSetting { Key = key, Value = value });
            }

            await _context.SaveChangesAsync();

            // ✅ Invalide le cache — le prochain GET verra les nouvelles valeurs
            _cache.Remove(CACHE_KEY);

            return Ok(new { success = true });
        }
    }
}