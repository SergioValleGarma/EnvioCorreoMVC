namespace RabbitMQApiService.Models
{
    public class EmailMessage
    {
        public int? EstudianteId { get; set; }
        public int? SeccionId { get; set; }
        public int? MatriculaId { get; set; }
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime SentDate { get; set; }
        public string? EventType { get; set; }
        public string? MessageType { get; set; }
    }
}
