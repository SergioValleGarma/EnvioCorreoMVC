using System.Net;
using System.Net.Mail;

namespace RabbitMQApiService.Services
{
    public class MailtrapEmailService : IEmailService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private readonly bool _enableSsl;

        public MailtrapEmailService(IConfiguration configuration)
        {
            var mailSettings = configuration.GetSection("MailSettings");
            _host = mailSettings["Host"] ?? "sandbox.smtp.mailtrap.io";
            _port = int.Parse(mailSettings["Port"] ?? "2525");
            _username = mailSettings["UserName"] ?? "067fd62d43548a";
            _password = mailSettings["Password"] ?? "911b606f845a7e";
            _enableSsl = bool.Parse(mailSettings["EnableSsl"] ?? "true");
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                using (var client = new SmtpClient(_host, _port))
                {
                    client.Credentials = new NetworkCredential(_username, _password);
                    client.EnableSsl = _enableSsl;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress("noreply@universidad.edu", "Gestión Académica"),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(to);

                    await client.SendMailAsync(mailMessage);
                    Console.WriteLine($"[MAILTRAP] Email enviado exitosamente a: {to}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MAILTRAP ERROR] Error enviando email: {ex.Message}");
                throw;
            }
        }
    }
}
