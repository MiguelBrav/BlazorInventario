using System.Threading.Tasks;

namespace BlazorInventario.Services
{
    public interface IKardexExportService
    {
        Task<byte[]> GenerateKardexCsvAsync(int productId, string productName, System.DateTime? from, System.DateTime? to, string? type);
    }
}