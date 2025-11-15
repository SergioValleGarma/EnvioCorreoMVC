using Confluent.Kafka;
using KafkaApiService.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace KafkaApiService.Services
{
    public class KafkaProducerService : IKafkaProducerService, IDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly KafkaOptions _kafkaOptions;

        public KafkaProducerService(IOptions<KafkaOptions> kafkaOptions)
        {
            _kafkaOptions = kafkaOptions.Value;

            var config = new ProducerConfig
            {
                BootstrapServers = _kafkaOptions.BootstrapServers,
                ClientId = "kafka-api-service",
                Acks = Acks.Leader,
                MessageTimeoutMs = 5000,
                RequestTimeoutMs = 3000,
                RetryBackoffMs = 100,
                EnableIdempotence = false
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task<bool> ProduceAsync(string topic, string message, string? key = null)
        {
            try
            {
                var kafkaMessage = new Message<string, string>
                {
                    Key = key ?? Guid.NewGuid().ToString(),
                    Value = message,
                    Timestamp = new Timestamp(DateTime.UtcNow)
                };

                var deliveryResult = await _producer.ProduceAsync(topic, kafkaMessage);

                Console.WriteLine($"[KAFKA] Message delivered to {deliveryResult.Topic} [{deliveryResult.Partition}] at offset {deliveryResult.Offset}");
                return deliveryResult.Status == PersistenceStatus.Persisted;
            }
            catch (ProduceException<string, string> ex)
            {
                Console.WriteLine($"[KAFKA ERROR] Delivery failed: {ex.Error.Reason}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KAFKA ERROR] {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ProduceMatriculaLogAsync(MatriculaLogMessage message)
        {
            try
            {
                var jsonMessage = JsonSerializer.Serialize(new
                {
                    message.MatriculaId,
                    message.EstudianteId,
                    message.SeccionId,
                    message.Costo,
                    message.MetodoPago,
                    message.Estado,
                    FechaMatricula = message.FechaMatricula.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    message.EventType,
                    message.Message,
                    Timestamp = DateTime.UtcNow
                });

                return await ProduceAsync(_kafkaOptions.TopicMatriculaLogs, jsonMessage, message.MatriculaId.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KAFKA ERROR] Error producing matricula log: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ProduceEmailEventAsync(EmailEventMessage message)
        {
            try
            {
                var jsonMessage = JsonSerializer.Serialize(new
                {
                    message.MatriculaId,
                    message.To,
                    message.Subject,
                    message.Body,
                    SentDate = message.SentDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    message.EventType,
                    Timestamp = DateTime.UtcNow
                });

                return await ProduceAsync(_kafkaOptions.TopicEmailEvents, jsonMessage, message.MatriculaId.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KAFKA ERROR] Error producing email event: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            _producer?.Flush(TimeSpan.FromSeconds(5));
            _producer?.Dispose();
        }
    }
}