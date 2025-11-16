using System.Text;
using System.Text.Json;
using EnvioCorreo.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EnvioCorreo.Service
{
    public class EmailConsumerService : BackgroundService
    {
        private readonly RabbitMQSettings _settings;
        private readonly IEmailService _emailService;
        private IConnection _connection;
        private IModel _channel;

        public EmailConsumerService(IOptions<RabbitMQSettings> settings, IEmailService emailService)
        {
            _settings = settings.Value;
            _emailService = emailService;
            InitializeRabbitMQ();
        }

        private void InitializeRabbitMQ()
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = _settings.HostName,
                    UserName = _settings.UserName,
                    Password = _settings.Password,
                    Port = _settings.Port
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(
                    queue: _settings.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                Console.WriteLine($"[CONSUMER] Inicializando conexión RabbitMQ...");
                Console.WriteLine($"[CONSUMER] Conectado a RabbitMQ y listo para consumir mensajes");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CONSUMER ERROR] Error al inicializar RabbitMQ: {ex.Message}");
                throw;
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine($"[CONSUMER] Mensaje recibido: {message}");

                    // Deserializar el mensaje
                    var emailEvent = JsonSerializer.Deserialize<EmailSentEvent>(message);
                    if (emailEvent != null)
                    {
                        Console.WriteLine($"[CONSUMER] Procesando correo para: {emailEvent.To}");

                        // Enviar el correo
                        await _emailService.SendEmailAsync(
                            emailEvent.To,
                            emailEvent.Subject,
                            emailEvent.Body
                        );

                        Console.WriteLine($"[CONSUMER] Correo enviado exitosamente a: {emailEvent.To}");
                    }

                    // Confirmar que el mensaje fue procesado
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CONSUMER ERROR] Error procesando mensaje: {ex.Message}");
                    // Rechazar el mensaje para que se reintente
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(
                queue: _settings.QueueName,
                autoAck: false, // IMPORTANTE: false para confirmar manualmente
                consumer: consumer
            );

            Console.WriteLine($"[CONSUMER] Iniciando consumo de mensajes...");
            Console.WriteLine($"[CONSUMER] Esperando mensajes...");

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}