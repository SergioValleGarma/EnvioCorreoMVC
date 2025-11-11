namespace EnvioCorreo.Models
{
    public class EmailSentEvent
    {
        public int EstudianteId { get; set; }
        public int SeccionId { get; set; }
        public int MatriculaId { get; set; } // ✅ NUEVO: ID de matrícula
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string MessageType { get; set; } = "EmailPending"; // ✅ Cambiado a "Pending"

        // Propiedades para información del correo
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime SentDate { get; set; }
    }
}