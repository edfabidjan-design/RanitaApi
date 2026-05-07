using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.Models;
using RanitaApi.Services;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrdersController(AppDbContext context)
        {
            _context = context;
        }

        // ✅ GET ALL (admin)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _context.Orders
                .Include(o => o.Items)
                .Include(o => o.Client)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var result = orders.Select(o => new
            {
                o.Id,
                o.CustomerName,
                o.CustomerPhone,
                o.CustomerAddress,
                o.PaymentMethod,
                o.Total,
                o.Status,
                o.CreatedAt,
                o.ClientId,
                Client = o.Client == null ? null : new { o.Client.Id, o.Client.FullName, o.Client.Email },
                Items = o.Items.Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    i.ProductName,
                    i.Price,
                    i.Quantity,
                    i.ImageUrl,
                    i.VariantId,
                    i.VariantName  // ✅ ajout
                })
            });

            return Ok(result);
        }

        // ✅ GET BY ID
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var o = await _context.Orders
                .Include(o => o.Items)
                .Include(o => o.Client)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (o == null) return NotFound();

            return Ok(new
            {
                o.Id,
                o.CustomerName,
                o.CustomerPhone,
                o.CustomerAddress,
                o.PaymentMethod,
                o.Total,
                o.Status,
                o.CreatedAt,
                o.ClientId,
                Items = o.Items.Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    i.ProductName,
                    i.Price,
                    i.Quantity,
                    i.ImageUrl,
                    i.VariantId,
                    i.VariantName  // ✅ ajout
                })
            });
        }

        // ✅ CREATE
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
                return BadRequest("Panier vide.");

            var order = new Order
            {
                CustomerName = dto.CustomerName,
                CustomerPhone = dto.CustomerPhone,
                CustomerAddress = dto.CustomerAddress,
                PaymentMethod = dto.PaymentMethod,
                ClientId = dto.ClientId,
                Status = "En attente",
                CreatedAt = DateTime.UtcNow
            };

            decimal total = 0;

            foreach (var item in dto.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null) continue;

                var price = product.Price;
                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = price,
                    Quantity = item.Quantity,
                    ImageUrl = product.ImageUrl,
                    VariantId = item.VariantId,
                    VariantName = item.VariantName
                };

                total += price * item.Quantity;
                order.Items.Add(orderItem);

                // ✅ Déduire le stock
                if (item.VariantId.HasValue)
                {
                    var variant = await _context.ProductVariants.FindAsync(item.VariantId.Value);
                    if (variant != null)
                        variant.Stock = Math.Max(0, variant.Stock - item.Quantity);
                }
                else
                {
                    product.Stock = Math.Max(0, product.Stock - item.Quantity);
                }

            }

            // Ajouter frais de livraison
            total += dto.ShippingFee;
            order.Total = total;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Email notification admin
            try
            {
                var emailService = HttpContext.RequestServices.GetService<EmailService>();
                if (emailService != null)
                    await emailService.SendNewOrderNotificationAsync(
                        order.Id, order.CustomerName, order.CustomerPhone,
                        order.CustomerAddress, order.Total);
            }
            catch (Exception ex) { Console.WriteLine("EMAIL ERROR: " + ex.Message); }


            return Ok(new { order.Id, order.Total, order.Status });
        }

        // ✅ UPDATE STATUS (admin)
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            var ancienStatut = order.Status;
            order.Status = dto.Status;

            // ✅ Si on annule → remettre le stock
            if (dto.Status == "Annulée" && ancienStatut != "Annulée")
            {
                foreach (var item in order.Items)
                {
                    if (item.VariantId.HasValue)
                    {
                        var variant = await _context.ProductVariants.FindAsync(item.VariantId.Value);
                        if (variant != null)
                            variant.Stock += item.Quantity;
                    }
                    else
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null)
                            product.Stock += item.Quantity;
                    }
                }
            }

            // ✅ Si on remet en attente/validée depuis annulée → déduire le stock
            if (ancienStatut == "Annulée" && dto.Status != "Annulée")
            {
                foreach (var item in order.Items)
                {
                    if (item.VariantId.HasValue)
                    {
                        var variant = await _context.ProductVariants.FindAsync(item.VariantId.Value);
                        if (variant != null)
                            variant.Stock = Math.Max(0, variant.Stock - item.Quantity);
                    }
                    else
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null)
                            product.Stock = Math.Max(0, product.Stock - item.Quantity);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { order.Id, order.Status });
        }

        // ✅ ANNULER commande (client)
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            if (order.Status != "En attente")
                return BadRequest("Seules les commandes en attente peuvent être annulées.");

            order.Status = "Annulée";

            // ✅ Remettre le stock
            foreach (var item in order.Items)
            {
                if (item.VariantId.HasValue)
                {
                    var variant = await _context.ProductVariants.FindAsync(item.VariantId.Value);
                    if (variant != null)
                        variant.Stock += item.Quantity;
                }
                else
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                        product.Stock += item.Quantity;
                }
            }

            await _context.SaveChangesAsync();
            return Ok("Commande annulée");
        }
    }

    // DTOs
    public class CreateOrderDto
    {
        public string CustomerName { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public string CustomerAddress { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public int? ClientId { get; set; }
        public decimal ShippingFee { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int? VariantId { get; set; }
        public string? VariantName { get; set; }

    }

    public class UpdateStatusDto
    {
        public string Status { get; set; } = "";
    }
}