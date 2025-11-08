namespace EnvioCorreo.Models
{
    public class MatriculaRespuestaDto
    {
        public int MatriculaId { get; set; }
        public int EstudianteId { get; set; }
        public int SeccionId { get; set; }
        public decimal Costo { get; set; }
        public string Estado { get; set; }
        // Opcional: Nombre del estudiante, si lo necesitas
        public string NombreCompletoEstudiante { get; set; }
    }
}
