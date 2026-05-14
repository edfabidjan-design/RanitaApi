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
            // APRÈS
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
                    {
                        variant.Stock = Math.Max(0, variant.Stock - item.Quantity);

                        // ✅ Déduire aussi dans SellerProduct
                        var sp = await _context.SellerProducts
                            .FirstOrDefaultAsync(x => x.ProductId == product.Id && x.ApprovalStatus == "Approved");
                        if (sp != null)
                            sp.Stock = Math.Max(0, sp.Stock - item.Quantity);
                    }
                }
                else
                {
                    product.Stock = Math.Max(0, product.Stock - item.Quantity);

                    // ✅ Déduire aussi dans SellerProduct
                    var sp = await _context.SellerProducts
                        .FirstOrDefaultAsync(x => x.ProductId == product.Id && x.ApprovalStatus == "Approved");
                    if (sp != null)
                        sp.Stock = product.Stock;
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
                    // ── Email admin ──
                    await _emailService.SendNewOrderNotificationAsync(
                        orderId, customerName, customerPhone, customerAddress, orderTotal);

                    // ── Push admin ──
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
                            try { await pushClient.SendNotificationAsync(new PushSubscription(s.Endpoint, s.P256dh, s.Auth), payload, vapid); }
                            catch { }
                        }
                    }
                    catch (Exception ex) { Console.WriteLine("PUSH ADMIN ERROR: " + ex.Message); }

                    // ── Push + Email client ──
                    if (clientId.HasValue)
                    {
                        try
                        {
                            using var scopeClient = _scopeFactory.CreateScope();
                            var dbClient = scopeClient.ServiceProvider.GetRequiredService<AppDbContext>();
                            var clientSubs = await dbClient.ClientPushSubscriptions
                                .Where(s => s.ClientId == clientId.Value).ToListAsync();
                            var pushClientNew = new WebPushClient();
                            var vapidNew = new VapidDetails(VapidSubject, VapidPublic, VapidPrivate);
                            var payloadNew = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                title = "✅ Commande confirmée !",
                                body = $"Votre commande #{orderId} de {orderTotal.ToString("N0")} FCFA a bien été reçue."
                            });
                            foreach (var s in clientSubs)
                            {
                                try { await pushClientNew.SendNotificationAsync(new PushSubscription(s.Endpoint, s.P256dh, s.Auth), payloadNew, vapidNew); }
                                catch { }
                            }
                        }
                        catch (Exception ex) { Console.WriteLine("PUSH CLIENT NEW ORDER ERROR: " + ex.Message); }

                        // Email confirmation client
                        try
                        {
                            using var scope = _scopeFactory.CreateScope();
                            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                            var client = await db.Clients.FindAsync(clientId.Value);
                            if (client != null && !string.IsNullOrEmpty(client.Email))
                                await _emailService.SendOrderConfirmationToClientAsync(
                                    client.Email, client.FullName, orderId, orderTotal, orderItems);
                        }
                        catch (Exception ex) { Console.WriteLine("EMAIL CLIENT ERROR: " + ex.Message); }
                    }

                    // ── Push + Email VENDEURS concernés ──
                    try
                    {
                        using var scopeVendor = _scopeFactory.CreateScope();
                        var dbVendor = scopeVendor.ServiceProvider.GetRequiredService<AppDbContext>();

                        // Grouper par vendeur pour n'envoyer qu'un email par vendeur
                        var notifiedSellerIds = new HashSet<int>();

                        foreach (var item in orderItems)
                        {
                            var sellerProduct = await dbVendor.SellerProducts
                                .Include(sp => sp.Seller)
                                    .ThenInclude(s => s.Client)
                                .FirstOrDefaultAsync(sp => sp.ProductId == item.ProductId && sp.ApprovalStatus == "Approved");

                            if (sellerProduct?.Seller == null) continue;

                            // ── Push vendeur ──
                            var vendorSubs = await dbVendor.SellerPushSubscriptions
                                .Where(s => s.SellerId == sellerProduct.SellerId).ToListAsync();
                            var pushVendor = new WebPushClient();
                            var vapidVendor = new VapidDetails(VapidSubject, VapidPublic, VapidPrivate);
                            var payloadVendor = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                title = "🛒 Nouvelle vente !",
                                body = $"{item.ProductName} x{item.Quantity} — {(item.Price * item.Quantity).ToString("N0")} FCFA"
                            });
                            foreach (var s in vendorSubs)
                            {
                                try { await pushVendor.SendNotificationAsync(new PushSubscription(s.Endpoint, s.P256dh, s.Auth), payloadVendor, vapidVendor); }
                                catch { }
                            }

                            // ── Email vendeur (1 seul par vendeur) ──
                            if (!notifiedSellerIds.Contains(sellerProduct.SellerId))
                            {
                                notifiedSellerIds.Add(sellerProduct.SellerId);
                                var sellerEmail = sellerProduct.Seller.Client?.Email;
                                if (!string.IsNullOrEmpty(sellerEmail))
                                {
                                    // Récupérer tous les items de CE vendeur dans la commande
                                    var allSellerProductIds = await dbVendor.SellerProducts
                                        .Where(sp => sp.SellerId == sellerProduct.SellerId && sp.ApprovalStatus == "Approved" && sp.ProductId != null)
                                        .Select(sp => sp.ProductId!.Value)
                                        .ToListAsync();
                                    var sellerItems = orderItems.Where(i => allSellerProductIds.Contains(i.ProductId)).ToList();
                                    var grossTotal = sellerItems.Sum(i => i.Price * i.Quantity);
                                    var sellerNet = Math.Round(grossTotal * (1 - sellerProduct.Seller.CommissionRate), 0);

                                    try
                                    {
                                        await _emailService.SendNewOrderToSellerAsync(
                                            sellerEmail, sellerProduct.Seller.ShopName,
                                            orderId, sellerItems, sellerNet);
                                    }
                                    catch (Exception ex) { Console.WriteLine("EMAIL VENDEUR NEW ORDER ERROR: " + ex.Message); }
                                }
                            }
                        }
                    }
                    catch (Exception ex) { Console.WriteLine("PUSH VENDOR ORDER ERROR: " + ex.Message); }
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

            // Échec livraison — remettre le stock + annuler payout
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
                var payouts = await _context.SellerPayouts
                    .Where(p => p.OrderId == order.Id && p.Status == "Pending").ToListAsync();
                _context.SellerPayouts.RemoveRange(payouts);
                await _context.SaveChangesAsync();
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
                    // ── Email + Push CLIENT ──
                    if (clientId.HasValue)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var client = await db.Clients.FindAsync(clientId.Value);
                        if (client != null && !string.IsNullOrEmpty(client.Email))
                            await _emailService.SendOrderStatusUpdateAsync(
                                client.Email, client.FullName, orderId, newStatus, orderItems);

                        try
                        {
                            using var scope2 = _scopeFactory.CreateScope();
                            var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
                            var clientSubs = await db2.ClientPushSubscriptions
                                .Where(s => s.ClientId == clientId.Value).ToListAsync();
                            var pushClient = new WebPushClient();
                            var vapid = new VapidDetails(VapidSubject, VapidPublic, VapidPrivate);
                            var (notifTitle, notifBody) = newStatus switch
                            {
                                "Validée" => ("✅ Commande confirmée !", $"Votre commande #{orderId} a été confirmée et sera bientôt expédiée."),
                                "En cours" => ("🚚 Commande en route !", $"Votre commande #{orderId} est en cours de livraison."),
                                "Livrée" => ("📦 Commande livrée !", $"Votre commande #{orderId} a été livrée. Merci pour votre achat !"),
                                "Annulée" => ("❌ Commande annulée", $"Votre commande #{orderId} a été annulée."),
                                "Échec livraison" => ("⚠️ Échec de livraison", $"La livraison de votre commande #{orderId} a échoué. Nous vous recontactons."),
                                "Remboursé" => ("💸 Remboursement effectué", $"Votre remboursement pour la commande #{orderId} a été effectué."),
                                _ => ("🛒 Commande mise à jour", $"Votre commande #{orderId} est maintenant : {newStatus}")
                            };
                            var payload = System.Text.Json.JsonSerializer.Serialize(new { title = notifTitle, body = notifBody });
                            foreach (var s in clientSubs)
                            {
                                try { await pushClient.SendNotificationAsync(new PushSubscription(s.Endpoint, s.P256dh, s.Auth), payload, vapid); }
                                catch { }
                            }
                        }
                        catch (Exception ex) { Console.WriteLine("PUSH CLIENT ERROR: " + ex.Message); }
                    }
                }
                catch (Exception ex) { Console.WriteLine("EMAIL ERROR: " + ex.Message); }
            });

            // ── Push + Email VENDEUR quand commande livrée ──
            if (newStatus == "Livrée")
            {
                try
                {
                    using var scopeV = _scopeFactory.CreateScope();
                    var dbV = scopeV.ServiceProvider.GetRequiredService<AppDbContext>();
                    var sellerProductIds = orderItems.Select(i => i.ProductId).ToList();
                    var sellerProducts = await dbV.SellerProducts
                        .Include(sp => sp.Seller)
                            .ThenInclude(s => s.Client)
                        .Where(sp => sellerProductIds.Contains(sp.ProductId ?? 0) && sp.ApprovalStatus == "Approved")
                        .ToListAsync();

                    var notifiedSellers = new HashSet<int>();
                    foreach (var sp in sellerProducts)
                    {
                        if (sp.Seller == null || notifiedSellers.Contains(sp.SellerId)) continue;
                        notifiedSellers.Add(sp.SellerId);

                        // Push vendeur
                        var vendorSubs = await dbV.SellerPushSubscriptions
                            .Where(s => s.SellerId == sp.SellerId).ToListAsync();
                        var pushV = new WebPushClient();
                        var vapidV = new VapidDetails(VapidSubject, VapidPublic, VapidPrivate);
                        var payloadV = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            title = "💰 Paiement en cours !",
                            body = $"Commande #{orderId} livrée — votre paiement est en cours de traitement."
                        });
                        foreach (var s in vendorSubs)
                        {
                            try { await pushV.SendNotificationAsync(new PushSubscription(s.Endpoint, s.P256dh, s.Auth), payloadV, vapidV); }
                            catch { }
                        }

                        // ✅ Email vendeur — commande livrée
                        var sellerEmail = sp.Seller.Client?.Email;
                        if (!string.IsNullOrEmpty(sellerEmail))
                        {
                            var allSellerProductIds = await dbV.SellerProducts
                                .Where(x => x.SellerId == sp.SellerId && x.ApprovalStatus == "Approved" && x.ProductId != null)
                                .Select(x => x.ProductId!.Value).ToListAsync();
                            var sellerItemsTotal = orderItems
                                .Where(i => allSellerProductIds.Contains(i.ProductId))
                                .Sum(i => i.Price * i.Quantity);
                            var netAmount = Math.Round(sellerItemsTotal * (1 - sp.Seller.CommissionRate), 0);
                            try
                            {
                                await _emailService.SendOrderDeliveredToSellerAsync(
                                    sellerEmail, sp.Seller.ShopName, orderId, netAmount);
                            }
                            catch (Exception ex) { Console.WriteLine("EMAIL VENDEUR LIVREE ERROR: " + ex.Message); }
                        }
                    }
                }
                catch (Exception ex) { Console.WriteLine("PUSH VENDOR DELIVERED ERROR: " + ex.Message); }
            }

            return Ok(new { order.Id, order.Status });
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            if (order.Status != "En attente") return BadRequest("Seules les commandes en attente peuvent être annulées.");
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
            var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
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



        // PUT /api/orders/{id}/vendor-confirm
        [HttpPut("{id}/vendor-confirm")]
        public async Task<IActionResult> VendorConfirm(int id, [FromBody] VendorConfirmDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            if (order.Status != "En attente")
                return BadRequest(new { message = "Seules les commandes en attente peuvent être confirmées" });

            order.Status = dto.Available ? "Confirmée par vendeur" : "Annulée";
            await _context.SaveChangesAsync();

            // Push admin
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var subs = await db.PushSubscriptions.ToListAsync();
                    var pushClient = new WebPushClient();
                    var vapid = new VapidDetails(VapidSubject, VapidPublic, VapidPrivate);
                    var payload = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        title = dto.Available ? "✅ Vendeur a confirmé !" : "❌ Produit indisponible !",
                        body = dto.Available
                            ? $"Commande #{id} confirmée par le vendeur — prête à livrer."
                            : $"Commande #{id} : produit indisponible. Motif : {dto.Motif ?? "Non précisé"}"
                    });
                    foreach (var s in subs)
                    {
                        try { await pushClient.SendNotificationAsync(new PushSubscription(s.Endpoint, s.P256dh, s.Auth), payload, vapid); }
                        catch { }
                    }
                }
                catch { }
            });

            return Ok(new { message = dto.Available ? "Commande confirmée" : "Commande annulée", status = order.Status });
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


    public class VendorConfirmDto
    {
        public bool Available { get; set; }
        public string? Motif { get; set; }
    }
}