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

    public ClientAuthController(AppDbContext context)
    {
        _context = context;
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
            PasswordHash = HashPassword(dto.Password)
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

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
            email = client.Email
        });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        var client = await _context.Clients
            .FirstOrDefaultAsync(x => x.Email == dto.Email);

        if (client == null)
            return Ok("Si le compte existe, un code a été envoyé.");

        var code = GenerateCode();

        client.ResetCode = code;
        client.ResetCodeExpiresAt = DateTime.UtcNow.AddMinutes(10);

        await _context.SaveChangesAsync();

        // 👉 TEMPORAIRE (dev)
        Console.WriteLine($"RESET CODE for {client.Email}: {code}");

        return Ok("Code envoyé.");
    }


    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        var client = await _context.Clients
            .FirstOrDefaultAsync(x => x.Email == dto.Email);

        if (client == null)
            return BadRequest("Compte introuvable");

        if (client.ResetCode != dto.Code)
            return BadRequest("Code invalide");

        if (client.ResetCodeExpiresAt < DateTime.UtcNow)
            return BadRequest("Code expiré");

        client.PasswordHash = HashPassword(dto.NewPassword);
        client.ResetCode = null;
        client.ResetCodeExpiresAt = null;

        await _context.SaveChangesAsync();

        return Ok("Mot de passe réinitialisé");
    }
}