using EnvioCorreo.Models;

namespace EnvioCorreo.Service
{
    public interface IMessageQueueService : IDisposable  // ← Verifica si hereda de IDisposable
    {
        void PublishEmailSentMessage(EmailSentEvent message);
    }
}