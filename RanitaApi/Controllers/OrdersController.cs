using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanitaApi.Data;
using RanitaApi.Models;
using RanitaApi.Services;
using WebPush;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly EmailService _emailService;
        private readonly IServiceScopeFactory _scopeFactory;

        private const string VapidPublic = "BK0OMo2QWE4SuKh0RTa6yvHfpkBXcPzL5sZkaJe3nNLesXQjRDhMzyimA8UNBCGvB9AOYpv_Q0RQrmgmA9YdNdY";
        private const string VapidPrivate = "lBGZ5H6iym-tYNbvfp-XOhNIFhDbdLO1Qjq6WqtBVLs";
        private const string VapidSubject = "mailto:admin@ranita-shop.com";

        public OrdersController(AppDbContext context, EmailService emailService, IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _emailService = emailService;
            _scopeFactory = scopeFactory;
        }

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
                o.RefundMotif,
                o.ClientId,
                Client = o.Client == null ? null : new { o.Client.Id, o.Client.FullName, o.Client.Email },
                Items = o.Items.Select(i => new { i.Id, i.ProductId, i.ProductName, i.Price, i.Quantity, i.ImageUrl, i.VariantId, i.VariantName })
            });

            return Ok(result);
        }

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
                Items = o.Items.Select(i => new { i.Id, i.ProductId, i.ProductName, i.Price, i.Quantity, i.ImageUrl, i.VariantId, i.VariantName })
            });
        }

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

                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = item.Quantity,
                    ImageUrl = product.ImageUrl,
                    VariantId = item.VariantId,
                    VariantName = item.VariantName
                };

                total += product.Price * item.Quantity;
                order.Items.Add(orderItem);

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

            total += dto.ShippingFee;
            order.Total = total;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var orderId = order.Id;
            var customerName = order.CustomerName;
            var customerPhone = order.CustomerPhone;
            var customerAddress = order.CustomerAddress;
            var orderTotal = order.Total;
            var clientId = order.ClientId;
            var orderItems = order.Items.ToList();

            _ = Task.Run(async () =>
            {
                try
                {
                    // Email admin
                    await _emailService.SendNewOrderNotificationAsync(
                        orderId, customerName, customerPhone, customerAddress, orderTotal);

                    // Push notification ADMIN
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var subs = await db.PushSubscriptions.ToListAsync();
                        var pushClient = new WebPushClient();
                        var vapid = new VapidDetails(VapidSubject, VapidPublic, VapidPrivate);
                        var payload = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            title = "🛒 Nouvelle commande !",
                            body = $"{customerName} — {orderTotal.ToString("N0")} FCFA"
                        });
                        foreach (var s in subs)
                        {
                            try { var sub = new PushSubscription(s.Endpoint, s.P256dh, s.Auth); await pushClient.SendNotificationAsync(sub, payload, vapid); }
                            catch { }
                        }
                    }
                    catch (Exception ex) { Console.WriteLine("PUSH ADMIN ERROR: " + ex.Message); }


                    

                    // ✅ Push notification CLIENT à la passation de commande
                    if (clientId.HasValue)
                    {
                        try
                        {
                            using var scopeClient = _scopeFactory.CreateScope();
                            var dbClient = scopeClient.ServiceProvider.GetRequiredService<AppDbContext>();
                            var clientSubs = await dbClient.ClientPushSubscriptions
                                .Where(s => s.ClientId == clientId.Value)
                                .ToListAsync();
                            var pushClientNew = new WebPushClient();
                            var vapidNew = new VapidDetails(VapidSubject, VapidPublic, VapidPrivate);
                            var payloadNew = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                title = "✅ Commande confirmée !",
                                body = $"Votre commande #{orderId} de {orderTotal.ToString("N0")} FCFA a bien été reçue."
                            });
                            foreach (var s in clientSubs)
                            {
                                try
                                {
                                    var pushSub = new PushSubscription(s.Endpoint, s.P256dh, s.Auth);
                                    await pushClientNew.SendNotificationAsync(pushSub, payloadNew, vapidNew);
                                }
                                catch { }
                            }
                        }
                        catch (Exception ex) { Console.WriteLine("PUSH CLIENT NEW ORDER ERROR: " + ex.Message); }
                    }

                    // Email confirmation client
                    if (clientId.HasValue)



                        // Email confirmation client
                        if (clientId.HasValue)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var client = await db.Clients.FindAsync(clientId.Value);
                        if (client != null && !string.IsNullOrEmpty(client.Email))
                            await _emailService.SendOrderConfirmationToClientAsync(
                                client.Email, client.FullName, orderId, orderTotal, orderItems);
                    }
                }
                catch (Exception ex) { Console.WriteLine("EMAIL ERROR: " + ex.Message); }
            });

            return Ok(new { order.Id, order.Total, order.Status });
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            var ancienStatut = order.Status;
            order.Status = dto.Status;

            if (dto.Status == "Annulée" && ancienStatut != "Annulée")
            {
                foreach (var item in order.Items)
                {
                    if (item.VariantId.HasValue)
                    {
                        var variant = await _context.ProductVariants.FindAsync(item.VariantId.Value);
                        if (variant != null) variant.Stock += item.Quantity;
                    }
                    else
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null) product.Stock += item.Quantity;
                    }
                }
            }

            if (ancienStatut == "Annulée" && dto.Status != "Annulée")
            {
                foreach (var item in order.Items)
                {
                    if (item.VariantId.HasValue)
                    {
                        var variant = await _context.ProductVariants.FindAsync(item.VariantId.Value);
                        if (variant != null) variant.Stock = Math.Max(0, variant.Stock - item.Quantity);
                    }
                    else
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null) product.Stock = Math.Max(0, product.Stock - item.Quantity);
                    }
                }
            }

            await _context.SaveChangesAsync();


            // Échec livraison — remettre le stock
            if (dto.Status == "Échec livraison" && ancienStatut != "Échec livraison")
            {
                foreach (var item in order.Items)
                {
                    if (item.VariantId.HasValue)
                    {
                        var variant = await _context.ProductVariants.FindAsync(item.VariantId.Value);
                        if (variant != null) variant.Stock += item.Quantity;
                    }
                    else
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null) product.Stock += item.Quantity;
                    }
                }

                // Annuler le payout si créé
                var payouts = await _context.SellerPayouts
                    .Where(p => p.OrderId == order.Id && p.Status == "Pending")
                    .ToListAsync();
                _context.SellerPayouts.RemoveRange(payouts);
            }


            // Payout automatique quand commande Livrée
            if (dto.Status == "Livrée" && ancienStatut != "Livrée")
            {
                foreach (var item in order.Items)
                {
                    var sellerProduct = await _context.SellerProducts
                        .Include(sp => sp.Seller)
                        .FirstOrDefaultAsync(sp => sp.ProductId == item.ProductId && sp.ApprovalStatus == "Approved");

                    if (sellerProduct?.Seller == null) continue;

                    // Éviter les doublons
                    var alreadyExists = await _context.SellerPayouts
                        .AnyAsync(p => p.OrderId == order.Id && p.SellerId == sellerProduct.SellerId);
                    if (alreadyExists) continue;

                    var gross = item.Price * item.Quantity;
                    var commission = Math.Round(gross * sellerProduct.Seller.CommissionRate, 2);
                    var net = gross - commission;

                    _context.SellerPayouts.Add(new SellerPayout
                    {
                        SellerId = sellerProduct.SellerId,
                        OrderId = order.Id,
                        GrossAmount = gross,
                        CommissionAmount = commission,
                        NetAmount = net,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await _context.SaveChangesAsync();
            }


            var orderId = order.Id;
            var clientId = order.ClientId;
            var newStatus = dto.Status;
            var orderItems = order.Items.ToList();

            _ = Task.Run(async () =>
            {
                try
                {
                    if (clientId.HasValue)
                    {
                        // Email client
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var client = await db.Clients.FindAsync(clientId.Value);
                        if (client != null && !string.IsNullOrEmpty(client.Email))
                            await _emailService.SendOrderStatusUpdateAsync(
                                client.Email, client.FullName, orderId, newStatus, orderItems);

                        // Push notification CLIENT
                        try
                        {
                            using var scope2 = _scopeFactory.CreateScope();
                            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
                            var clientSubs = await db2.ClientPushSubscriptions
                                .Where(s => s.ClientId == clientId.Value)
                                .ToListAsync();
                            var pushClient = new WebPushClient();
                            var vapid = new VapidDetails(VapidSubject, VapidPublic, VapidPrivate);
                            var payload = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                title = "🛒 Ranita - Commande mise à jour",
                                body = $"Votre commande #{orderId} est maintenant : {newStatus}"
                            });
                            foreach (var s in clientSubs)
                            {
                                try
                                {
                                    var pushSub = new PushSubscription(s.Endpoint, s.P256dh, s.Auth);
                                    await pushClient.SendNotificationAsync(pushSub, payload, vapid);
                                }
                                catch { }
                            }
                        }
                        catch (Exception ex) { Console.WriteLine("PUSH CLIENT ERROR: " + ex.Message); }
                    }
                }
                catch (Exception ex) { Console.WriteLine("EMAIL ERROR: " + ex.Message); }
            });

            return Ok(new { order.Id, order.Status });
        }

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

            foreach (var item in order.Items)
            {
                if (item.VariantId.HasValue)
                {
                    var variant = await _context.ProductVariants.FindAsync(item.VariantId.Value);
                    if (variant != null) variant.Stock += item.Quantity;
                }
                else
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null) product.Stock += item.Quantity;
                }
            }

            await _context.SaveChangesAsync();
            return Ok("Commande annulée");
        }

        [HttpPost("{id}/refund-request")]
        public async Task<IActionResult> RefundRequest(int id, [FromBody] RefundRequestDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (order.Status != "Livrée") return BadRequest("Remboursement uniquement pour les commandes livrées.");

            order.Status = "Remboursement demandé";
            order.RefundMotif = dto.Motif;
            await _context.SaveChangesAsync();

            return Ok("Demande de remboursement envoyée.");
        }

        [HttpPost("{id}/refund-approve")]
        public async Task<IActionResult> RefundApprove(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            if (order.Status != "Remboursement demandé") return BadRequest("Statut invalide.");

            foreach (var item in order.Items)
            {
                if (item.VariantId.HasValue)
                {
                    var variant = await _context.ProductVariants.FindAsync(item.VariantId.Value);
                    if (variant != null) variant.Stock += item.Quantity;
                }
                else
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null) product.Stock += item.Quantity;
                }
            }

            order.Status = "Remboursé";
            await _context.SaveChangesAsync();
            return Ok("Remboursement validé.");
        }

        [HttpPost("{id}/refund-reject")]
        public async Task<IActionResult> RefundReject(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (order.Status != "Remboursement demandé") return BadRequest("Statut invalide.");

            order.Status = "Remboursement rejeté";
            await _context.SaveChangesAsync();
            return Ok("Remboursement rejeté.");
        }
    }

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