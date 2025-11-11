using EnvioCorreo.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EnvioCorreo.Service
{
    public class EmailConsumerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitMQSettings _settings;
        private IConnection _connection;
        private IModel _channel;
        private const string QueueName = "email_sent_queue";

        public EmailConsumerService(IServiceProvider serviceProvider, IOptions<RabbitMQSettings> settings)
        {
            _serviceProvider = serviceProvider;
            _settings = settings.Value;
            InitializeRabbitMQ();
        }

        private void InitializeRabbitMQ()
        {
            try
            {
                Console.WriteLine($"[CONSUMER] Inicializando conexión RabbitMQ...");

                var factory = new ConnectionFactory()
                {
                    HostName = _settings.HostName,
                    UserName = _settings.UserName,
                    Password = _settings.Password,
                    Port = _settings.Port
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                // Asegurar que la cola existe
                _channel.QueueDeclare(
                    queue: QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                Console.WriteLine($"[CONSUMER] Conectado a RabbitMQ y listo para consumir mensajes");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CONSUMER ERROR] Error al inicializar RabbitMQ: {ex.Message}");
                throw;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"[CONSUMER] Iniciando consumo de mensajes...");

            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                try
                {
                    Console.WriteLine($"[CONSUMER] Mensaje recibido: {message}");

                    // Procesar el mensaje
                    await ProcessMessageAsync(message);

                    // Confirmar que el mensaje fue procesado exitosamente
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

                    Console.WriteLine($"[CONSUMER] Mensaje procesado y confirmado");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CONSUMER ERROR] Error procesando mensaje: {ex.Message}");

                    // Rechazar el mensaje y requeue (reintentar)
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            // Comenzar a consumir mensajes
            _channel.BasicConsume(
                queue: QueueName,
                autoAck: false, // IMPORTANTE: false para confirmar manualmente
                consumer: consumer
            );

            Console.WriteLine($"[CONSUMER] Esperando mensajes...");

            // Mantener el servicio corriendo
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task ProcessMessageAsync(string messageJson)
        {
            try
            {
                var emailEvent = JsonSerializer.Deserialize<EmailSentEvent>(messageJson);

                if (emailEvent == null)
                {
                    Console.WriteLine($"[CONSUMER ERROR] No se pudo deserializar el mensaje");
                    return;
                }

                Console.WriteLine($"[CONSUMER] Procesando correo para: {emailEvent.To}");

                // Usar scope para obtener IEmailService
                using (var scope = _serviceProvider.CreateScope())
                {
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    // Enviar el correo
                    await emailService.SendEmailAsync(emailEvent.To, emailEvent.Subject, emailEvent.Body);

                    Console.WriteLine($"[CONSUMER] Correo enviado exitosamente a: {emailEvent.To}");

                    // Actualizar el evento como enviado
                    emailEvent.MessageType = "EmailSent";
                    emailEvent.Timestamp = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CONSUMER ERROR] Error en ProcessMessageAsync: {ex.Message}");
                throw;
            }
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
