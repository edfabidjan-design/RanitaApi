using System.Text;
using System.Text.Json;

namespace RanitaApi.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http = new HttpClient();
        private const string ADMIN_EMAIL = "ranitabouda@gmail.com";

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        // ✅ Reset password
        public async Task SendResetCodeAsync(string toEmail, string code)
        {
            var payload = new
            {
                sender = new { name = _config["Brevo:SenderName"], email = _config["Brevo:SenderEmail"] },
                to = new[] { new { email = toEmail } },
                subject = "Code de réinitialisation - Ranita Market",
                htmlContent = $@"
<div style='font-family:Arial;padding:20px;background:#f4f4f4'>
  <div style='max-width:500px;margin:auto;background:white;padding:20px;border-radius:10px;text-align:center'>
    <h2 style='color:#059669'>Ranita Market</h2>
    <p>Vous avez demandé une réinitialisation de mot de passe.</p>
    <h1 style='letter-spacing:5px;color:#f97316'>{code}</h1>
    <p style='color:#777'>Ce code expire dans 10 minutes.</p>
  </div>
</div>"
            };
            await SendBrevoEmail(payload);
        }

        // ✅ Nouvelle commande → admin
        public async Task SendNewOrderNotificationAsync(int orderId, string customerName, string customerPhone, string customerAddress, decimal total)
        {
            var payload = new
            {
                sender = new { name = _config["Brevo:SenderName"], email = _config["Brevo:SenderEmail"] },
                to = new[] { new { email = ADMIN_EMAIL } },
                subject = $"🛒 Nouvelle commande #{orderId} - {customerName}",
                htmlContent = $@"
<div style='font-family:Arial;padding:20px;background:#f4f4f4'>
  <div style='max-width:500px;margin:auto;background:white;padding:24px;border-radius:10px;'>
    <h2 style='color:#059669;margin:0 0 16px'>🛒 Nouvelle commande reçue !</h2>
    <table style='width:100%;font-size:15px;'>
      <tr><td style='padding:6px 0;color:#666'>Commande</td><td><strong>#{orderId}</strong></td></tr>
      <tr><td style='padding:6px 0;color:#666'>Client</td><td><strong>{customerName}</strong></td></tr>
      <tr><td style='padding:6px 0;color:#666'>Téléphone</td><td><strong>{customerPhone}</strong></td></tr>
      <tr><td style='padding:6px 0;color:#666'>Adresse</td><td>{customerAddress}</td></tr>
      <tr><td style='padding:6px 0;color:#666'>Total</td><td><strong style='color:#f97316;font-size:18px'>{total.ToString("N0")} FCFA</strong></td></tr>
    </table>
    <a href='https://www.ranita-shop.com/admin-orders.html'
       style='display:inline-block;margin-top:20px;background:#059669;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:700;font-size:15px;'>
      Voir la commande →
    </a>
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
                subject = $"👤 Nouveau client inscrit - {clientName}",
                htmlContent = $@"
<div style='font-family:Arial;padding:20px;background:#f4f4f4'>
  <div style='max-width:500px;margin:auto;background:white;padding:24px;border-radius:10px;'>
    <h2 style='color:#3b82f6;margin:0 0 16px'>👤 Nouveau client inscrit !</h2>
    <table style='width:100%;font-size:15px;'>
      <tr><td style='padding:6px 0;color:#666'>Nom</td><td><strong>{clientName}</strong></td></tr>
      <tr><td style='padding:6px 0;color:#666'>Email</td><td>{clientEmail}</td></tr>
      <tr><td style='padding:6px 0;color:#666'>Téléphone</td><td>{clientPhone ?? "—"}</td></tr>
    </table>
    <a href='https://www.ranita-shop.com/admin-clients.html'
       style='display:inline-block;margin-top:20px;background:#3b82f6;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:700;font-size:15px;'>
      Voir les clients →
    </a>
  </div>
</div>"
            };
            await SendBrevoEmail(payload);
        }

        private async Task SendBrevoEmail(object payload)
        {
            var apiKey = _config["Brevo:ApiKey"];
            var senderEmail = _config["Brevo:SenderEmail"];
            var senderName = _config["Brevo:SenderName"];

            // LOG de diagnostic
            Console.WriteLine($"BREVO DEBUG - ApiKey présent: {!string.IsNullOrEmpty(apiKey)}");
            Console.WriteLine($"BREVO DEBUG - SenderEmail: {senderEmail}");
            Console.WriteLine($"BREVO DEBUG - SenderName: {senderName}");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _http.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"BREVO RESPONSE ({response.StatusCode}): {result}");

            if (!response.IsSuccessStatusCode)
                Console.WriteLine("BREVO ERROR: " + result);
        }
    }
}