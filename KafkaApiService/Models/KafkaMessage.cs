namespace KafkaApiService.Models
{
    public class KafkaMessage
    {
        public string Topic { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public string? Key { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
