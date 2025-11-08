using System.ComponentModel.DataAnnotations;

namespace EnvioCorreo.Models
{
    // Models/ContactoDto.cs (o donde guardes tus DTOs)
    public class ContactoDto
    {
        [Required]
        public string Nombre { get; set; }

        [Required]
        [EmailAddress]
        public string EmailUsuario { get; set; }

        [Required]
        public string Mensaje { get; set; }
    }
}
