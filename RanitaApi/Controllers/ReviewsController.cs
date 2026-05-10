using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.Models;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/reviews")]
    public class ReviewsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReviewsController(AppDbContext context)
        {
            _context = context;
        }

        // GET — avis d'un produit
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetByProduct(int productId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.Client)
                .Where(r => r.ProductId == productId && r.Approuve)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new {
                    r.Id,
                    r.ProductId,
                    r.Note,
                    r.Commentaire,
                    r.CreatedAt,
                    Client = r.Client == null ? "Anonyme" : r.Client.FullName
                })
                .ToListAsync();

            return Ok(reviews);
        }

        // POST — soumettre un avis
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        {
            // Vérifier que le client a bien commandé ce produit et que c'est livré
            var commande = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o =>
                    o.ClientId == dto.ClientId &&
                    o.Id == dto.OrderId &&
                    o.Status == "Livrée" &&
                    o.Items.Any(i => i.ProductId == dto.ProductId));

            if (commande == null)
                return BadRequest("Vous ne pouvez pas laisser un avis pour ce produit.");

            // Vérifier qu'il n'a pas déjà laissé un avis
            var dejaAvis = await _context.Reviews.AnyAsync(r =>
                r.ClientId == dto.ClientId &&
                r.ProductId == dto.ProductId &&
                r.OrderId == dto.OrderId);

            if (dejaAvis)
                return BadRequest("Vous avez déjà laissé un avis pour ce produit.");

            if (dto.Note < 1 || dto.Note > 5)
                return BadRequest("La note doit être entre 1 et 5.");

            var review = new Review
            {
                ProductId = dto.ProductId,
                ClientId = dto.ClientId,
                OrderId = dto.OrderId,
                Note = dto.Note,
                Commentaire = dto.Commentaire,
                Approuve = true
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Avis enregistré, merci !" });
        }

        // GET — tous les avis (admin)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var reviews = await _context.Reviews
                .Include(r => r.Client)
                .Include(r => r.Product)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new {
                    r.Id,
                    r.Note,
                    r.Commentaire,
                    r.Approuve,
                    r.CreatedAt,
                    Client = r.Client == null ? "Anonyme" : r.Client.FullName,
                    Produit = r.Product == null ? "—" : r.Product.Name
                })
                .ToListAsync();

            return Ok(reviews);
        }

        // DELETE — admin supprime un avis
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null) return NotFound();
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return Ok("Avis supprimé");
        }
    }

    public class CreateReviewDto
    {
        public int ProductId { get; set; }
        public int ClientId { get; set; }
        public int OrderId { get; set; }
        public int Note { get; set; }
        public string Commentaire { get; set; } = "";
    }
}