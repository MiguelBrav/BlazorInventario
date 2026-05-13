using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;

namespace BlazorInventario.Repositories
{
    public interface IProductsRepository
    {
        Task<int> CreateAsync(ProductRecord product);
        Task<ProductRecord?> GetByIdAsync(int id);
        Task<IEnumerable<ProductRecord>> GetAllAsync();
        Task UpdateAsync(ProductRecord product);
        Task DeleteAsync(int id);

        // Logical delete / mark as inactive
        Task MarkAsInactiveAsync(int id);

        // Updates stock and average cost (used in transactions)
        Task UpdateStockAndAverageCostAsync(int productId, int newStock, decimal newAverageCost, IDbTransaction? tx = null);
    }
}
