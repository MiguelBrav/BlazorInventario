using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;

namespace BlazorInventario.Repositories
{
    public interface IMovementsRepository
    {
        Task<long> CreateAsync(MovementRecord movement, IDbTransaction? tx = null);
        Task<MovementRecord?> GetByIdAsync(long id);
        Task CancelAsync(long id, IDbTransaction? tx = null);
        Task<bool> HasMovementsAsync(int productId);
        Task<IEnumerable<MovementRecord>> GetByFiltersAsync(DateTime? from, DateTime? to, int? productId, string? type);
        Task<IEnumerable<MovementRecord>> GetRecentAsync(int limit);
    }
}
