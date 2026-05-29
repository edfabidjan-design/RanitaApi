using Microsoft.AspNetCore.Mvc;
using RanitaApi.Data;
using RanitaApi.Models;
using RanitaApi.Services;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JwtService _jwt;

    public AuthController(AppDbContext context, JwtService jwt)
    {
        _context = context;
        _jwt = jwt;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] AdminLoginDto dto)
    {
        try
        {
            // 1. Trouver l'admin par username
            var user = _context.Users
                .FirstOrDefault(u => u.Username == dto.Username);

            if (user == null)
            {
                // Anti-timing : effectuer une vérification factice
                BCrypt.Net.BCrypt.Verify(dto.Password, "$2a$12$dummy.hash.for.timing.only.xxxxxxxxxxxxxxxxxx");
                return Unauthorized("Identifiants incorrects");
            }

            if (!user.IsActive)
                return Unauthorized("Compte suspendu. Contactez le Super Admin.");

            // 2. Vérifier le mot de passe
            bool isValid;

            if (user.Password.StartsWith("$2"))
            {
                // Hash BCrypt normal
                isValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);
            }
            else if (user.Password.StartsWith("LEGACY:SHA256:"))
            {
                // Migration depuis SHA-256 (anciens comptes)
                var oldHash = user.Password["LEGACY:SHA256:".Length..];
                var inputHash = Convert.ToBase64String(
                    System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(dto.Password)
                    )
                );
                isValid = oldHash == inputHash;
                if (isValid)
                {
                    user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12);
                    _context.SaveChanges();
                    Console.WriteLine($"Migration BCrypt admin OK pour user #{user.Id}");
                }
            }
            else
            {
                // Mot de passe en clair (legacy — admin "1234")
                isValid = user.Password == dto.Password;
                if (isValid)
                {
                    // Re-hacher immédiatement en BCrypt
                    user.Password = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12);
                    _context.SaveChanges();
                    Console.WriteLine($"Migration BCrypt admin (plain text) OK pour user #{user.Id}");
                }
            }

            if (!isValid)
                return Unauthorized("Identifiants incorrects");

            // 3. Générer le token JWT admin (même service que les clients)
            var token = _jwt.GenerateAdminToken(user.Id, user.Username, user.Role, user.Email);

            return Ok(new
            {
                token,
                role = user.Role,
                username = user.Username,
                email = user.Email,
                id = user.Id
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("AUTH ERROR: " + ex.Message);
            return StatusCode(500, "Erreur serveur");
        }
    }
}

public class AdminLoginDto
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}