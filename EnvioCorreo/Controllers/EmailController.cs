using EnvioCorreo.Models;
using EnvioCorreo.Service;
using Microsoft.AspNetCore.Mvc;

namespace EnvioCorreo.Controllers
{

    [ApiController]
    [Route("api/[controller]")] // Define la ruta base como /api/Email
    public class EmailController : ControllerBase // Hereda de ControllerBase para API
    {
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        // 💡 Este es el nuevo endpoint de API. La URL será POST /api/Email/enviar
        [HttpPost("enviar")]
        public async Task<IActionResult> EnviarContacto([FromBody] ContactoDto model)
        {
            // 1. Validación de Modelo
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); // Devuelve 400 con detalles si faltan campos
            }

            // 2. Preparar el contenido del correo (misma lógica que antes)
            string subject = $"[API TEST] Nuevo mensaje de {model.Nombre}";
            string body = $"Email: {model.EmailUsuario}<br>Mensaje: {model.Mensaje}";
            string recipient = "contacto@tuempresa.com"; // El email que se captura en Mailtrap

            try
            {
                await _emailService.SendEmailAsync(recipient, subject, body);

                // 3. Devolver una respuesta HTTP 200/202 (Éxito)
                return Ok(new { Message = "Correo de prueba enviado a Mailtrap con éxito.", Status = "Success" });
            }
            catch (Exception ex)
            {
                // 4. Devolver una respuesta HTTP 500 (Error interno)
                return StatusCode(500, new { Message = "Error interno al intentar enviar el correo.", Error = ex.Message });
            }
        }
    }
}
