using System.Threading.Tasks;

namespace BlazorInventario.Services
{
    public interface IUsersExportService
    {
        Task<byte[]> GenerateUsersCsvAsync();
    }
}
