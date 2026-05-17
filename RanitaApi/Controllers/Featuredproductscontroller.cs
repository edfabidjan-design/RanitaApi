using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/featured-products")]
    public class FeaturedProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public FeaturedProductsController(AppDbContext context) => _context = context;

        // GET /api/featured-products
        // Retourne les SellerProducts phares depuis les IDs sauvegardés dans SiteSettings
        [HttpGet]
        public async Task<IActionResult> GetFeatured()
        {
            try
            {
                var setting = await _context.SiteSettings
                    .FirstOrDefaultAsync(s => s.Key == "featured_product_ids");

                if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
                    return Ok(new List<object>());

                var ids = setting.Value
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x.Trim(), out var id) ? id : 0)
                    .Where(id => id > 0)
                    .Take(20)
                    .ToList();

                if (!ids.Any()) return Ok(new List<object>());

                var products = await _context.SellerProducts
                    .Include(sp => sp.Seller)
                    .Where(sp => ids.Contains(sp.Id) && sp.ApprovalStatus == "Approved")
                    .ToListAsync();

                // Conserver l'ordre défini par l'admin
                var ordered = ids
                    .Select(id => products.FirstOrDefault(p => p.Id == id))
                    .Where(p => p != null)
                    .Select(p => new {
                        id = p!.Id,
                        name = p.Name,
                        price = p.Price,
                        oldPrice = p.OldPrice,
                        images = p.Images,
                        category = p.Category,
                        shopName = p.Seller?.ShopName ?? "",
                        shopLogoUrl = p.Seller?.ShopLogoUrl ?? "",
                        sellerId = p.SellerId,
                        stock = p.Stock,
                        shortDesc = p.ShortDescription ?? ""
                    })
                    .ToList();

                return Ok(ordered);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET /api/featured-products/sellers
        // Retourne la liste des boutiques approuvées pour le sélecteur admin
        [HttpGet("sellers")]
        public async Task<IActionResult> GetSellers()
        {
            try
            {
                var sellers = await _context.Sellers
                    .Where(s => s.Status == "Approved")
                    .Select(s => new {
                        id = s.Id,
                        shopName = s.ShopName,
                        shopLogoUrl = s.ShopLogoUrl ?? "",
                        productCount = _context.SellerProducts
                            .Count(sp => sp.SellerId == s.Id && sp.ApprovalStatus == "Approved")
                    })
                    .OrderBy(s => s.shopName)
                    .ToListAsync();

                return Ok(sellers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // GET /api/featured-products/sellers/{sellerId}/products
        // Retourne les produits approuvés d'une boutique
        [HttpGet("sellers/{sellerId}/products")]
        public async Task<IActionResult> GetSellerProducts(int sellerId)
        {
            try
            {
                var products = await _context.SellerProducts
                    .Where(sp => sp.SellerId == sellerId && sp.ApprovalStatus == "Approved")
                    .OrderBy(sp => sp.Name)
                    .Select(sp => new {
                        id = sp.Id,
                        name = sp.Name,
                        price = sp.Price,
                        oldPrice = sp.OldPrice,
                        images = sp.Images,
                        category = sp.Category,
                        stock = sp.Stock
                    })
                    .ToListAsync();

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}