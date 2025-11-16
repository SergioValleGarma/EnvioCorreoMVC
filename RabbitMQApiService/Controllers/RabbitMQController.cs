using Microsoft.AspNetCore.Mvc;
using RabbitMQApiService.Models;
using RabbitMQApiService.Services;

namespace RabbitMQApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RabbitMQController : ControllerBase
    {
        private readonly IRabbitMQService _rabbitMQService;

        public RabbitMQController(IRabbitMQService rabbitMQService)
        {
            _rabbitMQService = rabbitMQService;
        }

        [HttpPost("email")]
        public async Task<IActionResult> PublishEmailMessage([FromBody] EmailMessage message)
        {
            try
            {
                var success = await _rabbitMQService.PublishEmailMessageAsync(message);

                return Ok(new
                {
                    success = success,
                    message = success ? "Email message published to RabbitMQ" : "Failed to publish message",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("message")]
        public async Task<IActionResult> PublishGenericMessage([FromBody] GenericMessageRequest request)
        {
            try
            {
                var success = await _rabbitMQService.PublishGenericMessageAsync(request.QueueName, request.Message);

                return Ok(new
                {
                    success = success,
                    message = success ? "Message published to RabbitMQ" : "Failed to publish message",
                    queue = request.QueueName,
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
                service = "RabbitMQ API Service",
                timestamp = DateTime.UtcNow
            });
        }
    }

    public class GenericMessageRequest
    {
        public string QueueName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
