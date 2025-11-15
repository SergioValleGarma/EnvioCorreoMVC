namespace KafkaApiService.Models
{
    public class EmailEventMessage
    {
        public int EstudianteId { get; set; }
        public int SeccionId { get; set; }
        public int MatriculaId { get; set; }
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime SentDate { get; set; }
        public string EventType { get; set; } = "EMAIL_SENT";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string MessageType { get; set; } = "EmailPending";
    }
}
