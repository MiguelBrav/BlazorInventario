using System.Threading.Tasks;
using BlazorInventario.Repositories;

namespace BlazorInventario.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserRecord?> ValidateCredentialsAsync(string email, string password)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user is null || !user.is_active)
            {
                return null;
            }

            try
            {
                var ok = BCrypt.Net.BCrypt.Verify(password, user.password_hash ?? string.Empty);
                return ok ? user : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
