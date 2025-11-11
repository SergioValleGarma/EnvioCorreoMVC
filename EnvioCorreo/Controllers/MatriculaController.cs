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
        private readonly IMessageQueueService _messageQueueService; // ✅ Solo RabbitMQ

        // ❌ REMOVEMOS IEmailService del constructor - ahora lo maneja el consumidor
        public MatriculaController(ApplicationDbContext context, IMessageQueueService messageQueueService)
        {
            _context = context;
            _messageQueueService = messageQueueService;
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
                Estado = "PENDIENTE"
            };

            // 3. Guardar en la Base de Datos
            _context.Matriculas.Add(nuevaMatricula);
            await _context.SaveChangesAsync();

            // 4. ✅ PUBLICAR EN RABBITMQ INMEDIATAMENTE (sin enviar correo aquí)
            try
            {
                string asunto = $"✅ Confirmación de Pre-Matrícula - Código #{nuevaMatricula.MatriculaId}";
                string cuerpo = $"Hola **{estudiante.Nombre} {estudiante.Apellido}**,<br><br>" +
                                $"Hemos recibido tu solicitud de matrícula (ID: {nuevaMatricula.MatriculaId}).<br>" +
                                $"Tu estado actual es: **{nuevaMatricula.Estado}** y el costo es ${nuevaMatricula.Costo:N2}.<br>" +
                                $"Revisa tu portal para completar el pago.<br><br>" +
                                $"Atentamente,<br>Gestión Académica.";

                var emailEvent = new EmailSentEvent
                {
                    EstudianteId = estudiante.EstudianteId,
                    SeccionId = nuevaMatricula.SeccionId,
                    MatriculaId = nuevaMatricula.MatriculaId, // ✅ Agregar esto
                    To = estudiante.Email,
                    Subject = asunto,
                    Body = cuerpo,
                    Timestamp = DateTime.UtcNow,
                    MessageType = "EmailPending" // ✅ Cambiar a "Pending"
                };

                _messageQueueService.PublishEmailSentMessage(emailEvent);
                Console.WriteLine($"[API] Mensaje publicado en RabbitMQ para estudiante {estudiante.EstudianteId}");
            }
            catch (Exception ex)
            {
                // Solo log el error de RabbitMQ, pero la matrícula ya está guardada
                Console.WriteLine($"[API ERROR] Error al publicar en RabbitMQ: {ex.Message}");
            }

            var respuesta = new MatriculaRespuestaDto
            {
                MatriculaId = nuevaMatricula.MatriculaId,
                EstudianteId = nuevaMatricula.EstudianteId,
                SeccionId = nuevaMatricula.SeccionId,
                Costo = nuevaMatricula.Costo,
                Estado = nuevaMatricula.Estado,
                NombreCompletoEstudiante = $"{estudiante.Nombre} {estudiante.Apellido}"
            };

            // ✅ El cliente recibe respuesta INMEDIATA sin esperar el correo
            return CreatedAtAction(nameof(RegistrarMatricula), new { id = respuesta.MatriculaId }, respuesta);
        }
    }
}