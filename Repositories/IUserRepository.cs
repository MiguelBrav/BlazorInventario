using System.Threading.Tasks;
using System.Collections.Generic;

namespace BlazorInventario.Repositories
{
    public interface IUserRepository
    {
        Task<UserRecord?> GetByEmailAsync(string email);
        Task<IEnumerable<UserRecord>> GetAllAsync();
        Task<UserRecord?> GetByIdAsync(int id);
        Task<int> CreateAsync(UserRecord user);
        Task UpdateAsync(UserRecord user);
        Task DeactivateAsync(int id);
    }
}
