// ═══════════════════════════════════════════════════════════════
// RANITA MARKET — Services/JwtService.cs
// Nouveau fichier à créer dans le dossier Services/
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
        private readonly int _expiryDays = 30; // Token valide 30 jours

        public JwtService(IConfiguration config)
        {
            _secret = config["JWT_SECRET"]
                ?? throw new InvalidOperationException(
                    "JWT_SECRET manquant dans les variables d'environnement Railway");
        }

        // ── Génère un token pour un client ────────────────────────────
        public string GenerateClientToken(int clientId, string email)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("clientId", clientId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim("role", "client"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: "ranita-shop.com",
                audience: "ranita-shop.com",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(_expiryDays),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ── Génère un token pour un vendeur ───────────────────────────
        public string GenerateSellerToken(int sellerId, int clientId, string email)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("sellerId", sellerId.ToString()),
                new Claim("clientId", clientId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim("role", "seller"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: "ranita-shop.com",
                audience: "ranita-shop.com",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(_expiryDays),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ── Valide un token et retourne les claims ────────────────────
        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
                var handler = new JwtSecurityTokenHandler();

                var principal = handler.ValidateToken(token, new TokenValidationParameters
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

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }

    // ── Extension pour lire facilement les claims dans les controllers ──
    public static class ClaimsPrincipalExtensions
    {
        public static int GetClientId(this ClaimsPrincipal user)
            => int.Parse(user.FindFirstValue("clientId") ?? "0");

        public static int GetSellerId(this ClaimsPrincipal user)
            => int.Parse(user.FindFirstValue("sellerId") ?? "0");

        public static string GetRole(this ClaimsPrincipal user)
            => user.FindFirstValue("role") ?? "";
    }
}