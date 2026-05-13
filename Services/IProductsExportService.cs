using System;
using System.Threading.Tasks;

namespace BlazorInventario.Services
{
    public interface IProductsExportService
    {
        Task<byte[]> GenerateProductsCsvAsync();
    }
}
