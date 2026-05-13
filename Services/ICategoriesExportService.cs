using System.Threading.Tasks;

namespace BlazorInventario.Services
{
    public interface ICategoriesExportService
    {
        Task<byte[]> GenerateCategoriesCsvAsync();
    }
}
