using EnvioCorreo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnvioCorreo.Service
{
    public interface IKafkaProducerService
    {
        Task<bool> ProduceMatriculaLogAsync(MatriculaLogEvent logEvent);
        Task<bool> ProduceEmailEventAsync(EmailSentEvent emailEvent);
        void Dispose();
    }
}
