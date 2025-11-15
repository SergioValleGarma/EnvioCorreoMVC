using KafkaApiService.Models;

namespace KafkaApiService.Services
{
    public interface IKafkaProducerService
    {
        Task<bool> ProduceAsync(string topic, string message, string? key = null);
        Task<bool> ProduceMatriculaLogAsync(MatriculaLogMessage message);
        Task<bool> ProduceEmailEventAsync(EmailEventMessage message);
    }
}
