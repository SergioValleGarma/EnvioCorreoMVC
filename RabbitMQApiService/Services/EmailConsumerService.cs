using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQApiService.Models;

namespace RabbitMQApiService.Services
{
    public class EmailConsumerService : BackgroundService
    {
        private readonly RabbitMQOptions _options;
        private readonly IEmailService _emailService;
        private IConnection? _connection;
        private IModel? _channel;

        public EmailConsumerService(IOptions<RabbitMQOptions> options, IEmailService emailService)
        {
            _options = options.Value;
            _emailService = emailService;
            InitializeRabbitMQ();
        }

        private void InitializeRabbitMQ()
        {
            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = _options.HostName,
                    UserName = _options.UserName,
                    Password = _options.Password,
                    Port = _options.Port
                };

                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(
                    queue: _options.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                Console.WriteLine($"[RABBITMQ API CONSUMER] Conectado a RabbitMQ: {_options.HostName}:{_options.Port}");
                Console.WriteLine($"[RABBITMQ API CONSUMER] Consumiendo de la cola: {_options.QueueName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RABBITMQ API CONSUMER ERROR] Error al conectar: {ex.Message}");
                // No lanzar excepción, reintentar en ExecuteAsync
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Esperar a que RabbitMQ esté listo
            await Task.Delay(10000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_channel == null || !_channel.IsOpen)
                    {
                        Console.WriteLine("[RABBITMQ API CONSUMER] Reconectando a RabbitMQ...");
                        InitializeRabbitMQ();
                    }

                    if (_channel != null && _channel.IsOpen)
                    {
                        var consumer = new EventingBasicConsumer(_channel);
                        consumer.Received += async (model, ea) =>
                        {
                            try
                            {
                                var body = ea.Body.ToArray();
                                var message = Encoding.UTF8.GetString(body);
                                Console.WriteLine($"[RABBITMQ API CONSUMER] Mensaje recibido: {message}");

                                // Procesar el mensaje
                                var emailMessage = JsonSerializer.Deserialize<EmailMessage>(message);
                                if (emailMessage != null)
                                {
                                    Console.WriteLine($"[RABBITMQ API CONSUMER] Procesando email para: {emailMessage.To}");

                                    await _emailService.SendEmailAsync(
                                        emailMessage.To,
                                        emailMessage.Subject,
                                        emailMessage.Body
                                    );

                                    Console.WriteLine($"[RABBITMQ API CONSUMER] Email procesado exitosamente: {emailMessage.To}");
                                }

                                // Confirmar el mensaje
                                _channel.BasicAck(ea.DeliveryTag, false);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[RABBITMQ API CONSUMER ERROR] Error procesando mensaje: {ex.Message}");
                                // Rechazar el mensaje (no reintentar)
                                _channel.BasicNack(ea.DeliveryTag, false, false);
                            }
                        };

                        _channel.BasicConsume(
                            queue: _options.QueueName,
                            autoAck: false,
                            consumer: consumer
                        );

                        Console.WriteLine($"[RABBITMQ API CONSUMER] Esperando mensajes...");
                        break; // Salir del bucle si se conectó exitosamente
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RABBITMQ API CONSUMER ERROR] Error general: {ex.Message}");
                }

                await Task.Delay(5000, stoppingToken);
            }

            // Mantener el servicio corriendo
            await Task.Delay(Timeout.Infinite, stoppingToken);
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