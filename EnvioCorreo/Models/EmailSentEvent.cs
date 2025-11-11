namespace EnvioCorreo.Models
{
    public class EmailSentEvent
    {
        public int EstudianteId { get; set; }
        public int SeccionId { get; set; }
        public int MatriculaId { get; set; } // ✅ Agregar esto
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string MessageType { get; set; } = "EmailSent";

        // Opcional: mantener las propiedades originales si las necesitas
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime SentDate { get; set; }
    }
}