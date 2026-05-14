using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.DTOs;
using RanitaApi.Models;
using RanitaApi.Services;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SellersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly EmailService _emailService;

        public SellersController(AppDbContext db, EmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        // POST /api/sellers/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] SellerRegisterDto dto, [FromQuery] int clientId)
        {
            var client = await _db.Clients.FindAsync(clientId);
            if (client == null) return NotFound(new { message = "Client introuvable" });

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

            try
            {
                var adminSubs = await _db.PushSubscriptions.ToListAsync();
                var vapidPublicKey = "BK0OMo2QWE4SuKh0RTa6yvHfpkBXcPzL5sZkaJe3nNLesXQjRDhMzyimA8UNBCGvB9AOYpv_Q0RQrmgmA9YdNdY";
                var vapidPrivateKey = "lBGZ5H6iym-tYNbvfp-XOhNIFhDbdLO1Qjq6WqtBVLs";
                var pushAdmin = new WebPush.WebPushClient();
                var vapidAdmin = new WebPush.VapidDetails("mailto:contact@ranita-shop.com", vapidPublicKey, vapidPrivateKey);
                var payloadAdmin = System.Text.Json.JsonSerializer.Serialize(new
                {
                    title = "🏪 Nouveau vendeur !",
                    body = $"\"{dto.ShopName}\" vient de s'inscrire sur Ranita Market."
                });
                foreach (var s in adminSubs)
                {
                    try { await pushAdmin.SendNotificationAsync(new WebPush.PushSubscription(s.Endpoint, s.P256dh, s.Auth), payloadAdmin, vapidAdmin); }
                    catch { }
                }
            }
            catch { }

            return Ok(new { message = "Demande envoyée, en attente de validation", sellerId = seller.Id });
        }

        // GET /api/sellers/my?clientId=5
        [HttpGet("my")]
        public async Task<IActionResult> GetMySeller([FromQuery] int clientId)
        {
            var seller = await _db.Sellers
                .Include(s => s.Client)
                .Include(s => s.SellerProducts)
                .Include(s => s.Payouts)
                .FirstOrDefaultAsync(s => s.ClientId == clientId);
            if (seller == null) return NotFound(new { message = "Aucune boutique trouvée" });
            return Ok(MapToDto(seller));
        }

        // GET /api/sellers/{sellerId}/products
        [HttpGet("{sellerId}/products")]
        public async Task<IActionResult> GetMyProducts(int sellerId)
        {
            var products = await _db.SellerProducts
                .Where(p => p.SellerId == sellerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var productIds = products.Where(p => p.ProductId.HasValue).Select(p => p.ProductId.Value).ToList();
            var mainProducts = await _db.Products.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

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
            if (seller == null) return NotFound(new { message = "Boutique introuvable" });
            if (seller.Status != "Approved") return BadRequest(new { message = "Votre boutique doit être approuvée" });

            var name = Request.Form["name"].ToString().Trim();
            var desc = Request.Form["description"].ToString().Trim();
            var shortDesc = Request.Form["shortDescription"].ToString().Trim();
            var category = Request.Form["category"].ToString().Trim();
            var sku = Request.Form["sku"].ToString().Trim();
            var brand = Request.Form["brand"].ToString().Trim();
            var imagesJson = Request.Form["images"].ToString();

            decimal.TryParse(Request.Form["price"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal price);
            decimal.TryParse(Request.Form["oldPrice"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal oldPrice);
            int.TryParse(Request.Form["stock"], out int stock);

            var imageUrls = new List<string>();
            if (!string.IsNullOrEmpty(imagesJson))
            {
                try { var ex = System.Text.Json.JsonSerializer.Deserialize<List<string>>(imagesJson); if (ex != null) imageUrls.AddRange(ex); }
                catch { }
            }

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

            try
            {
                var adminSubs = await _db.PushSubscriptions.ToListAsync();
                var vapidPublicKey = "BK0OMo2QWE4SuKh0RTa6yvHfpkBXcPzL5sZkaJe3nNLesXQjRDhMzyimA8UNBCGvB9AOYpv_Q0RQrmgmA9YdNdY";
                var vapidPrivateKey = "lBGZ5H6iym-tYNbvfp-XOhNIFhDbdLO1Qjq6WqtBVLs";
                var pushAdmin = new WebPush.WebPushClient();
                var vapidAdmin = new WebPush.VapidDetails("mailto:contact@ranita-shop.com", vapidPublicKey, vapidPrivateKey);
                var payloadAdmin = System.Text.Json.JsonSerializer.Serialize(new
                {
                    title = "📦 Nouveau produit à valider !",
                    body = $"\"{name}\" soumis par {seller.ShopName} — en attente de validation."
                });
                foreach (var s in adminSubs)
                {
                    try { await pushAdmin.SendNotificationAsync(new WebPush.PushSubscription(s.Endpoint, s.P256dh, s.Auth), payloadAdmin, vapidAdmin); }
                    catch { }
                }
            }
            catch { }

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
                            if (v.TryGetProperty("stock", out var sp) && int.TryParse(sp.ToString(), out int vs))
                                totalStock += vs;
                        }
                        product.Stock = totalStock;
                        product.Variants = variantsJson;
                        await _db.SaveChangesAsync();
                    }
                }
                catch { }
            }

            return Ok(new { message = "Produit soumis, en attente de validation", productId = product.Id });
        }

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

        // ✅ PUT /api/sellers/payouts/{payoutId}/pay
        // Marquer un payout comme payé → envoie email au vendeur
        [HttpPut("payouts/{payoutId}/pay")]
        public async Task<IActionResult> MarkPayoutPaid(int payoutId, [FromBody] MarkPayoutPaidDto dto)
        {
            var payout = await _db.SellerPayouts
                .Include(p => p.Seller)
                    .ThenInclude(s => s.Client)
                .FirstOrDefaultAsync(p => p.Id == payoutId);

            if (payout == null) return NotFound(new { message = "Payout introuvable" });
            if (payout.Status == "Paid") return BadRequest(new { message = "Déjà payé" });

            payout.Status = "Paid";
            payout.PaidAt = DateTime.UtcNow;
            payout.TransactionReference = dto.TransactionReference;

            await _db.SaveChangesAsync();

            // ✅ Email vendeur — paiement reçu
            var sellerEmail = payout.Seller?.Client?.Email;
            if (!string.IsNullOrEmpty(sellerEmail))
            {
                try
                {
                    await _emailService.SendPayoutToSellerAsync(
                        sellerEmail,
                        payout.Seller!.ShopName,
                        payout.OrderId,
                        payout.NetAmount,
                        payout.Seller.PaymentMethod,
                        payout.Seller.PaymentDetails
                    );
                }
                catch (Exception ex) { Console.WriteLine("EMAIL PAYOUT ERROR: " + ex.Message); }
            }

            // ✅ Push vendeur — paiement reçu
            try
            {
                var vendorSubs = await _db.SellerPushSubscriptions
                    .Where(s => s.SellerId == payout.SellerId).ToListAsync();
                var pushV = new WebPush.WebPushClient();
                var vapid = new WebPush.VapidDetails("mailto:contact@ranita-shop.com",
                    "BK0OMo2QWE4SuKh0RTa6yvHfpkBXcPzL5sZkaJe3nNLesXQjRDhMzyimA8UNBCGvB9AOYpv_Q0RQrmgmA9YdNdY",
                    "lBGZ5H6iym-tYNbvfp-XOhNIFhDbdLO1Qjq6WqtBVLs");
                var payloadV = System.Text.Json.JsonSerializer.Serialize(new
                {
                    title = "💸 Paiement reçu !",
                    body = $"{payout.NetAmount.ToString("N0")} FCFA vient d'être envoyé sur votre compte."
                });
                foreach (var s in vendorSubs)
                {
                    try { await pushV.SendNotificationAsync(new WebPush.PushSubscription(s.Endpoint, s.P256dh, s.Auth), payloadV, vapid); }
                    catch { }
                }
            }
            catch (Exception ex) { Console.WriteLine("PUSH PAYOUT ERROR: " + ex.Message); }

            return Ok(new { message = "Paiement enregistré ✓", payoutId, netAmount = payout.NetAmount });
        }

        // DELETE /api/sellers/{sellerId}/products/{productId}
        [HttpDelete("{sellerId}/products/{productId}")]
        public async Task<IActionResult> DeleteProduct(int sellerId, int productId)
        {
            var product = await _db.SellerProducts.FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == sellerId);
            if (product == null) return NotFound(new { message = "Produit introuvable" });
            if (product.ApprovalStatus == "Approved") return BadRequest(new { message = "Impossible de supprimer un produit déjà publié" });
            _db.SellerProducts.Remove(product);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Produit supprimé" });
        }

        // PUT /api/sellers/{sellerId}/products/{productId}
        [HttpPut("{sellerId}/products/{productId}")]
        public async Task<IActionResult> UpdateProduct(int sellerId, int productId)
        {
            var product = await _db.SellerProducts.FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == sellerId);
            if (product == null) return NotFound(new { message = "Produit introuvable" });

            var name = Request.Form["name"].ToString().Trim();
            var desc = Request.Form["description"].ToString().Trim();
            var shortDesc = Request.Form["shortDescription"].ToString().Trim();
            var category = Request.Form["category"].ToString().Trim();
            var sku = Request.Form["sku"].ToString().Trim();
            var brand = Request.Form["brand"].ToString().Trim();
            var imagesJson = Request.Form["images"].ToString();

            decimal.TryParse(Request.Form["price"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal price);
            decimal.TryParse(Request.Form["oldPrice"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal oldPrice);
            int.TryParse(Request.Form["stock"], out int stock);

            var imageUrls = new List<string>();
            if (!string.IsNullOrEmpty(imagesJson))
            {
                try { var ex = System.Text.Json.JsonSerializer.Deserialize<List<string>>(imagesJson); if (ex != null) imageUrls.AddRange(ex); }
                catch { }
            }

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

            product.Name = name; product.Description = desc; product.ShortDescription = shortDesc;
            product.Brand = brand; product.Sku = sku; product.Price = price;
            product.OldPrice = oldPrice > 0 ? oldPrice : null; product.Stock = stock;
            product.Category = category;
            product.Images = System.Text.Json.JsonSerializer.Serialize(imageUrls);
            product.ApprovalStatus = "Pending";
            product.UpdatedAt = DateTime.UtcNow;

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
                            if (v.TryGetProperty("stock", out var sp) && int.TryParse(sp.ToString(), out int vs))
                                totalStock += vs;
                        }
                        product.Stock = totalStock;
                        product.Variants = variantsJson;
                    }
                }
                catch { }
            }

            await _db.SaveChangesAsync();

            try
            {
                var adminSubs = await _db.PushSubscriptions.ToListAsync();
                var sellerInfo = await _db.Sellers.FindAsync(sellerId);
                var pushAdmin = new WebPush.WebPushClient();
                var vapidAdmin = new WebPush.VapidDetails("mailto:contact@ranita-shop.com",
                    "BK0OMo2QWE4SuKh0RTa6yvHfpkBXcPzL5sZkaJe3nNLesXQjRDhMzyimA8UNBCGvB9AOYpv_Q0RQrmgmA9YdNdY",
                    "lBGZ5H6iym-tYNbvfp-XOhNIFhDbdLO1Qjq6WqtBVLs");
                var payloadAdmin = System.Text.Json.JsonSerializer.Serialize(new
                {
                    title = "✏️ Produit modifié à re-valider !",
                    body = $"\"{product.Name}\" modifié par {sellerInfo?.ShopName ?? "un vendeur"} — en attente de validation."
                });
                foreach (var s in adminSubs)
                {
                    try { await pushAdmin.SendNotificationAsync(new WebPush.PushSubscription(s.Endpoint, s.P256dh, s.Auth), payloadAdmin, vapidAdmin); }
                    catch { }
                }
            }
            catch { }

            return Ok(new { message = "Produit mis à jour", productId = product.Id });
        }

        [HttpPut("{sellerId}/products/{productId}/toggle")]
        public async Task<IActionResult> ToggleProduct(int sellerId, int productId, [FromBody] JsonElement body)
        {
            var sellerProduct = await _db.SellerProducts.FirstOrDefaultAsync(p => p.Id == productId && p.SellerId == sellerId);
            if (sellerProduct == null) return NotFound();
            if (sellerProduct.ProductId == null) return BadRequest(new { message = "Produit pas encore publié" });
            var product = await _db.Products.FindAsync(sellerProduct.ProductId);
            if (product == null) return NotFound();
            bool isActive = body.GetProperty("isActive").GetBoolean();
            product.IsActive = isActive;
            await _db.SaveChangesAsync();
            return Ok(new { message = isActive ? "Produit activé" : "Produit désactivé", isActive });
        }

        // GET /api/sellers/{sellerId}/orders
        [HttpGet("{sellerId}/orders")]
        public async Task<IActionResult> GetMyOrders(int sellerId)
        {
            var sellerProductIds = await _db.SellerProducts
                .Where(p => p.SellerId == sellerId && p.ApprovalStatus == "Approved" && p.ProductId != null)
                .Select(p => p.ProductId.Value).ToListAsync();
            if (!sellerProductIds.Any()) return Ok(new List<object>());
            var orders = await _db.Orders
                .Include(o => o.Items)
                .Where(o => o.Items.Any(i => sellerProductIds.Contains(i.ProductId)))
                .OrderByDescending(o => o.CreatedAt).ToListAsync();
            var result = orders.Select(o => new
            {
                o.Id,
                o.CustomerName,
                o.CustomerPhone,
                o.CustomerAddress,
                o.Total,
                o.Status,
                o.CreatedAt,
                Items = o.Items.Where(i => sellerProductIds.Contains(i.ProductId)).Select(i => new
                {
                    i.ProductId,
                    i.ProductName,
                    i.Price,
                    i.Quantity,
                    i.ImageUrl,
                    i.VariantName,
                    Subtotal = i.Price * i.Quantity
                }),
                SellerTotal = o.Items.Where(i => sellerProductIds.Contains(i.ProductId)).Sum(i => i.Price * i.Quantity)
            });
            return Ok(result);
        }

        // PUT /api/sellers/{sellerId}/profile
        [HttpPut("{sellerId}/profile")]
        public async Task<IActionResult> UpdateProfile(int sellerId, [FromBody] UpdateSellerProfileDto dto)
        {
            var seller = await _db.Sellers.FindAsync(sellerId);
            if (seller == null) return NotFound(new { message = "Boutique introuvable" });
            seller.ShopName = dto.ShopName?.Trim() ?? seller.ShopName;
            seller.ShopDescription = dto.ShopDescription?.Trim();
            seller.PhoneNumber = dto.PhoneNumber?.Trim() ?? seller.PhoneNumber;
            seller.PaymentMethod = dto.PaymentMethod ?? seller.PaymentMethod;
            seller.PaymentDetails = dto.PaymentDetails?.Trim() ?? seller.PaymentDetails;
            seller.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Profil mis à jour ✓" });
        }

        // GET /api/sellers/{sellerId}/public
        [HttpGet("{sellerId}/public")]
        public async Task<IActionResult> GetPublic(int sellerId)
        {
            var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.Id == sellerId && s.Status == "Approved");
            if (seller == null) return NotFound();
            var products = await _db.Products
                .Include(p => p.Category).Include(p => p.Variants)
                .Where(p => _db.SellerProducts.Any(sp => sp.SellerId == sellerId && sp.ProductId == p.Id && sp.ApprovalStatus == "Approved") && p.IsActive)
                .ToListAsync();
            return Ok(new
            {
                seller.Id,
                seller.ShopName,
                seller.ShopDescription,
                TotalProducts = products.Count,
                Products = products.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.OldPrice,
                    p.ImageUrl,
                    p.Images,
                    p.ShortDescription,
                    Stock = p.Variants != null && p.Variants.Any() ? p.Variants.Sum(v => v.Stock) : p.Stock,
                    Category = p.Category == null ? null : new { p.Category.Id, p.Category.Name }
                })
            });
        }

        // GET /api/sellers/product/{productId}
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetByProductId(int productId)
        {
            var sellerProduct = await _db.SellerProducts
                .Include(sp => sp.Seller)
                .FirstOrDefaultAsync(sp => sp.ProductId == productId && sp.ApprovalStatus == "Approved");
            if (sellerProduct?.Seller == null) return NotFound();
            return Ok(new { SellerId = sellerProduct.SellerId, ShopName = sellerProduct.Seller.ShopName });
        }

        // POST /api/sellers/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] SellerLoginDto dto)
        {
            var client = await _db.Clients.FirstOrDefaultAsync(c => c.Email == dto.Email);
            if (client == null) return Unauthorized(new { message = "Email ou mot de passe incorrect" });

            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(dto.Password);
            var hash = sha.ComputeHash(bytes);
            var hashedPassword = Convert.ToBase64String(hash);
            if (client.PasswordHash != hashedPassword)
                return Unauthorized(new { message = "Email ou mot de passe incorrect" });

            var seller = await _db.Sellers.FirstOrDefaultAsync(s => s.ClientId == client.Id);
            if (seller == null) return NotFound(new { message = "Aucune boutique trouvée pour ce compte" });

            return Ok(new
            {
                clientId = client.Id,
                sellerId = seller.Id,
                shopName = seller.ShopName,
                status = seller.Status,
                name = client.FullName,
                email = client.Email
            });
        }

        // POST /api/sellers/push-subscribe
        [HttpPost("push-subscribe")]
        public async Task<IActionResult> PushSubscribe([FromBody] SellerPushSubDto dto)
        {
            var old = await _db.SellerPushSubscriptions.Where(s => s.SellerId == dto.SellerId).ToListAsync();
            _db.SellerPushSubscriptions.RemoveRange(old);
            _db.SellerPushSubscriptions.Add(new SellerPushSubscription
            {
                SellerId = dto.SellerId,
                Endpoint = dto.Endpoint,
                P256dh = dto.P256dh,
                Auth = dto.Auth,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            return Ok(new { message = "Push vendeur enregistré" });
        }

        // ── HELPERS ──
        private static SellerDto MapToDto(Seller s) => new SellerDto
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

    // DTO pour marquer un payout comme payé
    public class MarkPayoutPaidDto
    {
        public string? TransactionReference { get; set; }
    }
}