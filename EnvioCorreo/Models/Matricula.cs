
namespace EnvioCorreo.Models
{
    public class Matricula
    {
        public int MatriculaId { get; set; } // SQL Server: IDENTITY(1,1)
        public int EstudianteId { get; set; }
        public int SeccionId { get; set; }
        public DateTime FechaMatricula { get; set; } = DateTime.Now;
        public string Estado { get; set; } = "PENDIENTE";
        public decimal Costo { get; set; }
        public string MetodoPago { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Propiedades de navegación
        public Estudiante Estudiante { get; set; } // Necesario para acceder al email
        public Seccion Seccion { get; set; }
    }
}
