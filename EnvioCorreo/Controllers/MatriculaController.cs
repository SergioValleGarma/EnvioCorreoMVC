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
        private readonly IMessageQueueService _messageQueueService;
        private readonly IKafkaProducerService _kafkaProducerService;

        public MatriculaController(
            ApplicationDbContext context,
            IMessageQueueService messageQueueService,
            IKafkaProducerService kafkaProducerService)
        {
            _context = context;
            _messageQueueService = messageQueueService;
            _kafkaProducerService = kafkaProducerService;
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

            // 4. ✅ ENVIAR LOG DE MATRÍCULA A KAFKA
            try
            {
                var matriculaLog = new MatriculaLogEvent
                {
                    MatriculaId = nuevaMatricula.MatriculaId,
                    EstudianteId = nuevaMatricula.EstudianteId,
                    SeccionId = nuevaMatricula.SeccionId,
                    Costo = nuevaMatricula.Costo,
                    MetodoPago = nuevaMatricula.MetodoPago,
                    Estado = nuevaMatricula.Estado,
                    FechaMatricula = nuevaMatricula.FechaMatricula,
                    EventType = "MATRICULA_REGISTRADA",
                    Message = $"Matrícula registrada exitosamente para estudiante {estudiante.Nombre} {estudiante.Apellido}"
                };

                var kafkaSuccess = await _kafkaProducerService.ProduceMatriculaLogAsync(matriculaLog);

                if (kafkaSuccess)
                {
                    Console.WriteLine($"[CONTROLLER] Log de matrícula enviado a Kafka: {nuevaMatricula.MatriculaId}");
                }
                else
                {
                    Console.WriteLine($"[CONTROLLER WARNING] No se pudo enviar log a Kafka: {nuevaMatricula.MatriculaId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CONTROLLER ERROR] Error al enviar a Kafka: {ex.Message}");
                // No falla la matrícula si Kafka falla
            }

            // 5. ✅ PUBLICAR EN RABBITMQ PARA ENVÍO DE CORREO
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
                    MatriculaId = nuevaMatricula.MatriculaId,
                    To = estudiante.Email,
                    Subject = asunto,
                    Body = cuerpo,
                    Timestamp = DateTime.UtcNow,
                    MessageType = "EmailPending"
                };

                _messageQueueService.PublishEmailSentMessage(emailEvent);
                Console.WriteLine($"[CONTROLLER] Mensaje publicado en RabbitMQ para estudiante {estudiante.EstudianteId}");

                // 6. ✅ OPCIONAL: ENVIAR EVENTO DE EMAIL A KAFKA TAMBIÉN
                try
                {
                    await _kafkaProducerService.ProduceEmailEventAsync(emailEvent);
                    Console.WriteLine($"[CONTROLLER] Evento de email enviado a Kafka también");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CONTROLLER WARNING] No se pudo enviar evento de email a Kafka: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CONTROLLER ERROR] Error al publicar en RabbitMQ: {ex.Message}");
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

            return CreatedAtAction(nameof(RegistrarMatricula), new { id = respuesta.MatriculaId }, respuesta);
        }
    }
}