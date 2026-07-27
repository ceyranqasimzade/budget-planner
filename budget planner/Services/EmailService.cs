using System.Net;
using System.Net.Mail;

namespace budget_planner.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string message);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var host = _config["EmailSettings:Host"];
            var port = int.Parse(_config["EmailSettings:Port"]);
            var user = _config["EmailSettings:User"];
            var password = _config["EmailSettings:Password"];

            var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage(user, email, subject, message)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(mailMessage);
        }
    }
}