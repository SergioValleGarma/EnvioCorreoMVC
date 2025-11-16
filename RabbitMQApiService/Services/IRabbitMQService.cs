using RabbitMQApiService.Models;

namespace RabbitMQApiService.Services
{
    public interface IRabbitMQService
    {
        Task<bool> PublishEmailMessageAsync(EmailMessage message);
        Task<bool> PublishGenericMessageAsync(string queueName, string message);
    }
}
