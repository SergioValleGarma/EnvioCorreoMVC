using EnvioCorreo.Models;
using Microsoft.EntityFrameworkCore;

namespace EnvioCorreo.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Estudiante> Estudiantes { get; set; }
        public DbSet<Matricula> Matriculas { get; set; }
        public DbSet<Seccion> Secciones { get; set; }
        // ... DbSet para Curso y Profesor

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuración de clave compuesta (Unique)
            modelBuilder.Entity<Matricula>()
                .HasIndex(m => new { m.EstudianteId, m.SeccionId })
                .IsUnique();

            // Aquí puedes añadir más configuraciones basadas en tu DDL
            modelBuilder.Entity<Matricula>()
                .Property(m => m.Costo)
                .HasColumnType("decimal(18, 2)");

            // Configuración de clave compuesta (Unique)
            modelBuilder.Entity<Matricula>()
                .HasIndex(m => new { m.EstudianteId, m.SeccionId })
                .IsUnique();
        }
    }
}
