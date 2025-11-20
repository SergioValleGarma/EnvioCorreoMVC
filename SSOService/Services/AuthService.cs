using SSOService.Data;
using SSOService.DTOs;
using SSOService.Models;

namespace SSOService.Services
{
    public class AuthService : IAuthService
    {
        private readonly SSODbContext _context;
        private readonly ITokenService _tokenService;

        public AuthService(SSODbContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = _context.Users.FirstOrDefault(u =>
                (u.Username == request.Username || u.Email == request.Username) &&
                u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return new AuthResponse { Success = false, Message = "Credenciales inválidas" };
            }

            // Actualizar último login
            user.LastLogin = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Generar tokens
            var token = _tokenService.GenerateJwtToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Guardar refresh token
            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.UserId,
                Token = refreshToken,
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                Success = true,
                Message = "Login exitoso",
                Token = token,
                RefreshToken = refreshToken,
                Expires = DateTime.UtcNow.AddMinutes(60),
                User = new UserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role,
                    LastLogin = user.LastLogin
                }
            };
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            // Verificar si el usuario ya existe
            if (_context.Users.Any(u => u.Username == request.Username))
            {
                return new AuthResponse { Success = false, Message = "El nombre de usuario ya existe" };
            }

            if (_context.Users.Any(u => u.Email == request.Email))
            {
                return new AuthResponse { Success = false, Message = "El email ya está registrado" };
            }

            // Crear nuevo usuario
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                Role = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generar tokens
            var token = _tokenService.GenerateJwtToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.UserId,
                Token = refreshToken,
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                Success = true,
                Message = "Usuario registrado exitosamente",
                Token = token,
                RefreshToken = refreshToken,
                Expires = DateTime.UtcNow.AddMinutes(60),
                User = new UserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role
                }
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            if (_tokenService.ValidateRefreshToken(refreshToken, out int userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null && user.IsActive)
                {
                    var newToken = _tokenService.GenerateJwtToken(user);
                    var newRefreshToken = _tokenService.GenerateRefreshToken();

                    // Revocar el refresh token anterior
                    _tokenService.RevokeRefreshToken(refreshToken);

                    // Guardar nuevo refresh token
                    _context.RefreshTokens.Add(new RefreshToken
                    {
                        UserId = user.UserId,
                        Token = newRefreshToken,
                        Expires = DateTime.UtcNow.AddDays(7),
                        Created = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();

                    return new AuthResponse
                    {
                        Success = true,
                        Token = newToken,
                        RefreshToken = newRefreshToken,
                        Expires = DateTime.UtcNow.AddMinutes(60),
                        User = new UserDto
                        {
                            UserId = user.UserId,
                            Username = user.Username,
                            Email = user.Email,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Role = user.Role
                        }
                    };
                }
            }

            return new AuthResponse { Success = false, Message = "Refresh token inválido" };
        }

        public async Task<bool> LogoutAsync(string refreshToken)
        {
            _tokenService.RevokeRefreshToken(refreshToken);
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<bool> ValidateTokenAsync(string token)
        {
            // Implementación simple - en producción usaría JwtSecurityTokenHandler
            return Task.FromResult(!string.IsNullOrEmpty(token));
        }
    }
}
