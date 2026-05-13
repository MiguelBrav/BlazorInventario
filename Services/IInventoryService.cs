using System.Threading.Tasks;
using BlazorInventario.Repositories;

namespace BlazorInventario.Services
{
    public interface IInventoryService
    {
        Task<long> CreateEntryAsync(MovementRecord movement);
        Task<long> CreateExitAsync(MovementRecord movement);
        Task CancelMovementAsync(long movementId);
        Task RecalculateProductCostsAsync(int productId);
    }
}
