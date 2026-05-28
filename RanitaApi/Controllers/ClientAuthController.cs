using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
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
    private readonly JwtService _jwt;

    private static readonly string _dummyHash =
        BCrypt.Net.BCrypt.HashPassword("dummy-timing-protection", workFactor: 12);

    public ClientAuthController(AppDbContext context, EmailService emailService, JwtService jwt)
    {
        _context = context;
        _emailService = emailService;
        _jwt = jwt;
    }

    string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    bool VerifyPassword(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);

    string GenerateCode()
    {
        var bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);
        var value = BitConverter.ToUInt32(bytes) % 900000 + 100000;
        return value.ToString();
    }

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
            PasswordHash = HashPassword(dto.Password),
            ReferralCode = GenerateReferralCode(dto.FullName)
        };

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
    // LOGIN — retourne un JWT ✅
    // ══════════════════════════════════════════════════════════════════════
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(ClientLoginDto dto)
    {
        var client = await _context.Clients
            .FirstOrDefaultAsync(x => x.Email == dto.Email);

        // Anti-timing-attack
        if (client == null)
        {
            BCrypt.Net.BCrypt.Verify(dto.Password, _dummyHash);
            return Unauthorized("Identifiants invalides");
        }

        bool isValid;

        // Migration silencieuse SHA-256 → BCrypt
        if (client.PasswordHash.StartsWith("LEGACY:SHA256:"))
        {
            var oldHash = client.PasswordHash["LEGACY:SHA256:".Length..];
            var inputHash = Convert.ToBase64String(
                SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(dto.Password))
            );
            isValid = oldHash == inputHash;

            if (isValid)
            {
                client.PasswordHash = HashPassword(dto.Password);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Migration BCrypt OK pour client #{client.Id}");
            }
        }
        else
        {
            isValid = VerifyPassword(dto.Password, client.PasswordHash);
        }

        if (!isValid)
            return Unauthorized("Identifiants invalides");

        // ✅ Génère le token JWT
        var token = _jwt.GenerateClientToken(client.Id, client.Email);

        return Ok(new
        {
            token,                              // ✅ JWT à stocker côté client
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

        if (client == null)
            return Ok("Si le compte existe, un code a été envoyé.");

        if (client.ResetCodeExpiresAt.HasValue &&
            client.ResetCodeExpiresAt.Value > DateTime.UtcNow.AddMinutes(9))
            return BadRequest("Veuillez patienter avant de redemander un code.");

        var code = GenerateCode();
        client.ResetCode = code;
        client.ResetCodeExpiresAt = DateTime.UtcNow.AddMinutes(10);
        await _context.SaveChangesAsync();

        try { await _emailService.SendResetCodeAsync(client.Email, code); }
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

        client.PasswordHash = HashPassword(dto.NewPassword);
        client.ResetCode = null;
        client.ResetCodeExpiresAt = null;
        await _context.SaveChangesAsync();

        return Ok("Mot de passe réinitialisé");
    }

    // ══════════════════════════════════════════════════════════════════════
    // PARRAINAGE
    // ══════════════════════════════════════════════════════════════════════
    [HttpGet("referral/{clientId}")]
    [Authorize]   // ✅ Protégé — token requis
    public async Task<IActionResult> GetReferral(int clientId)
    {
        // Vérifie que le client ne consulte que ses propres données
        if (User.GetClientId() != clientId)
            return Forbid();

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
    // COMMANDES CLIENT — protégé ✅
    // ══════════════════════════════════════════════════════════════════════
    [HttpGet("orders/{clientId}")]
    [Authorize]
    public async Task<IActionResult> GetClientOrders(int clientId)
    {
        // Un client ne peut voir que SES commandes
        if (User.GetClientId() != clientId)
            return Forbid();

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
    // ADMIN — liste clients (protégé) ✅
    // ══════════════════════════════════════════════════════════════════════
    [HttpGet("/api/clients")]
    [Authorize]
    public async Task<IActionResult> GetAllClients()
    {
        // Seul un admin peut lister tous les clients
        // (à compléter avec [Authorize(Roles = "admin")] quand l'auth admin sera en place)
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
    // PROFIL — protégé ✅
    // ══════════════════════════════════════════════════════════════════════
    [HttpPut("update/{clientId}")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(int clientId, [FromBody] UpdateProfileDto dto)
    {
        if (User.GetClientId() != clientId) return Forbid();

        var client = await _context.Clients.FindAsync(clientId);
        if (client == null) return NotFound();

        if (!string.IsNullOrEmpty(dto.FullName)) client.FullName = dto.FullName;
        if (dto.Phone != null) client.Phone = dto.Phone;
        if (dto.Address != null) client.Address = dto.Address;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Profil mis à jour" });
    }

    // ══════════════════════════════════════════════════════════════════════
    // CHANGEMENT MOT DE PASSE — protégé ✅
    // ══════════════════════════════════════════════════════════════════════
    [HttpPut("change-password/{clientId}")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(int clientId, [FromBody] ChangePasswordDto dto)
    {
        if (User.GetClientId() != clientId) return Forbid();

        var client = await _context.Clients.FindAsync(clientId);
        if (client == null) return NotFound();

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
        public string OldPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}