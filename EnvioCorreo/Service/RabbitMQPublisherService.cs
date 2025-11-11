using System.Text;
using EnvioCorreo.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text.Json;

namespace EnvioCorreo.Service
{
    public class RabbitMQPublisherService : IMessageQueueService, IDisposable
    {
        private readonly RabbitMQSettings _settings;
        private IConnection _connection;
        private IModel _channel;
        private const int MaxRetries = 10;
        private const int DelayInSeconds = 5;
        private bool _disposed = false;

        public RabbitMQPublisherService(IOptions<RabbitMQSettings> settings)
        {
            _settings = settings.Value;
            Console.WriteLine($"[RABBITMQ DEBUG] Config loaded - Host: {_settings.HostName}, Queue: {_settings.QueueName}");
            InitializeRabbitMQ();
        }

        private void InitializeRabbitMQ()
        {
            Console.WriteLine($"[RABBITMQ DEBUG] Starting initialization...");

            var factory = new ConnectionFactory()
            {
                HostName = _settings.HostName,
                UserName = _settings.UserName,
                Password = _settings.Password,
                Port = 5672
            };

            Console.WriteLine($"[RABBITMQ DEBUG] Factory created for host: {_settings.HostName}");

            int retries = 0;
            while (retries < MaxRetries)
            {
                try
                {
                    Console.WriteLine($"[RABBITMQ] Intentando conectar al broker... Intento {retries + 1}/{MaxRetries}");
                    Console.WriteLine($"[RABBITMQ DEBUG] ConnectionFactory: {factory.HostName}:{factory.Port}");

                    _connection = factory.CreateConnection();
                    Console.WriteLine($"[RABBITMQ DEBUG] Connection created successfully");

                    _channel = _connection.CreateModel();
                    Console.WriteLine($"[RABBITMQ DEBUG] Channel created successfully");

                    // Declara la cola
                    _channel.QueueDeclare(
                        queue: _settings.QueueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null
                    );

                    Console.WriteLine($"[RABBITMQ] Conexión establecida y cola '{_settings.QueueName}' declarada.");
                    Console.WriteLine($"[RABBITMQ DEBUG] Connection IsOpen: {_connection.IsOpen}, Channel IsOpen: {_channel.IsOpen}");
                    return; // Salir si es exitoso
                }
                catch (Exception ex)
                {
                    retries++;
                    Console.WriteLine($"[RABBITMQ ERROR] Fallo en la conexión: {ex.Message}");
                    Console.WriteLine($"[RABBITMQ ERROR] StackTrace: {ex.StackTrace}");

                    // Limpiar recursos en caso de error
                    CleanupResources();

                    if (retries < MaxRetries)
                    {
                        Console.WriteLine($"[RABBITMQ] Reintentando en {DelayInSeconds} segundos...");
                        Thread.Sleep(DelayInSeconds * 1000);
                    }
                }
            }

            Console.WriteLine("[RABBITMQ FATAL] Fallo la conexión después de múltiples intentos.");
        }

        private void CleanupResources()
        {
            try
            {
                if (_channel != null)
                {
                    if (_channel.IsOpen)
                        _channel.Close();
                    _channel.Dispose();
                    _channel = null;
                    Console.WriteLine($"[RABBITMQ DEBUG] Channel cleaned up");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RABBITMQ WARNING] Error al limpiar el canal: {ex.Message}");
            }

            try
            {
                if (_connection != null)
                {
                    if (_connection.IsOpen)
                        _connection.Close();
                    _connection.Dispose();
                    _connection = null;
                    Console.WriteLine($"[RABBITMQ DEBUG] Connection cleaned up");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RABBITMQ WARNING] Error al limpiar la conexión: {ex.Message}");
            }
        }

        public void PublishEmailSentMessage(EmailSentEvent message)
        {
            Console.WriteLine($"[RABBITMQ DEBUG] PublishEmailSentMessage called");

            if (_disposed)
                throw new ObjectDisposedException(nameof(RabbitMQPublisherService));

            // Verifica que el canal esté abierto antes de publicar
            if (_channel == null || !_channel.IsOpen)
            {
                Console.WriteLine("Advertencia: El canal de RabbitMQ no está abierto. Intentando reconectar...");
                InitializeRabbitMQ();

                if (_channel == null || !_channel.IsOpen)
                {
                    Console.WriteLine("Error: No se pudo establecer conexión con RabbitMQ. El mensaje no se publicó.");
                    return;
                }
            }

            try
            {
                var jsonMessage = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(jsonMessage);

                Console.WriteLine($"[RABBITMQ DEBUG] Publishing message to queue: {_settings.QueueName}");

                _channel.BasicPublish(
                    exchange: string.Empty,
                    routingKey: _settings.QueueName,
                    basicProperties: null,
                    body: body
                );

                Console.WriteLine($"[RABBITMQ] Publicado: {jsonMessage}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RABBITMQ ERROR] Error al publicar mensaje: {ex.Message}");
                Console.WriteLine($"[RABBITMQ ERROR] StackTrace: {ex.StackTrace}");
                // Intentar reconectar para el próximo mensaje
                InitializeRabbitMQ();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Console.WriteLine($"[RABBITMQ DEBUG] Disposing resources");
                CleanupResources();
                _disposed = true;
            }
        }
    }
}