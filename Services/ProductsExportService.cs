using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorInventario.Repositories;

namespace BlazorInventario.Services
{
    public class ProductsExportService : IProductsExportService
    {
        private readonly IProductsRepository _productsRepository;
        private readonly ICategoriesRepository _categoriesRepository;

        public ProductsExportService(IProductsRepository productsRepository, ICategoriesRepository categoriesRepository)
        {
            _productsRepository = productsRepository;
            _categoriesRepository = categoriesRepository;
        }

        public async Task<byte[]> GenerateProductsCsvAsync()
        {
            var products = (await _productsRepository.GetAllAsync()).ToList();
            var categories = (await _categoriesRepository.GetAllAsync()).ToDictionary(c => c.id, c => c.name ?? string.Empty);

            var sb = new StringBuilder();
            // Columns visible on the Products page: Nombre, Categoría, Stock, Stock mínimo, Costo promedio
            sb.AppendLine("Nombre,Categoría,Stock,Stock mínimo,Costo promedio");

            string Escape(string s) => "\"" + (s ?? string.Empty).Replace("\"", "\"\"") + "\"";

            foreach (var p in products)
            {
                categories.TryGetValue(p.category_id ?? 0, out var cname);
                var id = p.id.ToString();
                var name = p.name ?? string.Empty;
                var categoryId = p.category_id?.ToString() ?? string.Empty;
                var stockCurrent = p.stock_current.ToString();
                var stockMin = p.stock_minimum.ToString();
                var avg = p.average_cost.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var created = p.created_at.ToString("o");
                var updated = p.updated_at?.ToString("o") ?? string.Empty;

                sb.AppendLine(string.Join(",", new[] {
                    Escape(name), Escape(cname), Escape(stockCurrent), Escape(stockMin), Escape(avg)
                }));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
