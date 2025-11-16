using EnvioCorreo.Models;

namespace EnvioCorreo.Service
{
    public class RabbitMQApiClientService : IMessageQueueService
    {
        private readonly IRabbitMQApiClient _rabbitMQApiClient;
        private bool _disposed = false;

        public RabbitMQApiClientService(IRabbitMQApiClient rabbitMQApiClient)
        {
            _rabbitMQApiClient = rabbitMQApiClient;
        }

        public void PublishEmailSentMessage(EmailSentEvent message)
        {
            // Ejecutar de forma asíncrona pero no esperar (fire and forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _rabbitMQApiClient.PublishEmailMessageAsync(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RABBITMQ API CLIENT SERVICE ERROR] {ex.Message}");
                }
            });
        }

        // Implementación del método Dispose
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Liberar recursos administrados aquí si los hay
                    Console.WriteLine($"[RABBITMQ API CLIENT SERVICE] Disposing resources");
                }

                _disposed = true;
            }
        }
    }
}