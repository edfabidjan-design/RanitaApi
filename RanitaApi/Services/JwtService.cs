// ═══════════════════════════════════════════════════════════════
// RANITA MARKET — Services/JwtService.cs
// ═══════════════════════════════════════════════════════════════
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RanitaApi.Services
{
    public class JwtService
    {
        private readonly string _secret;
        private readonly int _clientExpiryDays = 30;
        private readonly int _adminExpiryHours = 8;

        public JwtService(IConfiguration config)
        {
            _secret = config["JWT_SECRET"]
                ?? throw new InvalidOperationException(
                    "JWT_SECRET manquant dans les variables d'environnement Railway");
        }

        // ── Token CLIENT ──────────────────────────────────────────────
        public string GenerateClientToken(int clientId, string email)
        {
            var claims = new[]
            {
                new Claim("clientId", clientId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim("role", "client"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            return BuildToken(claims, TimeSpan.FromDays(_clientExpiryDays));
        }

        // ── Token VENDEUR ─────────────────────────────────────────────
        public string GenerateSellerToken(int sellerId, int clientId, string email)
        {
            var claims = new[]
            {
                new Claim("sellerId",  sellerId.ToString()),
                new Claim("clientId",  clientId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim("role", "seller"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            return BuildToken(claims, TimeSpan.FromDays(_clientExpiryDays));
        }

        // ── Token ADMIN ✅ ────────────────────────────────────────────
        public string GenerateAdminToken(int adminId, string username, string role, string? email)
        {
            var claims = new[]
            {
                new Claim("adminId",            adminId.ToString()),
                new Claim(ClaimTypes.Name,      username),
                new Claim(ClaimTypes.Role,      role),
                new Claim("role",               "admin"),
                new Claim("adminRole",          role),
                new Claim(ClaimTypes.Email,     email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            return BuildToken(claims, TimeSpan.FromHours(_adminExpiryHours));
        }

        // ── Validation token ──────────────────────────────────────────
        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
                var handler = new JwtSecurityTokenHandler();

                return handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "ranita-shop.com",
                    ValidAudience = "ranita-shop.com",
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.Zero
                }, out _);
            }
            catch { return null; }
        }

        // ── Builder interne ───────────────────────────────────────────
        private string BuildToken(Claim[] claims, TimeSpan expiry)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "ranita-shop.com",
                audience: "ranita-shop.com",
                claims: claims,
                expires: DateTime.UtcNow.Add(expiry),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // ── Extensions claims ─────────────────────────────────────────────
    public static class ClaimsPrincipalExtensions
    {
        public static int GetClientId(this ClaimsPrincipal user)
            => int.Parse(user.FindFirstValue("clientId") ?? "0");

        public static int GetSellerId(this ClaimsPrincipal user)
            => int.Parse(user.FindFirstValue("sellerId") ?? "0");

        public static int GetAdminId(this ClaimsPrincipal user)
            => int.Parse(user.FindFirstValue("adminId") ?? "0");

        public static string GetRole(this ClaimsPrincipal user)
            => user.FindFirstValue("role") ?? "";

        public static string GetAdminRole(this ClaimsPrincipal user)
            => user.FindFirstValue("adminRole") ?? "";
    }
}