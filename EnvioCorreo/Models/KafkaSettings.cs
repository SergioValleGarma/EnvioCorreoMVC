using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnvioCorreo.Models
{
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; } = "localhost:9092";
        public string TopicMatriculaLogs { get; set; } = "matricula-logs";
        public string TopicEmailEvents { get; set; } = "email-events";
        public string GroupId { get; set; } = "envio-correo-group";
    }
}
