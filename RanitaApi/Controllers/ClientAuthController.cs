using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.DTO;
using RanitaApi.Models;
using System.Security.Cryptography;
using System.Text;

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
}