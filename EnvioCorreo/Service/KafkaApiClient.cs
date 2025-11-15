using System.Text;
using System.Text.Json;
using EnvioCorreo.Models;
using Microsoft.Extensions.Configuration;

namespace EnvioCorreo.Service
{
    public interface IKafkaApiClient
    {
        Task<bool> SendMatriculaLogAsync(MatriculaLogEvent matriculaLog);
        Task<bool> SendEmailEventAsync(EmailSentEvent emailEvent);
        Task<bool> SendGenericMessageAsync(string topic, string message, string? key = null);
    }

    public class KafkaApiClient : IKafkaApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public KafkaApiClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;

            // Usar host.docker.internal para conectar desde contenedor a localhost
            _baseUrl = configuration["KafkaApiBaseUrl"] ?? "http://host.docker.internal:7070";

            Console.WriteLine($"[KAFKA API CLIENT] Base URL: {_baseUrl}");
        }

        public async Task<bool> SendMatriculaLogAsync(MatriculaLogEvent matriculaLog)
        {
            try
            {
                var message = new
                {
                    matriculaLog.MatriculaId,
                    matriculaLog.EstudianteId,
                    matriculaLog.SeccionId,
                    matriculaLog.Costo,
                    matriculaLog.MetodoPago,
                    matriculaLog.Estado,
                    FechaMatricula = matriculaLog.FechaMatricula.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    matriculaLog.EventType,
                    matriculaLog.Message
                };

                var json = JsonSerializer.Serialize(message);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/kafka/matricula-log", content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[KAFKA API] Matricula log sent successfully: {matriculaLog.MatriculaId}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[KAFKA API ERROR] HTTP {response.StatusCode}: {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KAFKA API ERROR] {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendEmailEventAsync(EmailSentEvent emailEvent)
        {
            try
            {
                var message = new
                {
                    emailEvent.MatriculaId,
                    emailEvent.To,
                    emailEvent.Subject,
                    emailEvent.Body,
                    SentDate = emailEvent.SentDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    emailEvent.EventType
                };

                var json = JsonSerializer.Serialize(message);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/kafka/email-event", content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[KAFKA API] Email event sent successfully: {emailEvent.MatriculaId}");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[KAFKA API ERROR] HTTP {response.StatusCode}: {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KAFKA API ERROR] {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendGenericMessageAsync(string topic, string message, string? key = null)
        {
            try
            {
                var request = new
                {
                    Topic = topic,
                    Data = message,
                    Key = key
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/kafka/message", content);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KAFKA API ERROR] {ex.Message}");
                return false;
            }
        }
    }
}