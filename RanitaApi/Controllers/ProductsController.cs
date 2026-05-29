using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RanitaApi.Data;
using RanitaApi.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;

        // Clés de cache
        private const string CACHE_ALL_PRODUCTS = "products:all";
        private const string CACHE_PRODUCT_PREFIX = "products:id:";
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(5);

        public ProductsController(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // ══════════════════════════════════════════════════════════════
        // GET ALL — avec cache 5 minutes ✅
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search = null)
        {
            // Recherche → pas de cache (résultats dynamiques)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var allForSearch = await LoadAllProductsFromDb();
                var words = search.ToLower().Trim()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);

                var filtered = allForSearch.Where(p =>
                    words.All(w =>
                        p.Name.ToLower().Contains(w) ||
                        (p.Brand ?? "").ToLower().Contains(w) ||
                        (p.ShortDescription ?? "").ToLower().Contains(w) ||
                        (p.Category?.Name ?? "").ToLower().Contains(w)
                    )
                ).ToList();

                return Ok(MapProducts(filtered));
            }

            // Sans recherche → cache
            if (!_cache.TryGetValue(CACHE_ALL_PRODUCTS, out List<Product>? cachedProducts) || cachedProducts == null)
            {
                cachedProducts = await LoadAllProductsFromDb();

                _cache.Set(CACHE_ALL_PRODUCTS, cachedProducts, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CACHE_DURATION,
                    SlidingExpiration = TimeSpan.FromMinutes(2),
                    Size = 1
                });
            }

            return Ok(MapProducts(cachedProducts));
        }

        // ══════════════════════════════════════════════════════════════
        // GET BY ID — avec cache individuel ✅
        // ══════════════════════════════════════════════════════════════
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var cacheKey = CACHE_PRODUCT_PREFIX + id;

            if (!_cache.TryGetValue(cacheKey, out Product? p) || p == null)
            {
                p = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Variants)
                    .Where(p => p.Id == id)
                    .FirstOrDefaultAsync();

                if (p == null) return NotFound();

                _cache.Set(cacheKey, p, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CACHE_DURATION,
                    Size = 1
                });
            }

            return Ok(MapProduct(p));
        }

        // ══════════════════════════════════════════════════════════════
        // CREATE — invalide le cache ✅
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] Product product, List<IFormFile>? imageFiles)
        {
            ModelState.Remove("Brand"); ModelState.Remove("ShortDescription");
            ModelState.Remove("Description"); ModelState.Remove("Slug");
            ModelState.Remove("MetaDescription"); ModelState.Remove("ImageUrl");
            ModelState.Remove("Images"); ModelState.Remove("Attributes"); ModelState.Remove("Sku");

            if (product.CategoryId == null || product.CategoryId <= 0)
                return BadRequest("Catégorie obligatoire.");

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == product.CategoryId);
            if (!categoryExists) return BadRequest("Catégorie introuvable.");

            var imageUrls = await UploadImages(imageFiles);

            if (imageUrls.Count > 0)
            {
                product.ImageUrl = imageUrls[0];
                product.Images = System.Text.Json.JsonSerializer.Serialize(imageUrls);
            }

            var isActiveStr = Request.Form["IsActive"].FirstOrDefault();
            product.IsActive = isActiveStr == "true" || isActiveStr == "True";

            product.Brand = product.Brand ?? "";
            product.ShortDescription = product.ShortDescription ?? "";
            product.Slug = product.Slug ?? "";
            product.MetaDescription = product.MetaDescription ?? "";
            product.Attributes = product.Attributes ?? "{}";
            product.Sku = product.Sku ?? "";
            product.ImageUrl = product.ImageUrl ?? "";
            product.Images = product.Images ?? "[]";

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            await SaveVariants(product.Id, Request.Form["Variants"].FirstOrDefault());

            // ✅ Invalide le cache — les prochains GET verront le nouveau produit
            InvalidateProductCache(product.Id);

            return Ok(product);
        }

        // ══════════════════════════════════════════════════════════════
        // UPDATE — invalide le cache ✅
        // ══════════════════════════════════════════════════════════════
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] Product updated, List<IFormFile>? imageFiles)
        {
            ModelState.Remove("Brand"); ModelState.Remove("ShortDescription");
            ModelState.Remove("Description"); ModelState.Remove("Slug");
            ModelState.Remove("MetaDescription"); ModelState.Remove("ImageUrl");
            ModelState.Remove("Images"); ModelState.Remove("Attributes"); ModelState.Remove("Sku");

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.Name = updated.Name;
            product.Price = updated.Price;
            product.OldPrice = updated.OldPrice;
            product.Stock = updated.Stock;
            product.ShortDescription = updated.ShortDescription ?? "";
            product.Description = updated.Description;
            product.CategoryId = updated.CategoryId;
            product.Brand = updated.Brand ?? "";
            product.Slug = updated.Slug ?? "";
            product.MetaDescription = updated.MetaDescription ?? "";
            product.Attributes = updated.Attributes ?? "{}";
            product.Sku = updated.Sku ?? "";

            var isActiveStr = Request.Form["IsActive"].FirstOrDefault();
            product.IsActive = isActiveStr == "true" || isActiveStr == "True";

            // Images
            var existingImages = new List<string>();
            try { existingImages = System.Text.Json.JsonSerializer.Deserialize<List<string>>(updated.Images ?? "[]") ?? new(); }
            catch { }

            var newImages = await UploadImages(imageFiles);
            existingImages.AddRange(newImages);

            if (existingImages.Count > 0)
            {
                product.ImageUrl = existingImages[0];
                product.Images = System.Text.Json.JsonSerializer.Serialize(existingImages);
            }

            await _context.SaveChangesAsync();
            await SaveVariants(id, Request.Form["Variants"].FirstOrDefault());

            // ✅ Invalide le cache
            InvalidateProductCache(id);

            return Ok(product);
        }

        // ══════════════════════════════════════════════════════════════
        // DELETE — invalide le cache ✅
        // ══════════════════════════════════════════════════════════════
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            // ✅ Invalide le cache
            InvalidateProductCache(id);

            return Ok("Supprimé");
        }

        // ══════════════════════════════════════════════════════════════
        // ADMIN ONLY — sans cache (usage admin uniquement)
        // ══════════════════════════════════════════════════════════════
        [HttpGet("admin-only")]
        public async Task<IActionResult> GetAdminOnly()
        {
            var sellerProductIds = await _context.SellerProducts
                .Where(sp => sp.ProductId != null)
                .Select(sp => sp.ProductId.Value)
                .ToListAsync();

            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Where(p => p.IsActive && !sellerProductIds.Contains(p.Id))
                .ToListAsync();

            var result = products.Select(p => new {
                p.Id,
                p.Name,
                p.Price,
                p.OldPrice,
                Stock = p.Variants != null && p.Variants.Any() ? p.Variants.Sum(v => v.Stock) : p.Stock,
                p.ShortDescription,
                p.ImageUrl,
                p.Images,
                p.CategoryId,
                p.Brand,
                p.IsActive,
                Category = p.Category == null ? null : new { p.Category.Id, p.Category.Name, p.Category.ParentId },
                Variants = p.Variants?.Select(v => new { v.Id, v.Combination, v.Stock, v.Price })
            });

            return Ok(result);
        }

        // ══════════════════════════════════════════════════════════════
        // HELPERS PRIVÉS
        // ══════════════════════════════════════════════════════════════

        private async Task<List<Product>> LoadAllProductsFromDb()
            => await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .AsNoTracking()   // ✅ Plus rapide pour les lectures
                .ToListAsync();

        private void InvalidateProductCache(int productId)
        {
            _cache.Remove(CACHE_ALL_PRODUCTS);
            _cache.Remove(CACHE_PRODUCT_PREFIX + productId);
        }

        private static object MapProduct(Product p) => new
        {
            p.Id,
            p.Name,
            p.Price,
            p.OldPrice,
            Stock = p.Variants != null && p.Variants.Any() ? p.Variants.Sum(v => v.Stock) : p.Stock,
            p.ShortDescription,
            p.Description,
            p.ImageUrl,
            p.Images,
            p.CategoryId,
            p.Brand,
            p.Sku,
            p.IsActive,
            p.Slug,
            p.MetaDescription,
            Attributes = p.Attributes ?? "{}",
            Category = p.Category == null ? null : new { p.Category.Id, p.Category.Name, p.Category.ParentId },
            Variants = p.Variants?.Select(v => new { v.Id, v.Combination, v.Stock, v.Price })
        };

        private static IEnumerable<object> MapProducts(IEnumerable<Product> products)
            => products.Select(p => MapProduct(p));

        private async Task<List<string>> UploadImages(List<IFormFile>? imageFiles)
        {
            var urls = new List<string>();
            if (imageFiles == null || imageFiles.Count == 0) return urls;

            var cloudinaryUrl = Environment.GetEnvironmentVariable("CLOUDINARY_URL");
            var cloudinary = new Cloudinary(cloudinaryUrl);
            cloudinary.Api.Secure = true;

            foreach (var file in imageFiles)
            {
                await using var stream = file.OpenReadStream();
                var result = await cloudinary.UploadAsync(new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "ranita-products"
                });
                urls.Add(result.SecureUrl.ToString());
            }
            return urls;
        }

        private async Task SaveVariants(int productId, string? variantsJson)
        {
            if (string.IsNullOrWhiteSpace(variantsJson)) return;
            try
            {
                var variants = System.Text.Json.JsonSerializer.Deserialize<List<ProductVariant>>(
                    variantsJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
                if (variants == null || variants.Count == 0) return;

                var old = _context.ProductVariants.Where(v => v.ProductId == productId);
                _context.ProductVariants.RemoveRange(old);
                await _context.SaveChangesAsync();

                foreach (var v in variants)
                {
                    v.Id = 0;
                    v.ProductId = productId;
                    _context.ProductVariants.Add(v);
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception ex) { Console.WriteLine($"[ERROR] Variantes: {ex.Message}"); }
        }
    }
}