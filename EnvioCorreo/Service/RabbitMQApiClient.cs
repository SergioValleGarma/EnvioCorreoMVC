using System.Text;
using System.Text.Json;
using EnvioCorreo.Models;
using Microsoft.Extensions.Configuration;

namespace EnvioCorreo.Service
{
    public interface IRabbitMQApiClient
    {
        Task<bool> PublishEmailMessageAsync(EmailSentEvent emailEvent);
        Task<bool> PublishGenericMessageAsync(string queueName, string message);
    }

    public class RabbitMQApiClient : IRabbitMQApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public RabbitMQApiClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;

            // Usar variable de entorno o valor por defecto
            _baseUrl = configuration["RabbitMQApiBaseUrl"] ??
                      (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
                          ? "http://rabbitmq-api:8080"
                          : "http://localhost:7080");

            Console.WriteLine($"[RABBITMQ API CLIENT] Base URL: {_baseUrl}");
        }

        public async Task<bool> PublishEmailMessageAsync(EmailSentEvent emailEvent)
        {
            try
            {
                var message = new
                {
                    emailEvent.EstudianteId,
                    emailEvent.SeccionId,
                    emailEvent.MatriculaId,
                    emailEvent.To,
                    emailEvent.Subject,
                    emailEvent.Body,
                    SentDate = emailEvent.SentDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    emailEvent.EventType,
                    emailEvent.MessageType
                };

                var json = JsonSerializer.Serialize(message);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/rabbitmq/email", content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[RABBITMQ API] Email message sent successfully: {emailEvent.MatriculaId}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[RABBITMQ API ERROR] HTTP {response.StatusCode}: {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RABBITMQ API ERROR] {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PublishGenericMessageAsync(string queueName, string message)
        {
            try
            {
                var request = new
                {
                    QueueName = queueName,
                    Message = message
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/rabbitmq/message", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RABBITMQ API ERROR] {ex.Message}");
                return false;
            }
        }
    }
}
