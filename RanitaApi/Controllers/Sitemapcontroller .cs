using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using System.Text;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("")]
    public class SitemapController : ControllerBase
    {
        private readonly AppDbContext _db;
        private const string BASE_URL = "https://www.ranita-shop.com";

        public SitemapController(AppDbContext db)
        {
            _db = db;
        }

        // GET /sitemap-dynamic.xml
        [HttpGet("sitemap-dynamic.xml")]
        public async Task<IActionResult> GetSitemap()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"");
            sb.AppendLine("        xmlns:image=\"http://www.google.com/schemas/sitemap-image/1.1\">");

            // ── Pages statiques ──────────────────────────────────────────────
            var staticPages = new[]
            {
                (url: "/", freq: "daily",   priority: "1.0"),
                (url: "/products.html",     freq: "daily",   priority: "0.9"),
                (url: "/register.html",     freq: "monthly", priority: "0.5"),
                (url: "/vendeur.html",      freq: "monthly", priority: "0.5"),
                (url: "/login.html",        freq: "monthly", priority: "0.3"),
            };

            foreach (var page in staticPages)
            {
                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{BASE_URL}{page.url}</loc>");
                sb.AppendLine($"    <changefreq>{page.freq}</changefreq>");
                sb.AppendLine($"    <priority>{page.priority}</priority>");
                sb.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
                sb.AppendLine("  </url>");
            }

            // ── Catégories ───────────────────────────────────────────────────
            var categories = await _db.Categories
                .Where(c => c.ParentId == null)
                .OrderBy(c => c.Name)
                .ToListAsync();

            foreach (var cat in categories)
            {
                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{BASE_URL}/products.html?category={Uri.EscapeDataString(cat.Name)}</loc>");
                sb.AppendLine("    <changefreq>daily</changefreq>");
                sb.AppendLine("    <priority>0.8</priority>");
                sb.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
                sb.AppendLine("  </url>");
            }

            // ── Produits actifs ──────────────────────────────────────────────
            var products = await _db.Products
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            foreach (var p in products)
            {
                // Images
                List<string> images = new();
                try
                {
                    images = System.Text.Json.JsonSerializer.Deserialize<List<string>>(p.Images ?? "[]") ?? new();
                }
                catch { }
                if (images.Count == 0 && !string.IsNullOrEmpty(p.ImageUrl))
                    images.Add(p.ImageUrl);

                // Titre SEO propre
                var title = System.Net.WebUtility.HtmlEncode(p.Name);

                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{BASE_URL}/product-details.html?id={p.Id}</loc>");
                sb.AppendLine("    <changefreq>weekly</changefreq>");
                sb.AppendLine("    <priority>0.85</priority>");
                sb.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");

                // Balises image pour Google Images
                foreach (var imgUrl in images.Take(3))
                {
                    sb.AppendLine("    <image:image>");
                    sb.AppendLine($"      <image:loc>{System.Net.WebUtility.HtmlEncode(imgUrl)}</image:loc>");
                    sb.AppendLine($"      <image:title>{title}</image:title>");
                    sb.AppendLine("    </image:image>");
                }

                sb.AppendLine("  </url>");
            }

            // ── Boutiques vendeurs ───────────────────────────────────────────
            var sellers = await _db.Sellers
                .Where(s => s.Status == "Approved")
                .ToListAsync();

            foreach (var s in sellers)
            {
                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{BASE_URL}/boutique.html?id={s.Id}</loc>");
                sb.AppendLine("    <changefreq>weekly</changefreq>");
                sb.AppendLine("    <priority>0.7</priority>");
                sb.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
                sb.AppendLine("  </url>");
            }

            sb.AppendLine("</urlset>");

            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }
    }
}