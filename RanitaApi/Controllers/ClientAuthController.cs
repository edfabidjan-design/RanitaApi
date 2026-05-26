using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.DTO;
using RanitaApi.Models;
using System.Security.Cryptography;
using System.Text;
using RanitaApi.Services;

[ApiController]
[Route("api/client-auth")]
public class ClientAuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly EmailService _emailService;

    public ClientAuthController(AppDbContext context, EmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    string GenerateCode()
    {
        var rnd = new Random();
        return rnd.Next(100000, 999999).ToString();
    }

    // Génère un code parrainage unique ex: KALIM4821
    string GenerateReferralCode(string fullName)
    {
        var first = fullName.Split(' ')[0].ToUpper();
        if (first.Length > 6) first = first.Substring(0, 6);
        var rnd = new Random();
        return first + rnd.Next(1000, 9999).ToString();
    }

    [HttpPost("register")]
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

        // Gérer le parrainage si un code est fourni
        if (!string.IsNullOrEmpty(dto.ReferralCode))
        {
            var parrain = await _context.Clients
                .FirstOrDefaultAsync(c => c.ReferralCode == dto.ReferralCode.ToUpper());
            if (parrain != null)
                client.ReferredById = parrain.Id;
        }

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        // Email notification admin
        try
        {
            await _emailService.SendNewClientNotificationAsync(
                client.FullName, client.Email, client.Phone);
        }
        catch (Exception ex) { Console.WriteLine("EMAIL ERROR: " + ex.Message); }

        return Ok(new { message = "Compte créé" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(ClientLoginDto dto)
    {
        var hash = HashPassword(dto.Password);

        var client = await _context.Clients
            .FirstOrDefaultAsync(x => x.Email == dto.Email && x.PasswordHash == hash);

        if (client == null)
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

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        var client = await _context.Clients
            .FirstOrDefaultAsync(x => x.Email == dto.Email);

        if (client == null)
            return Ok("Si le compte existe, un code a été envoyé.");

        if (client.ResetCodeExpiresAt.HasValue &&
            client.ResetCodeExpiresAt.Value > DateTime.UtcNow.AddMinutes(9))
        {
            return BadRequest("Veuillez patienter avant de redemander un code.");
        }

        var code = GenerateCode();
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
            return StatusCode(500, "BREVO ERROR: " + ex.Message);
        }

        return Ok("Code envoyé.");
    }

    [HttpPost("reset-password")]
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

    // ── PARRAINAGE : infos du client ──────────────────────────
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

    public class ApplyReferralDto
    {
        public int ClientId { get; set; }
        public string ReferralCode { get; set; } = "";
    }

    [HttpGet("/ref/{code}")]
    public IActionResult RedirectReferral(string code)
    {
        return Redirect($"/register.html?ref={code}");
    }
}
