using RanitaApi.Models;
using System.Text;
using System.Text.Json;

namespace RanitaApi.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http = new HttpClient();
        private const string ADMIN_EMAIL = "ranitabouda@gmail.com";
        private const string FOOTER = @"
            <hr style='border:none;border-top:1px solid #f3f4f6;margin:24px 0;'/>
            <p style='font-size:11px;color:#9ca3af;text-align:center;margin:0;'>
                Ranita Market — <a href='https://www.ranita-shop.com' style='color:#9ca3af;'>www.ranita-shop.com</a><br>
                Vous recevez cet email suite à une activite sur votre compte Ranita Market.
            </p>";

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        // ✅ Nouvelle commande → admin
        public async Task SendNewOrderNotificationAsync(int orderId, string customerName, string customerPhone, string customerAddress, decimal total)
        {
            var payload = new
            {
                sender = new { name = _config["Brevo:SenderName"], email = _config["Brevo:SenderEmail"] },
                to = new[] { new { email = ADMIN_EMAIL } },
                subject = $"Nouvelle commande #{orderId} - {customerName}",
                htmlContent = $@"
<div style='font-family:Arial;padding:20px;background:#f4f4f4'>
  <div style='max-width:500px;margin:auto;background:white;padding:24px;border-radius:10px;'>
    <h2 style='color:#059669;margin:0 0 16px'>Nouvelle commande recue !</h2>
    <table style='width:100%;font-size:15px;'>
      <tr><td style='padding:6px 0;color:#666'>Commande</td><td><strong>#{orderId}</strong></td></tr>
      <tr><td style='padding:6px 0;color:#666'>Client</td><td><strong>{customerName}</strong></td></tr>
      <tr><td style='padding:6px 0;color:#666'>Telephone</td><td><strong>{customerPhone}</strong></td></tr>
      <tr><td style='padding:6px 0;color:#666'>Adresse</td><td>{customerAddress}</td></tr>
      <tr><td style='padding:6px 0;color:#666'>Total</td><td><strong style='color:#f97316;font-size:18px'>{total.ToString("N0")} FCFA</strong></td></tr>
    </table>
    <a href='https://www.ranita-shop.com/admin-orders.html'
       style='display:inline-block;margin-top:20px;background:#059669;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:700;font-size:15px;'>
      Voir la commande
    </a>
    {FOOTER}
  </div>
</div>"
            };
            await SendBrevoEmail(payload);
        }

        // ✅ Nouvelle commande → vendeur
        public async Task SendNewOrderToSellerAsync(string sellerEmail, string shopName, int orderId, List<OrderItem> items, decimal sellerTotal)
        {
            var itemsHtml = string.Join("", items.Select(i => $@"
                <tr>
                    <td style='padding:8px;border-bottom:1px solid #f3f4f6;'>{i.ProductName}{(string.IsNullOrEmpty(i.VariantName) ? "" : $" ({i.VariantName})")}</td>
                    <td style='padding:8px;border-bottom:1px solid #f3f4f6;text-align:center;'>{i.Quantity}</td>
                    <td style='padding:8px;border-bottom:1px solid #f3f4f6;text-align:right;font-weight:700;color:#10b981;'>{(i.Price * i.Quantity).ToString("N0")} FCFA</td>
                </tr>"));

            var payload = new
            {
                sender = new { name = _config["Brevo:SenderName"], email = _config["Brevo:SenderEmail"] },
                to = new[] { new { email = sellerEmail } },
                subject = $"🛒 Nouvelle vente #{orderId} - {shopName}",
                htmlContent = $@"
<div style='font-family:Arial;padding:20px;background:#f4f4f4'>
  <div style='max-width:500px;margin:auto;background:white;padding:24px;border-radius:10px;'>
    <h2 style='color:#059669;margin:0 0 8px'>🎉 Vous avez une nouvelle vente !</h2>
    <p>Bonjour <strong>{shopName}</strong>,</p>
    <p>Un client vient de passer une commande contenant vos produits sur <strong>Ranita Market</strong>.</p>
    <div style='background:#f0fdf4;border-radius:8px;padding:16px;margin:16px 0;text-align:center;border:1px solid #d1fae5;'>
        <div style='font-size:13px;color:#6b7280;'>Numéro de commande</div>
        <div style='font-size:28px;font-weight:800;color:#111827;'>#{orderId}</div>
        <div style='display:inline-block;margin-top:8px;background:#fef3c7;color:#92400e;padding:6px 16px;border-radius:999px;font-weight:700;font-size:14px;'>En attente de livraison</div>
    </div>
    <table style='width:100%;border-collapse:collapse;font-size:14px;'>
        <thead>
            <tr style='background:#f9fafb;'>
                <th style='padding:8px;text-align:left;color:#6b7280;font-weight:600;'>Produit</th>
                <th style='padding:8px;text-align:center;color:#6b7280;font-weight:600;'>Qté</th>
                <th style='padding:8px;text-align:right;color:#6b7280;font-weight:600;'>Montant</th>
            </tr>
        </thead>
        <tbody>{itemsHtml}</tbody>
    </table>
    <div style='display:flex;justify-content:space-between;font-size:16px;font-weight:800;margin-top:16px;padding-top:12px;border-top:2px solid #f3f4f6;'>
        <span>Votre part (après commission)</span>
        <span style='color:#10b981;'>{sellerTotal.ToString("N0")} FCFA</span>
    </div>
    <a href='https://www.ranita-shop.com/vendeur.html'
       style='display:inline-block;margin-top:20px;background:#059669;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:700;font-size:15px;'>
      Voir mes commandes
    </a>
    {FOOTER}
  </div>
</div>"
            };
            await SendBrevoEmail(payload);
        }

        // ✅ Commande livrée → vendeur
        public async Task SendOrderDeliveredToSellerAsync(string sellerEmail, string shopName, int orderId, decimal sellerTotal)
        {
            var payload = new
            {
                sender = new { name = _config["Brevo:SenderName"], email = _config["Brevo:SenderEmail"] },
                to = new[] { new { email = sellerEmail } },
                subject = $"📦 Commande #{orderId} livrée - Paiement en cours",
                htmlContent = $@"
<div style='font-family:Arial;padding:20px;background:#f4f4f4'>
  <div style='max-width:500px;margin:auto;background:white;padding:24px;border-radius:10px;'>
    <h2 style='color:#059669;margin:0 0 8px'>📦 Commande livrée avec succès !</h2>
    <p>Bonjour <strong>{shopName}</strong>,</p>
    <p>La commande <strong>#{orderId}</strong> a été livrée au client. Votre paiement est en cours de traitement.</p>
    <div style='background:#f0fdf4;border-radius:8px;padding:20px;margin:16px 0;text-align:center;border:1px solid #d1fae5;'>
        <div style='font-size:13px;color:#6b7280;margin-bottom:4px;'>Montant à recevoir</div>
        <div style='font-size:36px;font-weight:800;color:#10b981;'>{sellerTotal.ToString("N0")} FCFA</div>
        <div style='display:inline-block;margin-top:8px;background:#dbeafe;color:#1e40af;padding:6px 16px;border-radius:999px;font-weight:700;font-size:14px;'>Paiement en cours</div>
    </div>
    <p style='color:#6b7280;font-size:13px;'>Le paiement sera effectué sur votre moyen de paiement enregistré sous 24 à 48h.</p>
    <a href='https://www.ranita-shop.com/vendeur.html'
       style='display:inline-block;margin-top:20px;background:#059669;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:700;font-size:15px;'>
      Mon tableau de bord
    </a>
    {FOOTER}
  </div>
</div>"
            };
            await SendBrevoEmail(payload);
        }

        // ✅ Paiement effectué → vendeur
        public async Task SendPayoutToSellerAsync(string sellerEmail, string shopName, int orderId, decimal netAmount, string paymentMethod, string paymentDetails)
        {
            var payload = new
            {
                sender = new { name = _config["Brevo:SenderName"], email = _config["Brevo:SenderEmail"] },
                to = new[] { new { email = sellerEmail } },
                subject = $"💰 Paiement reçu - {netAmount.ToString("N0")} FCFA",
                htmlContent = $@"
<div style='font-family:Arial;padding:20px;background:#f4f4f4'>
  <div style='max-width:500px;margin:auto;background:white;padding:24px;border-radius:10px;'>
    <h2 style='color:#059669;margin:0 0 8px'>💰 Votre paiement a été effectué !</h2>
    <p>Bonjour <strong>{shopName}</strong>,</p>
    <p>Nous avons effectué votre paiement pour la commande <strong>#{orderId}</strong>.</p>
    <div style='background:#f0fdf4;border-radius:8px;padding:20px;margin:16px 0;text-align:center;border:1px solid #d1fae5;'>
        <div style='font-size:13px;color:#6b7280;margin-bottom:4px;'>Montant reçu</div>
        <div style='font-size:36px;font-weight:800;color:#10b981;'>{netAmount.ToString("N0")} FCFA</div>
        <div style='display:inline-block;margin-top:8px;background:#d1fae5;color:#065f46;padding:6px 16px;border-radius:999px;font-weight:700;font-size:14px;'>✅ Payé</div>
    </div>
    <table style='width:100%;font-size:14px;'>
        <tr><td style='padding:6px 0;color:#6b7280;'>Commande</td><td><strong>#{orderId}</strong></td></tr>
        <tr><td style='padding:6px 0;color:#6b7280;'>Moyen de paiement</td><td><strong>{paymentMethod?.Replace("_", " ")}</strong></td></tr>
        <tr><td style='padding:6px 0;color:#6b7280;'>Numéro</td><td><strong>{paymentDetails}</strong></td></tr>
        <tr><td style='padding:6px 0;color:#6b7280;'>Montant</td><td><strong style='color:#10b981;font-size:16px;'>{netAmount.ToString("N0")} FCFA</strong></td></tr>
    </table>
    <a href='https://www.ranita-shop.com/vendeur.html'
       style='display:inline-block;margin-top:20px;background:#059669;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:700;font-size:15px;'>
      Voir mes gains
    </a>
    {FOOTER}
  </div>
</div>"
            };
            await SendBrevoEmail(payload);
        }

        // ✅ Nouveau client → admin
        public async Task SendNewClientNotificationAsync(string clientName, string clientEmail, string clientPhone)
        {
            var payload = new
            {
                sender = new { name = _config["Brevo:SenderName"], email = _config["Brevo:SenderEmail"] },
                to = new[] { new { email = ADMIN_EMAIL } },
                subject = $"Nouveau client inscrit - {clientName}",
                htmlContent = $@"
<div style='font-family:Arial;padding:20px;background:#f4f4f4'>
  <div style='max-width:500px;margin:auto;background:white;padding:24px;border-radius:10px;'>
    <h2 style='color:#3b82f6;margin:0 0 16px'>Nouveau client inscrit !</h2>
    <table style='width:100%;font-size:15px;'>
      <tr><td style='padding:6px 0;color:#666'>Nom</td><td><strong>{clientName}</strong></td></tr>
      <tr><td style='padding:6px 0;color:#666'>Email</td><td>{clientEmail}</td></tr>
      <tr><td style='padding:6px 0;color:#666'>Telephone</td><td>{clientPhone ?? "—"}</td></tr>
    </table>
    <a href='https://www.ranita-shop.com/admin-clients.html'
       style='display:inline-block;margin-top:20px;background:#3b82f6;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:700;font-size:15px;'>
      Voir les clients
    </a>
    {FOOTER}
  </div>
</div>"
            };
            await SendBrevoEmail(payload);
        }

        // ✅ Reset password → client
        public async Task SendResetCodeAsync(string toEmail, string code)
        {
            var payload = new
            {
                sender = new { name = _config["Brevo:SenderName"], email = _config["Brevo:SenderEmail"] },
                to = new[] { new { email = toEmail } },
                subject = "Code de reinitialisation - Ranita Market",
                htmlContent = $@"
<div style='font-family:Arial;padding:20px;background:#f4f4f4'>
  <div style='max-width:500px;margin:auto;background:white;padding:20px;border-radius:10px;text-align:center'>
    <h2 style='color:#059669'>Ranita Market</h2>
    <p>Vous avez demande une reinitialisation de mot de passe.</p>
    <div style='font-size:36px;font-weight:800;letter-spacing:8px;color:#f97316;padding:16px;background:#fff7ed;border-radius:8px;margin:16px 0;'>{code}</div>
    <p style='color:#777'>Ce code expire dans 10 minutes.</p>
    {FOOTER}
  </div>
</div>"
            };
            await SendBrevoEmail(payload);
        }

        // ✅ Confirmation commande → client
        public async Task SendOrderConfirmationToClientAsync(string toEmail, string clientName, int orderId, decimal total, List<OrderItem> items)
        {
            var itemsHtml = string.Join("", items.Select(i => $@"
                <tr>
                    <td style='padding:8px;border-bottom:1px solid #f3f4f6;'>{i.ProductName}{(string.IsNullOrEmpty(i.VariantName) ? "" : $" ({i.VariantName})")}</td>
                    <td style='padding:8px;border-bottom:1px solid #f3f4f6;text-align:center;'>{i.Quantity}</td>
                    <td style='padding:8px;border-bottom:1px solid #f3f4f6;text-align:right;font-weight:700;color:#10b981;'>{(i.Price * i.Quantity).ToString("N0")} FCFA</td>
                </tr>"));

            var payload = new
            {
                replyTo = new { email = "ranitabouda@gmail.com", name = "Ranita Market" },
                sender = new { name = _config["Brevo:SenderName"], email = _config["Brevo:SenderEmail"] },
                to = new[] { new { email = toEmail } },
                subject = $"Commande #{orderId} confirmee - Ranita Market",
                htmlContent = $@"
<div style='font-family:Arial;padding:20px;background:#f4f4f4'>
  <div style='max-width:500px;margin:auto;background:white;padding:24px;border-radius:10px;'>
    <h2 style='color:#059669;margin:0 0 8px'>Commande recue !</h2>
    <p>Bonjour <strong>{clientName}</strong>,</p>
    <p>Merci pour votre commande sur <strong>Ranita Market</strong> ! Nous l'avons bien recue et elle est en cours de traitement.</p>
    <div style='background:#f9fafb;border-radius:8px;padding:16px;margin:16px 0;text-align:center;'>
        <div style='font-size:13px;color:#6b7280;'>Numero de commande</div>
        <div style='font-size:28px;font-weight:800;color:#111827;'>#{orderId}</div>
        <div style='display:inline-block;margin-top:8px;background:#fef3c7;color:#92400e;padding:6px 16px;border-radius:999px;font-weight:700;font-size:14px;'>En attente</div>
    </div>
    <table style='width:100%;border-collapse:collapse;font-size:14px;'>
        <thead>
            <tr style='background:#f9fafb;'>
                <th style='padding:8px;text-align:left;color:#6b7280;font-weight:600;'>Produit</th>
                <th style='padding:8px;text-align:center;color:#6b7280;font-weight:600;'>Qte</th>
                <th style='padding:8px;text-align:right;color:#6b7280;font-weight:600;'>Prix</th>
            </tr>
        </thead>
        <tbody>{itemsHtml}</tbody>
    </table>
    <div style='display:flex;justify-content:space-between;font-size:16px;font-weight:800;margin-top:16px;padding-top:12px;border-top:2px solid #f3f4f6;'>
        <span>Total</span>
        <span style='color:#10b981;'>{total.ToString("N0")} FCFA</span>
    </div>
    <a href='https://www.ranita-shop.com/client-orders.html'
       style='display:inline-block;margin-top:20px;background:#059669;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:700;font-size:15px;'>
      Suivre ma commande
    </a>
    {FOOTER}
  </div>
</div>"
            };
            await SendBrevoEmail(payload);
        }

        // ✅ Changement de statut → client
        public async Task SendOrderStatusUpdateAsync(string clientEmail, string clientName, int orderId, string newStatus, List<OrderItem>? items = null)
        {
            var (color, message) = newStatus switch
            {
                "Validée" => ("#3b82f6", "Votre commande a ete validee et est en cours de preparation."),
                "Livrée" => ("#10b981", "Votre commande a ete livree. Merci pour votre achat !"),
                "Annulée" => ("#ef4444", "Votre commande a ete annulee. Contactez-nous pour plus d'informations."),
                _ => ("#f59e0b", "Le statut de votre commande a ete mis a jour.")
            };

            var avisSection = "";
            if (newStatus == "Livrée" && items != null && items.Count > 0)
            {
                var produitsHtml = string.Join("", items.Select(i => $@"
        <div style='display:flex;align-items:center;gap:12px;margin-bottom:10px;padding:10px;background:#f9fafb;border-radius:10px;'>
            <img src='{i.ImageUrl}' style='width:50px;height:50px;object-fit:cover;border-radius:8px;'>
            <div style='flex:1;font-size:14px;font-weight:600;color:#111827;'>{i.ProductName}</div>
            <a href='https://www.ranita-shop.com/review.html?orderId={orderId}&productId={i.ProductId}'
               style='background:#f97316;color:white;padding:8px 14px;border-radius:8px;text-decoration:none;font-weight:700;font-size:13px;white-space:nowrap;'>
                ⭐ Avis
            </a>
        </div>"));

                avisSection = $@"
        <div style='margin-top:24px;padding-top:16px;border-top:1px solid #f3f4f6;'>
            <p style='font-weight:700;font-size:15px;color:#111827;margin:0 0 8px;'>Donnez votre avis !</p>
            <p style='color:#6b7280;font-size:13px;margin:0 0 14px;'>Votre avis aide d'autres clients a faire le bon choix.</p>
            {produitsHtml}
        </div>";
            }

            var payload = new
            {
                replyTo = new { email = "ranitabouda@gmail.com", name = "Ranita Market" },
                sender = new { name = _config["Brevo:SenderName"], email = _config["Brevo:SenderEmail"] },
                to = new[] { new { email = clientEmail } },
                subject = $"📦 Commande #{orderId} - {newStatus}",
                htmlContent = $@"
<div style='font-family:Arial;padding:20px;background:#f4f4f4'>
  <div style='max-width:500px;margin:auto;background:white;padding:24px;border-radius:10px;'>
    <h2 style='color:{color};margin:0 0 16px'>Statut de votre commande mis a jour</h2>
    <p>Bonjour <strong>{clientName}</strong>,</p>
    <p>{message}</p>
    <div style='background:#f9fafb;border-radius:8px;padding:16px;margin:16px 0;text-align:center;'>
        <div style='font-size:13px;color:#6b7280;'>Commande</div>
        <div style='font-size:28px;font-weight:800;color:#111827;'>#{orderId}</div>
        <div style='display:inline-block;margin-top:8px;background:{color};color:white;padding:6px 16px;border-radius:999px;font-weight:700;font-size:14px;'>{newStatus}</div>
    </div>
    <a href='https://www.ranita-shop.com/client-orders.html'
       style='display:inline-block;margin-top:8px;background:#059669;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:700;font-size:15px;'>
      Voir mes commandes
    </a>
    {avisSection}
    {FOOTER}
  </div>
</div>"
            };
            await SendBrevoEmail(payload);
        }

        // ✅ NOUVEAU — Crédit parrainage → parrain
        public async Task SendReferralCreditNotificationAsync(
            string parrainEmail, string parrainName,
            string friendName, int creditAmount)
        {
            var payload = new
            {
                sender = new { name = _config["Brevo:SenderName"], email = _config["Brevo:SenderEmail"] },
                to = new[] { new { email = parrainEmail } },
                subject = $"🎁 +{creditAmount.ToString("N0")} F CFA crédités — Parrainage Ranita Market",
                htmlContent = $@"
<div style='font-family:Arial;padding:20px;background:#f4f4f4'>
  <div style='max-width:500px;margin:auto;background:white;padding:24px;border-radius:10px;'>
    <h2 style='color:#10b981;margin:0 0 8px'>🎉 Bonne nouvelle !</h2>
    <p>Bonjour <strong>{parrainName}</strong>,</p>
    <p>Votre ami <strong>{friendName}</strong> vient d'effectuer son premier achat sur Ranita Market grâce à votre lien de parrainage.</p>
    <div style='background:#f0fdf4;border-radius:12px;padding:24px;margin:20px 0;text-align:center;border:1px solid #a7f3d0;'>
        <div style='font-size:14px;color:#6b7280;margin-bottom:6px;'>Crédit ajouté à votre compte</div>
        <div style='font-size:42px;font-weight:800;color:#10b981;'>+{creditAmount.ToString("N0")} F</div>
        <div style='font-size:12px;color:#6b7280;margin-top:4px;'>CFA</div>
    </div>
    <p style='color:#6b7280;font-size:14px;'>Continuez à parrainer vos amis pour gagner encore plus ! Chaque ami qui passe sa première commande vous rapporte <strong>{creditAmount.ToString("N0")} F CFA</strong>.</p>
    <a href='https://www.ranita-shop.com/referral.html'
       style='display:inline-block;margin-top:16px;background:#10b981;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:700;font-size:15px;'>
      Voir mes gains de parrainage
    </a>
    {FOOTER}
  </div>
</div>"
            };
            await SendBrevoEmail(payload);
        }

        // ✅ MÉTHODE PRIVÉE — Brevo API
        private async Task SendBrevoEmail(object payload)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", _config["Brevo:ApiKey"]);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _http.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"BREVO RESPONSE ({response.StatusCode}): {result}");
            if (!response.IsSuccessStatusCode)
                Console.WriteLine("BREVO ERROR: " + result);
        }
    }
}