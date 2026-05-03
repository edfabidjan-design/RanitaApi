using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.Models;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ Créer une commande
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] Order order)
        {
            if (order.Items == null || !order.Items.Any())
                return BadRequest("Panier vide");

            // 🔥 recalcul du total sécurisé côté serveur
            decimal total = 0;

            foreach (var item in order.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                    return BadRequest($"Produit {item.ProductId} introuvable");

                if (product.Stock < item.Quantity)
                    return BadRequest($"Stock insuffisant pour {product.Name}");

                product.Stock -= item.Quantity;

                item.Price = product.Price;
                item.ProductName = product.Name;
                item.ImageUrl = product.ImageUrl ?? "";

                total += item.Price * item.Quantity;
            }

            order.Total = total;
            order.Status = "En attente";
            order.CreatedAt = DateTime.UtcNow;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                order.Id,
                order.CustomerName,
                order.CustomerPhone,
                order.CustomerAddress,
                order.PaymentMethod,
                order.Total,
                order.Status,
                order.CreatedAt,
                order.Items
            });
        }

        // ✅ Liste des commandes (admin)
        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return Ok(orders);
        }

        // ✅ Détail d'une commande
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            return Ok(order);
        }

        // ✅ Changer statut
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
                return NotFound();

            order.Status = status;

            await _context.SaveChangesAsync();

            return Ok(order);
        }
    }
}