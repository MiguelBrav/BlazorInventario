using System.Threading.Tasks;
using BlazorInventario.Repositories;

namespace BlazorInventario.Services
{
    public interface IAuthService
    {
        Task<UserRecord?> ValidateCredentialsAsync(string email, string password);
    }
}
