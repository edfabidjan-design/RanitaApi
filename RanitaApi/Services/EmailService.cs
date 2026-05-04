using System.Text;
using System.Text.Json;

namespace RanitaApi.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http = new HttpClient();

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendResetCodeAsync(string toEmail, string code)
        {
            var payload = new
            {
                sender = new
                {
                    name = _config["Brevo:SenderName"],
                    email = _config["Brevo:SenderEmail"]
                },
                to = new[]
                {
                    new { email = toEmail }
                },
                subject = "Code de réinitialisation - Ranita Market",
                htmlContent = $@"
<div style='font-family:Arial;padding:20px;background:#f4f4f4'>
  <div style='max-width:500px;margin:auto;background:white;padding:20px;border-radius:10px;text-align:center'>
    <h2 style='color:#059669'>Ranita Market</h2>
    <p>Vous avez demandé une réinitialisation de mot de passe.</p>
    <p>Voici votre code :</p>
    <h1 style='letter-spacing:5px;color:#f97316'>{code}</h1>
    <p style='color:#777'>Ce code expire dans 10 minutes.</p>
    <hr/>
    <p style='font-size:12px;color:#aaa'>
      Si vous n'êtes pas à l'origine de cette demande, ignorez cet email.
    </p>
  </div>
</div>"
            };

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.brevo.com/v3/smtp/email"
            );

            request.Headers.Add("api-key", _config["Brevo:ApiKey"]);
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _http.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception("BREVO ERROR: " + result);
        }
    }
}