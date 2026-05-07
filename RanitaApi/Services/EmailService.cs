using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
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
                Vous recevez cet email suite à une activité sur votre compte Ranita Market.
            </p>";

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        // ============================
        // EMAILS ADMIN → Brevo
        // ============================

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

        // ============================
        // EMAILS CLIENT → Gmail SMTP
        // ============================

        // ✅ Reset password → client
        public async Task SendResetCodeAsync(string toEmail, string code)
        {
            var subject = "Code de reinitialisation - Ranita Market";
            var html = $@"
<div style='font-family:Arial;padding:20px;background:#f4f4f4'>
  <div style='max-width:500px;margin:auto;background:white;padding:20px;border-radius:10px;text-align:center'>
    <h2 style='color:#059669'>Ranita Market</h2>
    <p>Vous avez demande une reinitialisation de mot de passe.</p>
    <div style='font-size:36px;font-weight:800;letter-spacing:8px;color:#f97316;padding:16px;background:#fff7ed;border-radius:8px;margin:16px 0;'>{code}</div>
    <p style='color:#777'>Ce code expire dans 10 minutes.</p>
    {FOOTER}
  </div>
</div>";
            await SendGmailEmail(toEmail, subject, html);
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

            var subject = $"Commande #{orderId} confirmee - Ranita Market";
            var html = $@"
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
    <a href='https://www.ranita-shop.com/my-orders.html'
       style='display:inline-block;margin-top:20px;background:#059669;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:700;font-size:15px;'>
      Suivre ma commande
    </a>
    {FOOTER}
  </div>
</div>";
            await SendGmailEmail(toEmail, subject, html);
        }

        // ✅ Changement de statut → client
        public async Task SendOrderStatusUpdateAsync(string clientEmail, string clientName, int orderId, string newStatus)
        {
            var (color, message) = newStatus switch
            {
                "Validée" => ("#3b82f6", "Votre commande a ete validee et est en cours de preparation."),
                "Livrée" => ("#10b981", "Votre commande a ete livree. Merci pour votre achat !"),
                "Annulée" => ("#ef4444", "Votre commande a ete annulee. Contactez-nous pour plus d'informations."),
                _ => ("#f59e0b", "Le statut de votre commande a ete mis a jour.")
            };

            var subject = $"Commande #{orderId} - {newStatus}";
            var html = $@"
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
    <a href='https://www.ranita-shop.com/my-orders.html'
       style='display:inline-block;margin-top:8px;background:#059669;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:700;font-size:15px;'>
      Voir mes commandes
    </a>
    {FOOTER}
  </div>
</div>";
            await SendGmailEmail(clientEmail, subject, html);
        }

        // ============================
        // MÉTHODES PRIVÉES
        // ============================

        private async Task SendGmailEmail(string toEmail, string subject, string htmlContent)
        {
            try
            {
                var gmailEmail = _config["Gmail:Email"];
                var gmailPassword = _config["Gmail:Password"];
                var senderName = _config["Gmail:SenderName"] ?? "Ranita Market";

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, gmailEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = htmlContent };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(gmailEmail, gmailPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Console.WriteLine($"GMAIL SENT to {toEmail} - {subject}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GMAIL ERROR: {ex.Message}");
            }
        }

        private async Task SendBrevoEmail(object payload)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", _config["Brevo:ApiKey"]);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _http.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                Console.WriteLine("BREVO ERROR: " + result);
        }
    }
}