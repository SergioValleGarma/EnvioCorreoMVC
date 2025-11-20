using System.ComponentModel.DataAnnotations;

namespace SSOService.DTOs
{
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
