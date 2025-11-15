using KafkaApiService.Models;
using KafkaApiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace KafkaApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KafkaController : ControllerBase
    {
        private readonly IKafkaProducerService _kafkaService;

        public KafkaController(IKafkaProducerService kafkaService)
        {
            _kafkaService = kafkaService;
        }

        [HttpPost("message")]
        public async Task<IActionResult> ProduceMessage([FromBody] KafkaMessage request)
        {
            try
            {
                var success = await _kafkaService.ProduceAsync(request.Topic, request.Data, request.Key);

                return Ok(new
                {
                    success = success,
                    message = success ? "Mensaje enviado a Kafka" : "Error al enviar mensaje",
                    topic = request.Topic,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("matricula-log")]
        public async Task<IActionResult> ProduceMatriculaLog([FromBody] MatriculaLogMessage message)
        {
            try
            {
                var success = await _kafkaService.ProduceMatriculaLogAsync(message);

                return Ok(new
                {
                    success = success,
                    message = success ? "Log de matrícula enviado" : "Error al enviar log",
                    matriculaId = message.MatriculaId,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("email-event")]
        public async Task<IActionResult> ProduceEmailEvent([FromBody] EmailEventMessage message)
        {
            try
            {
                var success = await _kafkaService.ProduceEmailEventAsync(message);

                return Ok(new
                {
                    success = success,
                    message = success ? "Evento de email enviado" : "Error al enviar evento",
                    matriculaId = message.MatriculaId,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                service = "Kafka API Service",
                timestamp = DateTime.UtcNow
            });
        }
    }
}