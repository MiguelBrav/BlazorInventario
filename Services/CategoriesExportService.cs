using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorInventario.Repositories;

namespace BlazorInventario.Services
{
    public class CategoriesExportService : ICategoriesExportService
    {
        private readonly ICategoriesRepository _categoriesRepository;

        public CategoriesExportService(ICategoriesRepository categoriesRepository)
        {
            _categoriesRepository = categoriesRepository;
        }

        public async Task<byte[]> GenerateCategoriesCsvAsync()
        {
            var categories = (await _categoriesRepository.GetAllAsync()).ToList();

            var sb = new StringBuilder();
            // Columns visible on the Categories page: Nombre, Descripción
            sb.AppendLine("Nombre,Descripción");

            string Escape(string s) => "\"" + (s ?? string.Empty).Replace("\"", "\"\"") + "\"";

            foreach (var c in categories)
            {
                var id = c.id.ToString();
                var name = c.name ?? string.Empty;
                var desc = c.description ?? string.Empty;
                var created = c.created_at.ToString("o");
                var updated = c.updated_at?.ToString("o") ?? string.Empty;

                sb.AppendLine(string.Join(",", new[] {
                    Escape(name), Escape(desc)
                }));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
