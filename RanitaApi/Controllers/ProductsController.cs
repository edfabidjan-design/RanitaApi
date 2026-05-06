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
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.OldPrice,
                    p.Stock,
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
                    Category = p.Category == null ? null : new
                    {
                        p.Category.Id,
                        p.Category.Name
                    }
                })
                .ToListAsync();

            return Ok(products);
        }

        // ✅ GET BY ID
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.OldPrice,
                    p.Stock,
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
                    Category = p.Category == null ? null : new
                    {
                        p.Category.Id,
                        p.Category.Name
                    }
                })
                .FirstOrDefaultAsync();

            if (product == null)
                return NotFound();

            return Ok(product);
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


        [HttpGet("run-migrations")]
        public IActionResult RunMigrations()
        {
            try
            {
                var conn = _context.Database.GetDbConnection();
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""OldPrice"" NUMERIC(18,2) NULL;
            ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""ShortDescription"" TEXT NOT NULL DEFAULT '';
            ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""IsActive"" BOOLEAN NOT NULL DEFAULT TRUE;
            ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""Brand"" TEXT NOT NULL DEFAULT '';
            ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""Slug"" TEXT NOT NULL DEFAULT '';
            ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""MetaDescription"" TEXT NOT NULL DEFAULT '';
            ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""Attributes"" TEXT NOT NULL DEFAULT '{}';
            ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""Sku"" TEXT NOT NULL DEFAULT '';
            ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""Images"" TEXT NOT NULL DEFAULT '[]';
            ALTER TABLE ""Products"" ADD COLUMN IF NOT EXISTS ""ImageUrl"" TEXT NOT NULL DEFAULT '';
            ALTER TABLE ""Orders"" ADD COLUMN IF NOT EXISTS ""ClientId"" INT NULL;
            ALTER TABLE ""Categories"" ADD COLUMN IF NOT EXISTS ""ParentId"" INT NULL;
        ";
                cmd.ExecuteNonQuery();
                conn.Close();
                return Ok("Migrations OK");
            }
            catch (Exception ex)
            {
                return BadRequest("Erreur: " + ex.Message);
            }
        }

    }

}