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
                    <h2>Ranita Market</h2>
                    <p>Votre code de réinitialisation est :</p>
                    <h1>{code}</h1>
                    <p>Ce code expire dans 10 minutes.</p>"
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