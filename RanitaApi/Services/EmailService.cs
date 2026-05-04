using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net.Mail;

namespace RanitaApi.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendResetCodeAsync(string toEmail, string code)
        {
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(
                _config["Smtp:FromName"],
                _config["Smtp:User"]
            ));

            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "Code de réinitialisation - Ranita Market";

            email.Body = new TextPart("html")
            {
                Text = $@"
                    <h2>Réinitialisation du mot de passe</h2>
                    <p>Votre code est :</p>
                    <h1>{code}</h1>
                    <p>Ce code expire dans 10 minutes.</p>
                "
            };

            using var smtp = new SmtpClient();

            await smtp.ConnectAsync(
                _config["Smtp:Host"],
                int.Parse(_config["Smtp:Port"]!),
                SecureSocketOptions.StartTls
            );

            await smtp.AuthenticateAsync(
                _config["Smtp:User"],
                _config["Smtp:Password"]
            );

            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}