namespace KafkaApiService.Models
{
    public class MatriculaLogMessage
    {
        public int MatriculaId { get; set; }
        public int EstudianteId { get; set; }
        public int SeccionId { get; set; }
        public decimal Costo { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaMatricula { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string EventType { get; set; } = "MATRICULA_REGISTRADA";
        public string Message { get; set; } = string.Empty;
    }
}
