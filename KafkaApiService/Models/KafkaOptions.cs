namespace KafkaApiService.Models
{
    public class KafkaOptions
    {
        public string BootstrapServers { get; set; } = "localhost:9092";
        public string TopicMatriculaLogs { get; set; } = "matricula-logs";
        public string TopicEmailEvents { get; set; } = "email-events";
        public string GroupId { get; set; } = "kafka-api-group";
    }
}
