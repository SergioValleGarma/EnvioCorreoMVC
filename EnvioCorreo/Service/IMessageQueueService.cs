using EnvioCorreo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnvioCorreo.Service
{
    public interface IMessageQueueService
    {
        void PublishEmailSentMessage(EmailSentEvent message);
        void Dispose();
    }
}