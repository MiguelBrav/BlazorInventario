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
        
        // Kardex-specific methods
        Task<IEnumerable<KardexRecord>> GetKardexAsync(int productId, DateTime? from, DateTime? to, string? type, int page, int pageSize);
        Task<int> GetKardexCountAsync(int productId, DateTime? from, DateTime? to, string? type);
    }

    public class KardexRecord
    {
        public DateTime date { get; set; }
        public string? type { get; set; }
        public string? notes { get; set; }
        public long id { get; set; }
        public int quantity { get; set; }
        public decimal unit_cost { get; set; }
        public int running_stock { get; set; }
        public decimal running_average_cost { get; set; }
        public string? user_name { get; set; }
        public string? supplier_name { get; set; }
        public bool canceled { get; set; }
    }
}
