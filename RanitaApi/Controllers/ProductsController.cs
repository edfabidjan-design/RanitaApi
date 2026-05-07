using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ GET ALL
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .ToListAsync();

            var result = products.Select(p => new
            {
                p.Id,
                p.Name,
                p.Price,
                p.OldPrice,
                Stock = p.Variants != null && p.Variants.Any()
                    ? p.Variants.Sum(v => v.Stock)
                    : p.Stock,
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
                p.Attributes,
                Category = p.Category == null ? null : new { p.Category.Id, p.Category.Name },
                Variants = p.Variants?.Select(v => new { v.Id, v.Combination, v.Stock, v.Price })
            });

            return Ok(result);
        }

        // ✅ GET BY ID
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var p = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync();

            if (p == null) return NotFound();

            var result = new
            {
                p.Id,
                p.Name,
                p.Price,
                p.OldPrice,
                Stock = p.Variants != null && p.Variants.Any()
                    ? p.Variants.Sum(v => v.Stock)
                    : p.Stock,
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
                p.Attributes,
                Category = p.Category == null ? null : new { p.Category.Id, p.Category.Name },
                Variants = p.Variants?.Select(v => new { v.Id, v.Combination, v.Stock, v.Price })
            };

            return Ok(result);
        }

        // ✅ CREATE
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] Product product, List<IFormFile>? imageFiles)
        {
            if (product.CategoryId == null || product.CategoryId <= 0)
                return BadRequest("Catégorie obligatoire.");

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == product.CategoryId);
            if (!categoryExists)
                return BadRequest("Catégorie introuvable.");

            var imageUrls = new List<string>();

            if (imageFiles != null && imageFiles.Count > 0)
            {
                var cloudinaryUrl = Environment.GetEnvironmentVariable("CLOUDINARY_URL");
                var cloudinary = new Cloudinary(cloudinaryUrl);
                cloudinary.Api.Secure = true;

                foreach (var file in imageFiles)
                {
                    await using var stream = file.OpenReadStream();
                    var uploadResult = await cloudinary.UploadAsync(new ImageUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        Folder = "ranita-products"
                    });
                    imageUrls.Add(uploadResult.SecureUrl.ToString());
                }
            }

            if (imageUrls.Count > 0)
            {
                product.ImageUrl = imageUrls[0];
                product.Images = System.Text.Json.JsonSerializer.Serialize(imageUrls);
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Sauvegarder les variantes
            var variantsJson = Request.Form["Variants"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(variantsJson))
            {
                try
                {
                    var variants = System.Text.Json.JsonSerializer.Deserialize<List<ProductVariant>>(
                        variantsJson,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    if (variants != null && variants.Count > 0)
                    {
                        var old = _context.ProductVariants.Where(v => v.ProductId == product.Id);
                        _context.ProductVariants.RemoveRange(old);
                        await _context.SaveChangesAsync();

                        foreach (var v in variants)
                        {
                            v.Id = 0;
                            v.ProductId = product.Id;
                            _context.ProductVariants.Add(v);
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Variantes Update: {ex.Message}");
                }
            }

            return Ok(product);
        }

        // ✅ UPDATE
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] Product updated, List<IFormFile>? imageFiles)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            product.Name = updated.Name;
            product.Price = updated.Price;
            product.OldPrice = updated.OldPrice;
            product.Stock = updated.Stock;
            product.ShortDescription = updated.ShortDescription;
            product.Description = updated.Description;
            product.CategoryId = updated.CategoryId;
            product.Brand = updated.Brand;
            product.IsActive = updated.IsActive;
            product.Slug = updated.Slug;
            product.MetaDescription = updated.MetaDescription;
            product.Attributes = updated.Attributes;
            product.Sku = updated.Sku;

            // Images existantes conservées
            var existingImages = new List<string>();
            try
            {
                existingImages = System.Text.Json.JsonSerializer
                    .Deserialize<List<string>>(updated.Images ?? "[]") ?? new();
            }
            catch { }

            // Upload nouvelles images
            if (imageFiles != null && imageFiles.Count > 0)
            {
                var cloudinaryUrl = Environment.GetEnvironmentVariable("CLOUDINARY_URL");
                var cloudinary = new Cloudinary(cloudinaryUrl);
                cloudinary.Api.Secure = true;

                foreach (var file in imageFiles)
                {
                    await using var stream = file.OpenReadStream();
                    var uploadResult = await cloudinary.UploadAsync(new ImageUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        Folder = "ranita-products"
                    });
                    existingImages.Add(uploadResult.SecureUrl.ToString());
                }
            }

            if (existingImages.Count > 0)
            {
                product.ImageUrl = existingImages[0];
                product.Images = System.Text.Json.JsonSerializer.Serialize(existingImages);
            }

            await _context.SaveChangesAsync();

            // Sauvegarder les variantes
            var variantsJson = Request.Form["Variants"].ToString();
            if (!string.IsNullOrEmpty(variantsJson))
            {
                var variants = System.Text.Json.JsonSerializer.Deserialize<List<ProductVariant>>(variantsJson);
                if (variants != null)
                {
                    // Supprimer les anciennes variantes
                    var old = _context.ProductVariants.Where(v => v.ProductId == product.Id);
                    _context.ProductVariants.RemoveRange(old);

                    // Ajouter les nouvelles
                    foreach (var v in variants)
                    {
                        v.ProductId = product.Id;
                        _context.ProductVariants.Add(v);
                    }
                    await _context.SaveChangesAsync();
                }
            }


            return Ok(product);
        }

        // ✅ DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok("Supprimé");
        }




    }

}