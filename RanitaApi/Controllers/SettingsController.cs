using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.Models;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SettingsController(AppDbContext db)
        {
            _db = db;
        }

        // ── GET /api/settings/commission ─────────────────────────────────────
        // Retourne le taux global + tous les overrides par catégorie
        [HttpGet("commission")]
        public async Task<IActionResult> GetCommission()
        {
            var settings = await _db.CommissionSettings.ToListAsync();

            var global = settings.FirstOrDefault(s => s.Key == "global");
            var globalRate = global?.Rate ?? 0.10m;

            var categories = await _db.Categories
                .Where(c => c.ParentId != null) // sous-catégories uniquement
                .OrderBy(c => c.Name)
                .ToListAsync();

            var result = new
            {
                GlobalRate = globalRate,
                Categories = categories.Select(c =>
                {
                    var key = $"cat_{c.Id}";
                    var ovr = settings.FirstOrDefault(s => s.Key == key);
                    return new
                    {
                        CategoryId = c.Id,
                        CategoryName = c.Name,
                        Rate = ovr?.Rate,          // null = utilise le global
                        HasOverride = ovr != null
                    };
                })
            };

            return Ok(result);
        }

        // ── PUT /api/settings/commission ─────────────────────────────────────
        // Sauvegarde le taux global + les overrides par catégorie
        [HttpPut("commission")]
        public async Task<IActionResult> SaveCommission([FromBody] SaveCommissionDto dto)
        {
            if (dto.GlobalRate < 0 || dto.GlobalRate > 1)
                return BadRequest(new { message = "Le taux global doit être entre 0 et 1 (ex: 0.10 pour 10%)" });

            // 1. Taux global
            var global = await _db.CommissionSettings.FirstOrDefaultAsync(s => s.Key == "global");
            if (global == null)
            {
                _db.CommissionSettings.Add(new CommissionSetting
                {
                    Key = "global",
                    Label = "Global",
                    Rate = dto.GlobalRate,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                global.Rate = dto.GlobalRate;
                global.UpdatedAt = DateTime.UtcNow;
            }

            // 2. Overrides par catégorie
            if (dto.CategoryOverrides != null)
            {
                foreach (var ovr in dto.CategoryOverrides)
                {
                    var key = $"cat_{ovr.CategoryId}";
                    var existing = await _db.CommissionSettings.FirstOrDefaultAsync(s => s.Key == key);

                    if (ovr.Rate == null)
                    {
                        // Supprimer l'override → revenir au global
                        if (existing != null) _db.CommissionSettings.Remove(existing);
                    }
                    else
                    {
                        if (ovr.Rate < 0 || ovr.Rate > 1)
                            return BadRequest(new { message = $"Taux invalide pour catégorie {ovr.CategoryId}" });

                        if (existing == null)
                        {
                            _db.CommissionSettings.Add(new CommissionSetting
                            {
                                Key = key,
                                Label = ovr.CategoryName ?? key,
                                Rate = ovr.Rate.Value,
                                UpdatedAt = DateTime.UtcNow
                            });
                        }
                        else
                        {
                            existing.Rate = ovr.Rate.Value;
                            existing.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "Taux de commission enregistrés ✓" });
        }

        // ── GET /api/settings/commission/rate?categoryName=Mode ──────────────
        // Endpoint utilitaire pour calculer le taux applicable à une catégorie
        [HttpGet("commission/rate")]
        public async Task<IActionResult> GetRateForCategory([FromQuery] string? categoryName)
        {
            var settings = await _db.CommissionSettings.ToListAsync();
            var globalRate = settings.FirstOrDefault(s => s.Key == "global")?.Rate ?? 0.10m;

            if (!string.IsNullOrEmpty(categoryName))
            {
                var cat = await _db.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
                if (cat != null)
                {
                    var ovr = settings.FirstOrDefault(s => s.Key == $"cat_{cat.Id}");
                    if (ovr != null)
                        return Ok(new { rate = ovr.Rate, source = "category_override", categoryName });
                }
            }

            return Ok(new { rate = globalRate, source = "global" });
        }
    }

    // ── DTOs ─────────────────────────────────────────────────────────────────
    public class SaveCommissionDto
    {
        public decimal GlobalRate { get; set; }
        public List<CategoryOverrideDto>? CategoryOverrides { get; set; }
    }

    public class CategoryOverrideDto
    {
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public decimal? Rate { get; set; } // null = supprimer l'override
    }
}