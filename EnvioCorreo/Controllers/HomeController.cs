using EnvioCorreo.Service;
using Microsoft.AspNetCore.Mvc;

namespace EnvioCorreo.Controllers
{
    public class HomeController : Controller
    {
        private readonly IEmailService _emailService;

        // 💡 Inyección de dependencias en el constructor
        public HomeController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        public IActionResult Contact()
        {
            // Esto simplemente busca y devuelve la vista Views/Home/Contact.cshtml
            return View();
        }

        [HttpPost] // O el método que uses para enviar
        public async Task<IActionResult> EnviarContacto(string nombre, string emailUsuario, string mensaje)
        {
            string subject = $"Nuevo mensaje de contacto de {nombre}";
            string body = $"Email: {emailUsuario}<br>Mensaje: {mensaje}";
            string recipient = "contacto@tuempresa.com"; // El email de quien recibe, que irá a Mailtrap

            try
            {
                await _emailService.SendEmailAsync(recipient, subject, body);
                TempData["Mensaje"] = "Su mensaje ha sido enviado correctamente (a Mailtrap).";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Hubo un error al enviar el correo: {ex.Message}";
            }

            return View("Contact"); // O la vista que prefieras
        }
    }
}
