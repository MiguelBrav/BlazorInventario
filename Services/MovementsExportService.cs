using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorInventario.Repositories;

namespace BlazorInventario.Services
{
    public class MovementsExportService : IMovementsExportService
    {
        private readonly IMovementsRepository _movementsRepository;
        private readonly IProductsRepository _productsRepository;

        public MovementsExportService(IMovementsRepository movementsRepository, IProductsRepository productsRepository)
        {
            _movementsRepository = movementsRepository;
            _productsRepository = productsRepository;
        }

        public async Task<byte[]> GenerateMovementsCsvAsync(DateTime? from, DateTime? to, int? productId, string? type)
        {
            var movements = (await _movementsRepository.GetByFiltersAsync(from, to, productId, type)).ToList();
            var products = (await _productsRepository.GetAllAsync()).ToDictionary(p => p.id, p => p.name ?? string.Empty);

            var sb = new StringBuilder();
            // Columns visible on the Movements page: Fecha, Producto, Tipo, Cantidad, Costo unit., Usuario, Notas
            sb.AppendLine("Fecha,Producto,Tipo,Cantidad,Costo unitario,Usuario,Notas");

            string Escape(string s) => "\"" + (s ?? string.Empty).Replace("\"", "\"\"") + "\"";

            foreach (var m in movements)
            {
                var fecha = m.date.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
                products.TryGetValue(m.product_id, out var pname);
                var tipoStr = m.type == "in" ? "Entrada" : m.type == "out" ? "Salida" : (m.type ?? string.Empty);
                var cantidad = m.quantity.ToString();
                var costo = m.unit_cost.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var usuario = m.user_name ?? (m.user_id?.ToString() ?? string.Empty);
                var notas = m.notes ?? string.Empty;

                sb.AppendLine(string.Join(",", new[] {
                    Escape(fecha), Escape(pname), Escape(tipoStr), Escape(cantidad), Escape(costo), Escape(usuario), Escape(notas)
                }));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
