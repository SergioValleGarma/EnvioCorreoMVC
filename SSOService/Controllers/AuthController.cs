using Microsoft.AspNetCore.Mvc;
using SSOService.DTOs;
using SSOService.Services;

namespace SSOService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);

            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }

        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);

            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<ActionResult> Logout([FromBody] LogoutRequest request)
        {
            await _authService.LogoutAsync(request.RefreshToken);
            return Ok(new { Success = true, Message = "Logout exitoso" });
        }

        [HttpPost("validate")]
        public async Task<ActionResult> ValidateToken([FromBody] ValidateTokenRequest request)
        {
            var isValid = await _authService.ValidateTokenAsync(request.Token);
            return Ok(new { Valid = isValid });
        }

        [HttpGet("health")]
        public ActionResult Health()
        {
            return Ok(new { Status = "SSO Service is running", Timestamp = DateTime.UtcNow });
        }
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; }
    }

    public class LogoutRequest
    {
        public string RefreshToken { get; set; }
    }

    public class ValidateTokenRequest
    {
        public string Token { get; set; }
    }
}
