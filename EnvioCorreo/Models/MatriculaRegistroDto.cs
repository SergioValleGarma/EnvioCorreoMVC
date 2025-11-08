// Models/MatriculaRegistroDto.cs
using System.ComponentModel.DataAnnotations;

namespace EnvioCorreo.Models
{
    public class MatriculaRegistroDto
    {
        [Required]
        public int EstudianteId { get; set; }

        [Required]
        public int SeccionId { get; set; }

        [Required]
        [Range(0.01, 10000.00)]
        public decimal Costo { get; set; }

        public string MetodoPago { get; set; }
    }
}
