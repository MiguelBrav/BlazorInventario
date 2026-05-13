using System;
using System.Threading.Tasks;

namespace BlazorInventario.Services
{
    public interface IMovementsExportService
    {
        Task<byte[]> GenerateMovementsCsvAsync(DateTime? from, DateTime? to, int? productId, string? type);
    }
}
