using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQApiService.Models;
using System.Text;
using System.Text.Json;

namespace RabbitMQApiService.Services
{
    public class RabbitMQService : IRabbitMQService, IDisposable
    {
        private readonly RabbitMQOptions _options;
        private IConnection? _connection;
        private IModel? _channel;
        private bool _disposed = false;

        public RabbitMQService(IOptions<RabbitMQOptions> options)
        {
            _options = options.Value;
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

                // Declarar la cola
                _channel.QueueDeclare(
                    queue: _options.QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                Console.WriteLine($"[RABBITMQ API] Conectado a {_options.HostName}:{_options.Port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RABBITMQ API ERROR] Error al conectar: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> PublishEmailMessageAsync(EmailMessage message)
        {
            try
            {
                var jsonMessage = JsonSerializer.Serialize(message);
                return await PublishGenericMessageAsync(_options.QueueName, jsonMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RABBITMQ API ERROR] Error al publicar email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> PublishGenericMessageAsync(string queueName, string message)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (_channel == null || !_channel.IsOpen)
                    {
                        Console.WriteLine("[RABBITMQ API] Canal no disponible, reconectando...");
                        InitializeRabbitMQ();
                    }

                    var body = Encoding.UTF8.GetBytes(message);

                    _channel!.BasicPublish(
                        exchange: string.Empty,
                        routingKey: queueName,
                        basicProperties: null,
                        body: body
                    );

                    Console.WriteLine($"[RABBITMQ API] Mensaje publicado en cola '{queueName}': {message}");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RABBITMQ API ERROR] Error al publicar mensaje: {ex.Message}");
                    return false;
                }
            });
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _channel?.Close();
                _channel?.Dispose();
                _connection?.Close();
                _connection?.Dispose();
                _disposed = true;
            }
        }
    }
}
