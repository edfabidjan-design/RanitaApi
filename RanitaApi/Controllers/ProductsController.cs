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
                    p.Stock,
                    p.Description,
                    p.ImageUrl,
                    p.CategoryId,
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
                    p.Stock,
                    p.Description,
                    p.ImageUrl,
                    p.CategoryId,
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
        public async Task<IActionResult> Create([FromForm] Product product, IFormFile image)
        {
            if (product.CategoryId == null || product.CategoryId <= 0)
                return BadRequest("Catégorie obligatoire.");

            var categoryExists = await _context.Categories.AnyAsync(c => c.Id == product.CategoryId);
            if (!categoryExists)
                return BadRequest("Catégorie introuvable.");

            if (image != null)
            {
                var cloudinaryUrl = Environment.GetEnvironmentVariable("CLOUDINARY_URL");
                var cloudinary = new Cloudinary(cloudinaryUrl);
                cloudinary.Api.Secure = true;

                await using var stream = image.OpenReadStream();

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(image.FileName, stream),
                    Folder = "ranita-products"
                };

                var uploadResult = await cloudinary.UploadAsync(uploadParams);

                product.ImageUrl = uploadResult.SecureUrl.ToString();
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return Ok(product);
        }

        // ✅ UPDATE
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] Product updated, IFormFile? image)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            product.Name = updated.Name;
            product.Price = updated.Price;
            product.Stock = updated.Stock;
            product.Description = updated.Description;
            product.CategoryId = updated.CategoryId;

                if (image != null)
                {
                    var cloudinaryUrl = Environment.GetEnvironmentVariable("CLOUDINARY_URL");
                    var cloudinary = new Cloudinary(cloudinaryUrl);
                    cloudinary.Api.Secure = true;

                    await using var stream = image.OpenReadStream();

                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(image.FileName, stream),
                        Folder = "ranita-products"
                    };

                    var uploadResult = await cloudinary.UploadAsync(uploadParams);

                    product.ImageUrl = uploadResult.SecureUrl.ToString();
                }
            

            await _context.SaveChangesAsync();

            return Ok(product);
        }


        // ✅ UPDATE avec FormData en POST
        [HttpPost("update/{id}")]
        public async Task<IActionResult> UpdatePost(int id, [FromForm] Product updated, IFormFile? image)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            product.Name = updated.Name;
            product.Price = updated.Price;
            product.Stock = updated.Stock;
            product.Description = updated.Description;
            product.CategoryId = updated.CategoryId;

            if (image != null)
            {
                var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");

                if (!Directory.Exists(imagesFolder))
                    Directory.CreateDirectory(imagesFolder);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
                var filePath = Path.Combine(imagesFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await image.CopyToAsync(stream);

                product.ImageUrl = "/images/" + fileName;
            }

            await _context.SaveChangesAsync();

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