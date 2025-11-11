using Confluent.Kafka;
using EnvioCorreo.Models;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace EnvioCorreo.Service
{
    public class KafkaProducerService : IKafkaProducerService, IDisposable
    {
        private readonly KafkaSettings _settings;
        private readonly IProducer<Null, string> _producer;
        private bool _disposed = false;
        private bool _kafkaAvailable = false;

        public KafkaProducerService(IOptions<KafkaSettings> settings)
        {
            _settings = settings.Value;

            try
            {
                Console.WriteLine($"[KAFKA] Inicializando productor para: {_settings.BootstrapServers}");

                var config = new ProducerConfig
                {
                    BootstrapServers = _settings.BootstrapServers,
                    // Configuración para desarrollo
                    MessageTimeoutMs = 5000,
                    RequestTimeoutMs = 3000,
                    SocketTimeoutMs = 5000,
                    // Configuraciones de rendimiento
                    LingerMs = 5,
                    BatchSize = 16384,
                    Acks = Acks.Leader,
                    EnableIdempotence = false
                };

                _producer = new ProducerBuilder<Null, string>(config)
                    .SetErrorHandler((_, error) =>
                    {
                        Console.WriteLine($"[KAFKA ERROR] Error del productor: {error.Reason}");
                    })
                    .Build();

                _kafkaAvailable = true;
                Console.WriteLine($"[KAFKA] ✅ Productor inicializado exitosamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KAFKA WARNING] ⚠️ No se pudo inicializar Kafka: {ex.Message}");
                Console.WriteLine($"[KAFKA WARNING] ⚠️ La aplicación funcionará sin Kafka");
                _kafkaAvailable = false;
            }
        }

        // ... (el resto de los métodos permanece igual)
        public async Task<bool> ProduceMatriculaLogAsync(MatriculaLogEvent logEvent)
        {
            if (!_kafkaAvailable)
            {
                Console.WriteLine($"[KAFKA WARNING] ⚠️ Kafka no disponible, omitiendo log de matrícula {logEvent.MatriculaId}");
                return false;
            }

            try
            {
                var messageJson = JsonSerializer.Serialize(logEvent, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var message = new Message<Null, string>
                {
                    Value = messageJson
                };

                // Usar Produce en lugar de ProduceAsync para mejor control
                var deliveryResult = await _producer.ProduceAsync(_settings.TopicMatriculaLogs, message);

                Console.WriteLine($"[KAFKA] ✅ Log de matrícula {logEvent.MatriculaId} enviado. Offset: {deliveryResult.Offset}");
                return true;
            }
            catch (ProduceException<Null, string> ex)
            {
                Console.WriteLine($"[KAFKA ERROR] ❌ Error al enviar log de matrícula: {ex.Error.Reason}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KAFKA ERROR] ❌ Error general: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ProduceEmailEventAsync(EmailSentEvent emailEvent)
        {
            if (!_kafkaAvailable)
            {
                Console.WriteLine($"[KAFKA WARNING] ⚠️ Kafka no disponible, omitiendo evento de email");
                return false;
            }

            try
            {
                var messageJson = JsonSerializer.Serialize(emailEvent, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var message = new Message<Null, string>
                {
                    Value = messageJson
                };

                var deliveryResult = await _producer.ProduceAsync(_settings.TopicEmailEvents, message);

                Console.WriteLine($"[KAFKA] ✅ Evento de email enviado. Offset: {deliveryResult.Offset}");
                return true;
            }
            catch (ProduceException<Null, string> ex)
            {
                Console.WriteLine($"[KAFKA ERROR] ❌ Error al enviar evento de email: {ex.Error.Reason}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KAFKA ERROR] ❌ Error general: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    _producer?.Flush(TimeSpan.FromSeconds(5));
                    _producer?.Dispose();
                    Console.WriteLine($"[KAFKA] 🔄 Productor disposed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[KAFKA ERROR] ❌ Error al hacer dispose: {ex.Message}");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }
}