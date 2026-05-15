using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.Models;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/admins")]
    public class AdminsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminsController(AppDbContext db)
        {
            _db = db;
        }

        // ── GET /api/admins ───────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var admins = await _db.Users
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role,
                    u.IsActive,
                    u.CreatedAt,
                    u.CreatedBy
                })
                .ToListAsync();
            return Ok(admins);
        }

        // ── POST /api/admins ──────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAdminDto dto)
        {
            if (string.IsNullOrEmpty(dto.Username) || string.IsNullOrEmpty(dto.Password))
                return BadRequest(new { message = "Username et mot de passe obligatoires" });

            var exists = await _db.Users.AnyAsync(u => u.Username == dto.Username);
            if (exists)
                return BadRequest(new { message = "Ce username existe déjà" });

            // Valider le rôle — accepte prédéfini OU JSON
            if (!IsValidRole(dto.Role))
                return BadRequest(new { message = "Rôle invalide" });

            var admin = new User
            {
                Username = dto.Username.Trim(),
                Email = dto.Email?.Trim() ?? "",
                Password = dto.Password,
                Role = dto.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = dto.CreatedBy
            };

            _db.Users.Add(admin);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Admin créé ✓", id = admin.Id, username = admin.Username, role = admin.Role });
        }

        // ── PUT /api/admins/{id}/role ─────────────────────────────────
        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleDto dto)
        {
            var admin = await _db.Users.FindAsync(id);
            if (admin == null) return NotFound(new { message = "Admin introuvable" });

            if (!IsValidRole(dto.Role))
                return BadRequest(new { message = "Rôle invalide" });

            admin.Role = dto.Role;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Permissions mises à jour ✓", role = admin.Role });
        }

        // ── PUT /api/admins/{id}/toggle ───────────────────────────────
        [HttpPut("{id}/toggle")]
        public async Task<IActionResult> Toggle(int id)
        {
            var admin = await _db.Users.FindAsync(id);
            if (admin == null) return NotFound(new { message = "Admin introuvable" });

            admin.IsActive = !admin.IsActive;
            await _db.SaveChangesAsync();
            return Ok(new { message = admin.IsActive ? "Admin activé ✓" : "Admin suspendu ✓", isActive = admin.IsActive });
        }

        // ── PUT /api/admins/{id}/password ─────────────────────────────
        [HttpPut("{id}/password")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto dto)
        {
            var admin = await _db.Users.FindAsync(id);
            if (admin == null) return NotFound(new { message = "Admin introuvable" });
            if (string.IsNullOrEmpty(dto.NewPassword) || dto.NewPassword.Length < 6)
                return BadRequest(new { message = "Mot de passe trop court (min 6 caractères)" });

            admin.Password = dto.NewPassword;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Mot de passe mis à jour ✓" });
        }

        // ── DELETE /api/admins/{id} ───────────────────────────────────
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var admin = await _db.Users.FindAsync(id);
            if (admin == null) return NotFound(new { message = "Admin introuvable" });

            if (admin.Role == "SuperAdmin")
            {
                var superAdminCount = await _db.Users.CountAsync(u => u.Role == "SuperAdmin" && u.IsActive);
                if (superAdminCount <= 1)
                    return BadRequest(new { message = "Impossible de supprimer le dernier Super Admin" });
            }

            _db.Users.Remove(admin);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Admin supprimé ✓" });
        }

        // ── VALIDATION RÔLE ───────────────────────────────────────────
        // Accepte : rôle prédéfini OU JSON valide avec "pages"
        private static bool IsValidRole(string? role)
        {
            if (string.IsNullOrEmpty(role)) return false;

            // Rôles prédéfinis acceptés
            var presets = new[] { "SuperAdmin", "Analyste" };
            if (presets.Contains(role)) return true;

            // JSON de permissions custom
            if (role.TrimStart().StartsWith("{"))
            {
                try
                {
                    var doc = System.Text.Json.JsonDocument.Parse(role);
                    // Doit contenir au moins "pages"
                    return doc.RootElement.TryGetProperty("pages", out _);
                }
                catch { return false; }
            }

            return false;
        }
    }

    // ── DTOs ─────────────────────────────────────────────────────────
    public class CreateAdminDto
    {
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Role { get; set; } = "Analyste";
        public string? CreatedBy { get; set; }
    }

    public class UpdateRoleDto
    {
        public string Role { get; set; } = "";
    }

    public class ChangePasswordDto
    {
        public string NewPassword { get; set; } = "";
    }
}