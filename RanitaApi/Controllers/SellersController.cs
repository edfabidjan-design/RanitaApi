using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.DTOs;
using RanitaApi.Models;
using System.Text.Json;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SellersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SellersController(AppDbContext db)
        {
            _db = db;
        }

        // ── INSCRIPTION VENDEUR ───────────────────────────────────────────
        // POST /api/sellers/register
        // Le client doit être connecté — on passe son ClientId dans le body
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] SellerRegisterDto dto, [FromQuery] int clientId)
        {
            var client = await _db.Clients.FindAsync(clientId);
            if (client == null)
                return NotFound(new { message = "Client introuvable" });

            // Un client ne peut avoir qu'une seule boutique
            var existing = await _db.Sellers.FirstOrDefaultAsync(s => s.ClientId == clientId);
            if (existing != null)
                return BadRequest(new { message = "Vous avez déjà soumis une demande de boutique", status = existing.Status });

            var seller = new Seller
            {
                ClientId = clientId,
                ShopName = dto.ShopName.Trim(),
                ShopDescription = dto.ShopDescription?.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                NationalIdNumber = dto.NationalIdNumber.Trim(),
                PaymentMethod = dto.PaymentMethod,
                PaymentDetails = dto.PaymentDetails,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Sellers.Add(seller);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Demande envoyée, en attente de validation", sellerId = seller.Id });
        }

        // ── STATUT VENDEUR (pour le client) ──────────────────────────────
        // GET /api/sellers/my?clientId=5
        [HttpGet("my")]
        public async Task<IActionResult> GetMySeller([FromQuery] int clientId)
        {
            var seller = await _db.Sellers
                .Include(s => s.Client)
                .Include(s => s.SellerProducts)
                .Include(s => s.Payouts)
                .FirstOrDefaultAsync(s => s.ClientId == clientId);

            if (seller == null)
                return NotFound(new { message = "Aucune boutique trouvée" });

            var dto = MapToDto(seller);
            return Ok(dto);
        }

        // ── PRODUITS DU VENDEUR ───────────────────────────────────────────
        // GET /api/sellers/{sellerId}/products
        [HttpGet("{sellerId}/products")]
        public async Task<IActionResult> GetMyProducts(int sellerId)
        {
            var products = await _db.SellerProducts
                .Where(p => p.SellerId == sellerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var productIds = products
                .Where(p => p.ProductId.HasValue)
                .Select(p => p.ProductId.Value).ToList();

            var mainProducts = await _db.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var result = products.Select(p => {
                List<string> images;
                try { images = JsonSerializer.Deserialize<List<string>>(p.Images) ?? new(); }
                catch { images = new(); }

                bool? isActive = null;
                if (p.ProductId.HasValue && mainProducts.ContainsKey(p.ProductId.Value))
                    isActive = mainProducts[p.ProductId.Value].IsActive;

                return new
                {
                    p.Id,
                    p.SellerId,
                    p.ProductId,
                    p.Name,
                    p.Description,
                    p.ShortDescription,
                    p.Price,
                    p.OldPrice,
                    p.Stock,
                    p.Category,
                    p.Sku,
                    p.Brand,
                    Images = images,
                    p.ApprovalStatus,
                    p.RejectionReason,
                    p.CreatedAt,
                    IsActive = isActive
                };
            });

            return Ok(result);
        }

        // POST /api/sellers/{sellerId}/products
        [HttpPost("{sellerId}/products")]
        public async Task<IActionResult> SubmitProduct(int sellerId)
        {
            var seller = await _db.Sellers.FindAsync(sellerId);
            if (seller == null)
                return NotFound(new { message = "Boutique introuvable" });

            if (seller.Status != "Approved")
                return BadRequest(new { message = "Votre boutique doit être approuvée" });

            // Lire depuis Request.Form directement
            var name = Request.Form["name"].ToString().Trim();
            var desc = Request.Form["description"].ToString().Trim();
            var shortDesc = Request.Form["shortDescription"].ToString().Trim();
            var category = Request.Form["category"].ToString().Trim();
            var sku = Request.Form["sku"].ToString().Trim();
            var brand = Request.Form["brand"].ToString().Trim();
            var imagesJson = Request.Form["images"].ToString();

            decimal.TryParse(Request.Form["price"], System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal price);
            decimal.TryParse(Request.Form["oldPrice"], System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal oldPrice);
            int.TryParse(Request.Form["stock"], out int stock);

            // Images existantes
            var imageUrls = new List<string>();
            if (!string.IsNullOrEmpty(imagesJson))
            {
                try
                {
                    var existing = System.Text.Json.JsonSerializer.Deserialize<List<string>>(imagesJson);
                    if (existing != null) imageUrls.AddRange(existing);
                }
                catch { }
            }

            // Upload vers Cloudinary (même logique que ProductsController)
            if (Request.Form.Files.Count > 0)
            {
                var cloudinaryUrl = Environment.GetEnvironmentVariable("CLOUDINARY_URL");
                var cloudinary = new CloudinaryDotNet.Cloudinary(cloudinaryUrl);
                cloudinary.Api.Secure = true;

                foreach (var file in Request.Form.Files.Take(5 - imageUrls.Count))
                {
                    if (file.Length > 0)
                    {
                        await using var stream = file.OpenReadStream();
                        var uploadResult = await cloudinary.UploadAsync(new CloudinaryDotNet.Actions.ImageUploadParams
                        {
                            File = new CloudinaryDotNet.FileDescription(file.FileName, stream),
                            Folder = "ranita-products"
                        });
                        imageUrls.Add(uploadResult.SecureUrl.ToString());
                    }
                }
            }

            var product = new SellerProduct
            {
                SellerId = sellerId,
                Name = name,
                Description = desc,
                ShortDescription = shortDesc,
                Brand = brand,
                Sku = sku,
                Price = price,
                OldPrice = oldPrice > 0 ? oldPrice : null,
                Stock = stock,
                Category = category,
                Images = System.Text.Json.JsonSerializer.Serialize(imageUrls),
                ApprovalStatus = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.SellerProducts.Add(product);
            await _db.SaveChangesAsync();

            // Sauvegarder les variantes
            var variantsJson = Request.Form["variants"].ToString();
            if (!string.IsNullOrEmpty(variantsJson))
            {
                try
                {
                    var variants = System.Text.Json.JsonSerializer.Deserialize<List<dynamic>>(variantsJson);
                    if (variants != null)
                    {
                        int totalStock = 0;
                        foreach (var v in variants)
                        {
                            var stockStr = v.GetProperty("stock").ToString();
                            if (int.TryParse(stockStr, out int vs))
                                totalStock += vs;
                        }
                        product.Stock = totalStock;
                        product.Variants = variantsJson; // ← AJOUTER
                        await _db.SaveChangesAsync();
                    }
                }
                catch { }
            }

            return Ok(new { message = "Produit soumis, en attente de validation", productId = product.Id });
        }


        // ── PAYOUTS DU VENDEUR ────────────────────────────────────────────
        // GET /api/sellers/{sellerId}/payouts
        [HttpGet("{sellerId}/payouts")]
        public async Task<IActionResult> GetPayouts(int sellerId)
        {
            var payouts = await _db.SellerPayouts
                .Include(p => p.Seller)
                .Where(p => p.SellerId == sellerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var result = payouts.Select(p => new PayoutDto
            {
                Id = p.Id,
                SellerId = p.SellerId,
                ShopName = p.Seller?.ShopName ?? "",
                OrderId = p.OrderId,
                GrossAmount = p.GrossAmount,
                CommissionAmount = p.CommissionAmount,
                NetAmount = p.NetAmount,
                Status = p.Status,
                TransactionReference = p.TransactionReference,
                CreatedAt = p.CreatedAt,
                PaidAt = p.PaidAt
            }).ToList();

            return Ok(result);
        }

        // ── HELPERS ───────────────────────────────────────────────────────

        private static SellerDto MapToDto(Seller s)
        {
            return new SellerDto
            {
                Id = s.Id,
                ClientId = s.ClientId,
                ClientName = s.Client?.FullName ?? "",
                ClientEmail = s.Client?.Email ?? "",
                ShopName = s.ShopName,
                ShopDescription = s.ShopDescription,
                PhoneNumber = s.PhoneNumber,
                NationalIdNumber = s.NationalIdNumber,
                ShopLogoUrl = s.ShopLogoUrl,
                CommissionRate = s.CommissionRate,
                PaymentMethod = s.PaymentMethod,
                PaymentDetails = s.PaymentDetails,
                Status = s.Status,
                RejectionReason = s.RejectionReason,
                CreatedAt = s.CreatedAt,
                TotalProducts = s.SellerProducts.Count(p => p.ApprovalStatus == "Approved"),
                PendingProducts = s.SellerProducts.Count(p => p.ApprovalStatus == "Pending"),
                TotalEarnings = s.Payouts.Where(p => p.Status == "Paid").Sum(p => p.NetAmount),
                PendingPayouts = s.Payouts.Where(p => p.Status == "Pending").Sum(p => p.NetAmount)
            };
        }

        private static SellerProductDto MapProductToDto(SellerProduct p, string shopName)
        {
            List<string> images;
            try { images = JsonSerializer.Deserialize<List<string>>(p.Images) ?? new(); }
            catch { images = new(); }

            return new SellerProductDto
            {
                Id = p.Id,
                SellerId = p.SellerId,
                ShopName = shopName,
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                OldPrice = p.OldPrice,
                Stock = p.Stock,
                Category = p.Category,
                Sku = p.Sku,      // ← AJOUTER
                Brand = p.Brand,    // ← AJOUTER
                Images = images,
                ApprovalStatus = p.ApprovalStatus,
                RejectionReason = p.RejectionReason,
                CreatedAt = p.CreatedAt
            };
        }


        // DELETE /api/sellers/{sellerId}/products/{productId}
        [HttpDelete("{sellerId}/products/{productId}")]
        public async Task<IActionResult> DeleteProduct(int sellerId, int productId)
        {
            var product = await _db.SellerProducts
                .FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == sellerId);

            if (product == null)
                return NotFound(new { message = "Produit introuvable" });

            // Ne peut supprimer que si pas encore approuvé
            if (product.ApprovalStatus == "Approved")
                return BadRequest(new { message = "Impossible de supprimer un produit déjà publié" });

            _db.SellerProducts.Remove(product);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Produit supprimé" });
        }

        // PUT /api/sellers/{sellerId}/products/{productId}
        [HttpPut("{sellerId}/products/{productId}")]
        public async Task<IActionResult> UpdateProduct(int sellerId, int productId)
        {
            var product = await _db.SellerProducts
                .FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == sellerId);

            if (product == null)
                return NotFound(new { message = "Produit introuvable" });

            // On permet la modification même si Approuvé — repassera en Pending

            // Lire depuis Request.Form
            var name = Request.Form["name"].ToString().Trim();
            var desc = Request.Form["description"].ToString().Trim();
            var shortDesc = Request.Form["shortDescription"].ToString().Trim();
            var category = Request.Form["category"].ToString().Trim();
            var sku = Request.Form["sku"].ToString().Trim();
            var brand = Request.Form["brand"].ToString().Trim();
            var imagesJson = Request.Form["images"].ToString();

            decimal.TryParse(Request.Form["price"], System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal price);
            decimal.TryParse(Request.Form["oldPrice"], System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal oldPrice);
            int.TryParse(Request.Form["stock"], out int stock);

            // Images existantes
            var imageUrls = new List<string>();
            if (!string.IsNullOrEmpty(imagesJson))
            {
                try
                {
                    var existing = System.Text.Json.JsonSerializer.Deserialize<List<string>>(imagesJson);
                    if (existing != null) imageUrls.AddRange(existing);
                }
                catch { }
            }

            // Nouvelles images Cloudinary
            if (Request.Form.Files.Count > 0)
            {
                var cloudinaryUrl = Environment.GetEnvironmentVariable("CLOUDINARY_URL");
                var cloudinary = new CloudinaryDotNet.Cloudinary(cloudinaryUrl);
                cloudinary.Api.Secure = true;

                foreach (var file in Request.Form.Files.Take(5 - imageUrls.Count))
                {
                    if (file.Length > 0)
                    {
                        await using var stream = file.OpenReadStream();
                        var uploadResult = await cloudinary.UploadAsync(new CloudinaryDotNet.Actions.ImageUploadParams
                        {
                            File = new CloudinaryDotNet.FileDescription(file.FileName, stream),
                            Folder = "ranita-products"
                        });
                        imageUrls.Add(uploadResult.SecureUrl.ToString());
                    }
                }
            }

            // Mettre à jour
            product.Name = name;
            product.Description = desc;
            product.ShortDescription = shortDesc;
            product.Brand = brand;
            product.Sku = sku;
            product.Price = price;
            product.OldPrice = oldPrice > 0 ? oldPrice : null;
            product.Stock = stock;
            product.Category = category;
            product.Images = System.Text.Json.JsonSerializer.Serialize(imageUrls);
            product.ApprovalStatus = "Pending"; // Repassé en attente après modification
            product.UpdatedAt = DateTime.UtcNow;

            // Variantes
            var variantsJson = Request.Form["variants"].ToString();
            if (!string.IsNullOrEmpty(variantsJson))
            {
                try
                {
                    var variants = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(variantsJson);
                    if (variants != null)
                    {
                        int totalStock = 0;
                        foreach (var v in variants)
                        {
                            if (v.TryGetProperty("stock", out var stockProp) &&
                                int.TryParse(stockProp.ToString(), out int vs))
                                totalStock += vs;
                        }
                        product.Stock = totalStock;
                        product.Variants = variantsJson; // ← AJOUTER
                    }
                }
                catch { }
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "Produit mis à jour", productId = product.Id });
        }


        [HttpPut("{sellerId}/products/{productId}/toggle")]
        public async Task<IActionResult> ToggleProduct(int sellerId, int productId, [FromBody] JsonElement body)
        {
            var sellerProduct = await _db.SellerProducts
                .FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == sellerId);

            if (sellerProduct == null) return NotFound();
            if (sellerProduct.ProductId == null) return BadRequest(new { message = "Produit pas encore publié" });

            var product = await _db.Products.FindAsync(sellerProduct.ProductId);
            if (product == null) return NotFound();

            bool isActive = body.GetProperty("isActive").GetBoolean();
            product.IsActive = isActive;
            await _db.SaveChangesAsync();

            return Ok(new { message = isActive ? "Produit activé" : "Produit désactivé", isActive });
        }


    }
}
