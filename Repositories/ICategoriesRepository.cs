using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorInventario.Repositories
{
    public interface ICategoriesRepository
    {
        Task<int> CreateAsync(CategoryRecord category);
        Task<CategoryRecord?> GetByIdAsync(int id);
        Task<IEnumerable<CategoryRecord>> GetAllAsync();
        Task UpdateAsync(CategoryRecord category);
        Task DeleteAsync(int id);
    }
}
