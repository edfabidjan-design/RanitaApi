using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.Models;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/site-settings")]
    public class SiteSettingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SiteSettingsController(AppDbContext context)
        {
            _context = context;
        }

        // GET /api/site-settings → retourne toutes les settings sous forme {key: value}
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var settings = await _context.SiteSettings.ToListAsync();
            var dict = settings.ToDictionary(s => s.Key, s => s.Value);
            return Ok(dict);
        }

        // PUT /api/site-settings → met à jour plusieurs settings en une fois
        [HttpPut]
        public async Task<IActionResult> UpdateAll([FromBody] Dictionary<string, string> updates)
        {
            foreach (var (key, value) in updates)
            {
                var existing = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == key);
                if (existing != null)
                    existing.Value = value;
                else
                    _context.SiteSettings.Add(new SiteSetting { Key = key, Value = value });
            }
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
    }
}