using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using RanitaApi.Data;
using RanitaApi.DTO;
using RanitaApi.Models;
using RanitaApi.Services;
using System.Security.Cryptography;

[ApiController]
[Route("api/client-auth")]
public class ClientAuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly EmailService _emailService;

    // Hash BCrypt utilisé pour le timing-attack fix (email inexistant)
    // Généré une seule fois au démarrage, jamais exposé
    private static readonly string _dummyHash =
        BCrypt.Net.BCrypt.HashPassword("dummy-timing-protection", workFactor: 12);

    public ClientAuthController(AppDbContext context, EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    // ── Hachage BCrypt (workFactor 12 = ~250ms, bon équilibre sécurité/perf) ──
    string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    bool VerifyPassword(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);

    // ── OTP cryptographiquement sûr (remplace System.Random) ──────────────
    string GenerateCode()
    {
        var bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);
        var value = BitConverter.ToUInt32(bytes) % 900000 + 100000;
        return value.ToString();
    }

    // ── Code parrainage unique ex: KALIM4821 ───────────────────────────────
    string GenerateReferralCode(string fullName)
    {
        var first = fullName.Split(' ')[0].ToUpper();
        if (first.Length > 6) first = first[..6];
        var bytes = new byte[2];
        RandomNumberGenerator.Fill(bytes);
        var suffix = (BitConverter.ToUInt16(bytes) % 9000 + 1000).ToString();
        return first + suffix;
    }

    // ══════════════════════════════════════════════════════════════════════
    // REGISTER
    // ══════════════════════════════════════════════════════════════════════
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register(ClientRegisterDto dto)
    {
        if (await _context.Clients.AnyAsync(x => x.Email == dto.Email))
            return BadRequest("Email déjà utilisé");

        var client = new Client
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone,
            PasswordHash = HashPassword(dto.Password),   // ✅ BCrypt
            ReferralCode = GenerateReferralCode(dto.FullName)
        };

        // Parrainage si code fourni
        if (!string.IsNullOrEmpty(dto.ReferralCode))
        {
            var parrain = await _context.Clients
                .FirstOrDefaultAsync(c => c.ReferralCode == dto.ReferralCode.ToUpper());
            if (parrain != null)
                client.ReferredById = parrain.Id;
        }

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        try
        {
            await _emailService.SendNewClientNotificationAsync(
                client.FullName, client.Email, client.Phone);
        }
        catch (Exception ex) { Console.WriteLine("EMAIL ERROR: " + ex.Message); }

        return Ok(new { message = "Compte créé" });
    }

    // ══════════════════════════════════════════════════════════════════════
    // LOGIN — timing-attack safe + migration silencieuse SHA256 → BCrypt
    // ══════════════════════════════════════════════════════════════════════
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(ClientLoginDto dto)
    {
        var client = await _context.Clients
            .FirstOrDefaultAsync(x => x.Email == dto.Email);

        // Anti-timing-attack : on effectue toujours une vérification,
        // même si l'email n'existe pas, pour que le temps de réponse
        // soit identique qu'il y ait un compte ou non.
        if (client == null)
        {
            BCrypt.Net.BCrypt.Verify(dto.Password, _dummyHash);
            return Unauthorized("Identifiants invalides");
        }

        bool isValid;

        // ── Migration silencieuse SHA-256 → BCrypt ─────────────────────
        // Les anciens hash sont marqués "LEGACY:SHA256:<hash>" dans Program.cs
        if (client.PasswordHash.StartsWith("LEGACY:SHA256:"))
        {
            var oldHash = client.PasswordHash["LEGACY:SHA256:".Length..];
            var inputHash = Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(dto.Password)
                )
            );
            isValid = oldHash == inputHash;

            // Si mot de passe correct → re-hacher en BCrypt immédiatement
            if (isValid)
            {
                client.PasswordHash = HashPassword(dto.Password);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Migration BCrypt OK pour client #{client.Id}");
            }
        }
        else
        {
            // Hash BCrypt standard
            isValid = VerifyPassword(dto.Password, client.PasswordHash);
        }

        if (!isValid)
            return Unauthorized("Identifiants invalides");

        return Ok(new
        {
            id = client.Id,
            name = client.FullName,
            email = client.Email,
            phone = client.Phone,
            referralCode = client.ReferralCode,
            referralCredits = client.ReferralCredits,
            referralCount = client.ReferralCount
        });
    }

    // ══════════════════════════════════════════════════════════════════════
    // FORGOT PASSWORD
    // ══════════════════════════════════════════════════════════════════════
    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        var client = await _context.Clients
            .FirstOrDefaultAsync(x => x.Email == dto.Email);

        // Réponse identique que le compte existe ou non (anti-enumération)
        if (client == null)
            return Ok("Si le compte existe, un code a été envoyé.");

        if (client.ResetCodeExpiresAt.HasValue &&
            client.ResetCodeExpiresAt.Value > DateTime.UtcNow.AddMinutes(9))
        {
            return BadRequest("Veuillez patienter avant de redemander un code.");
        }

        var code = GenerateCode();   // ✅ OTP cryptographique
        client.ResetCode = code;
        client.ResetCodeExpiresAt = DateTime.UtcNow.AddMinutes(10);
        await _context.SaveChangesAsync();

        try
        {
            await _emailService.SendResetCodeAsync(client.Email, code);
        }
        catch (Exception ex)
        {
            Console.WriteLine("BREVO ERROR: " + ex.ToString());
            return StatusCode(500, "Erreur lors de l'envoi du code.");
        }

        return Ok("Code envoyé.");
    }

    // ══════════════════════════════════════════════════════════════════════
    // RESET PASSWORD
    // ══════════════════════════════════════════════════════════════════════
    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        var client = await _context.Clients
            .FirstOrDefaultAsync(x => x.Email == dto.Email);

        if (client == null) return BadRequest("Compte introuvable");
        if (client.ResetCode != dto.Code) return BadRequest("Code invalide");
        if (client.ResetCodeExpiresAt < DateTime.UtcNow) return BadRequest("Code expiré");

        client.PasswordHash = HashPassword(dto.NewPassword);   // ✅ BCrypt
        client.ResetCode = null;
        client.ResetCodeExpiresAt = null;
        await _context.SaveChangesAsync();

        return Ok("Mot de passe réinitialisé");
    }

    // ══════════════════════════════════════════════════════════════════════
    // PARRAINAGE
    // ══════════════════════════════════════════════════════════════════════
    [HttpGet("referral/{clientId}")]
    public async Task<IActionResult> GetReferral(int clientId)
    {
        var client = await _context.Clients.FindAsync(clientId);
        if (client == null) return NotFound();

        return Ok(new
        {
            referralCode = client.ReferralCode,
            referralCount = client.ReferralCount,
            referralCredits = client.ReferralCredits,
            referralLink = $"https://www.ranita-shop.com/register.html?ref={client.ReferralCode}"
        });
    }

    [HttpPost("apply-referral")]
    public async Task<IActionResult> ApplyReferral([FromBody] ApplyReferralDto dto)
    {
        var client = await _context.Clients.FindAsync(dto.ClientId);
        if (client == null) return NotFound();
        if (client.ReferredById != null) return Ok("Déjà parrainé");

        var parrain = await _context.Clients
            .FirstOrDefaultAsync(c => c.ReferralCode == dto.ReferralCode.ToUpper());
        if (parrain == null || parrain.Id == client.Id) return Ok("Code invalide");

        client.ReferredById = parrain.Id;
        await _context.SaveChangesAsync();
        return Ok("Parrainage appliqué");
    }

    [HttpGet("/ref/{code}")]
    public IActionResult RedirectReferral(string code)
        => Redirect($"/register.html?ref={code}");

    // ══════════════════════════════════════════════════════════════════════
    // COMMANDES CLIENT
    // ══════════════════════════════════════════════════════════════════════
    [HttpGet("orders/{clientId}")]
    public async Task<IActionResult> GetClientOrders(int clientId)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.ClientId == clientId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var result = orders.Select(o => new
        {
            o.Id,
            o.CustomerName,
            o.CustomerPhone,
            o.CustomerAddress,
            o.PaymentMethod,
            o.Total,
            o.ShippingFee,
            o.Status,
            o.CreatedAt,
            o.PromoDiscount,
            o.PromoCode,
            o.ReferralCreditUsed,
            Items = o.Items.Select(i => new
            {
                i.Id,
                i.ProductName,
                i.Price,
                i.Quantity,
                i.ImageUrl
            })
        });

        return Ok(result);
    }

    // ══════════════════════════════════════════════════════════════════════
    // ADMIN — liste clients
    // ══════════════════════════════════════════════════════════════════════
    [HttpGet("/api/clients")]
    public async Task<IActionResult> GetAllClients()
    {
        var clients = await _context.Clients
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new {
                c.Id,
                c.FullName,
                c.Email,
                c.Phone,
                c.CreatedAt,
                c.ReferralCode,
                c.ReferralCount,
                c.ReferralCredits
            })
            .ToListAsync();

        return Ok(clients);
    }

    // ══════════════════════════════════════════════════════════════════════
    // PROFIL — mise à jour
    // ══════════════════════════════════════════════════════════════════════
    [HttpPut("update/{clientId}")]
    public async Task<IActionResult> UpdateProfile(int clientId, [FromBody] UpdateProfileDto dto)
    {
        var client = await _context.Clients.FindAsync(clientId);
        if (client == null) return NotFound();

        if (!string.IsNullOrEmpty(dto.FullName)) client.FullName = dto.FullName;
        if (dto.Phone != null) client.Phone = dto.Phone;
        if (dto.Address != null) client.Address = dto.Address;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Profil mis à jour" });
    }

    // ══════════════════════════════════════════════════════════════════════
    // CHANGEMENT MOT DE PASSE — vérifie l'ancien ✅
    // ══════════════════════════════════════════════════════════════════════
    [HttpPut("change-password/{clientId}")]
    public async Task<IActionResult> ChangePassword(int clientId, [FromBody] ChangePasswordDto dto)
    {
        var client = await _context.Clients.FindAsync(clientId);
        if (client == null) return NotFound();

        // ✅ Vérifie l'ancien mot de passe avant d'autoriser le changement
        if (!VerifyPassword(dto.OldPassword, client.PasswordHash))
            return BadRequest("Ancien mot de passe incorrect");

        client.PasswordHash = HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Mot de passe mis à jour" });
    }

    // ══════════════════════════════════════════════════════════════════════
    // DTOs internes
    // ══════════════════════════════════════════════════════════════════════
    public class ApplyReferralDto
    {
        public int ClientId { get; set; }
        public string ReferralCode { get; set; } = "";
    }

    public class UpdateProfileDto
    {
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    public class ChangePasswordDto
    {
        public string OldPassword { get; set; } = "";   // ✅ Nouveau champ requis
        public string NewPassword { get; set; } = "";
    }
}