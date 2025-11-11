using EnvioCorreo.Data;
using EnvioCorreo.Models;
using EnvioCorreo.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnvioCorreo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MatriculaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IMessageQueueService _messageQueueService; // ✅ AGREGAR ESTO

        // Inyección de dependencias para DBContext, EmailService y MessageQueueService
        public MatriculaController(ApplicationDbContext context, IEmailService emailService, IMessageQueueService messageQueueService)
        {
            _context = context;
            _emailService = emailService;
            _messageQueueService = messageQueueService; // ✅ AGREGAR ESTO
        }

        // POST: api/Matricula/registrar
        [HttpPost("registrar")]
        public async Task<IActionResult> RegistrarMatricula([FromBody] MatriculaRegistroDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. Verificar si el estudiante existe y obtener su Email
            var estudiante = await _context.Estudiantes
                                           .FirstOrDefaultAsync(e => e.EstudianteId == dto.EstudianteId);

            if (estudiante == null)
            {
                return NotFound(new { Message = $"Estudiante con ID {dto.EstudianteId} no encontrado." });
            }

            // 2. Crear la nueva entidad Matricula
            var nuevaMatricula = new Matricula
            {
                EstudianteId = dto.EstudianteId,
                SeccionId = dto.SeccionId,
                Costo = dto.Costo,
                MetodoPago = dto.MetodoPago,
                FechaMatricula = DateTime.Today,
                Estado = "PENDIENTE" // Estado inicial
            };

            // 3. Guardar en la Base de Datos
            _context.Matriculas.Add(nuevaMatricula);
            await _context.SaveChangesAsync(); // Se guarda el registro en la DB

            // 4. Enviar Correo de Notificación (Usando Mailtrap)
            try
            {
                string asunto = $"✅ Confirmación de Pre-Matrícula - Código #{nuevaMatricula.MatriculaId}";
                string cuerpo = $"Hola **{estudiante.Nombre} {estudiante.Apellido}**,<br><br>" +
                                $"Hemos recibido tu solicitud de matrícula (ID: {nuevaMatricula.MatriculaId}).<br>" +
                                $"Tu estado actual es: **{nuevaMatricula.Estado}** y el costo es ${nuevaMatricula.Costo:N2}.<br>" +
                                $"Revisa tu portal para completar el pago.<br><br>" +
                                $"Atentamente,<br>Gestión Académica.";

                await _emailService.SendEmailAsync(estudiante.Email, asunto, cuerpo);

                // ✅ 5. PUBLICAR MENSAJE EN RABBITMQ - DESPUÉS DE ENVIAR CORREO EXITOSAMENTE
                var emailEvent = new EmailSentEvent
                {
                    EstudianteId = estudiante.EstudianteId,
                    SeccionId = nuevaMatricula.SeccionId,
                    Timestamp = DateTime.UtcNow,
                    MessageType = "EmailSent",
                    // Opcional: agregar información del correo
                    To = estudiante.Email,
                    Subject = asunto
                };

                _messageQueueService.PublishEmailSentMessage(emailEvent);
                Console.WriteLine($"[RABBITMQ] Mensaje publicado para estudiante {estudiante.EstudianteId}");
            }
            catch (Exception ex)
            {
                // Opcional: Registrar el error de correo, pero permitir que el registro de matrícula continúe
                Console.WriteLine($"Error al enviar correo de notificación: {ex.Message}");

                // ❌ Si falla el correo, también publicar un evento de error
                var errorEvent = new EmailSentEvent
                {
                    EstudianteId = estudiante.EstudianteId,
                    SeccionId = nuevaMatricula.SeccionId,
                    Timestamp = DateTime.UtcNow,
                    MessageType = "EmailFailed",
                    To = estudiante.Email
                };
                _messageQueueService.PublishEmailSentMessage(errorEvent);
            }

            var respuesta = new MatriculaRespuestaDto
            {
                MatriculaId = nuevaMatricula.MatriculaId,
                EstudianteId = nuevaMatricula.EstudianteId,
                SeccionId = nuevaMatricula.SeccionId,
                Costo = nuevaMatricula.Costo,
                Estado = nuevaMatricula.Estado,
                // Usamos la entidad 'estudiante' que ya cargamos al inicio
                NombreCompletoEstudiante = $"{estudiante.Nombre} {estudiante.Apellido}"
            };

            // Devolvemos el DTO, que no tiene referencias circulares.
            return CreatedAtAction(nameof(RegistrarMatricula), new { id = respuesta.MatriculaId }, respuesta);
        }
    }
}