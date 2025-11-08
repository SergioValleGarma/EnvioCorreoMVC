using EnvioCorreo.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace EnvioCorreo.Service
{
    public class EmailService : IEmailService
    {
        private readonly MailSettings _mailSettings;

        // 💡 Inyección de dependencias: Recibimos la configuración
        public EmailService(IOptions<MailSettings> mailSettings)
        {
            _mailSettings = mailSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            using (var client = new SmtpClient(_mailSettings.Host, _mailSettings.Port))
            {
                client.Credentials = new NetworkCredential(_mailSettings.UserName, _mailSettings.Password);
                client.EnableSsl = _mailSettings.EnableSsl;
                // Opcional: Deshabilitar el uso del pool de conexiones para evitar problemas en algunos entornos
                // client.DeliveryMethod = SmtpDeliveryMethod.Network; 

                var mailMessage = new MailMessage(_mailSettings.SenderEmail, toEmail, subject, body);

                // Puedes agregar formato HTML si lo necesitas:
                mailMessage.IsBodyHtml = true;

                await client.SendMailAsync(mailMessage);
            }
        }
    }
}
