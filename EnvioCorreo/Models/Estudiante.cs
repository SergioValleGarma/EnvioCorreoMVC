namespace EnvioCorreo.Models
{
    public class Estudiante
    {
        public int EstudianteId { get; set; } // SQL Server: IDENTITY(1,1)
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Dni { get; set; }
        public string Email { get; set; } // ¡CLAVE para el email!
        public string Telefono { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public string Direccion { get; set; }
        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public bool Activo { get; set; } = true;

        // Propiedad de navegación
        public ICollection<Matricula> Matriculas { get; set; }
    }
}
