using SSOService.Models;

namespace SSOService.Services
{
    public interface ITokenService
    {
        string GenerateJwtToken(User user);
        string GenerateRefreshToken();
        bool ValidateRefreshToken(string refreshToken, out int userId);
        void RevokeRefreshToken(string refreshToken);
    }
}
