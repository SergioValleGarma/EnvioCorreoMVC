namespace EnvioCorreo.Models
{
    public class Seccion
    {
        public int SeccionId { get; set; } // SQL Server: IDENTITY(1,1)
        public int CursoId { get; set; }
        public int ProfesorId { get; set; }
        public string Codigo { get; set; }
        public string Horario { get; set; }
        // ... otras propiedades

        // Propiedad de navegación
        public ICollection<Matricula> Matriculas { get; set; }
    }
}
