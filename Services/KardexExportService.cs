using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorInventario.Repositories;

namespace BlazorInventario.Services
{
    public class KardexExportService : IKardexExportService
    {
        private readonly IMovementsRepository _movementsRepository;

        public KardexExportService(IMovementsRepository movementsRepository)
        {
            _movementsRepository = movementsRepository;
        }

        public async Task<byte[]> GenerateKardexCsvAsync(int productId, string productName, DateTime? from, DateTime? to, string? type)
        {
            // Get all kardex records (without pagination for export)
            var kardexRecords = (await _movementsRepository.GetKardexAsync(productId, from, to, type, 1, int.MaxValue)).ToList();

            var sb = new StringBuilder();
            // Kardex columns: Fecha, Tipo, Motivo, Referencia, Entrada, Salida, Stock, CostoUnitario, CostoPromedio
            sb.AppendLine("Fecha,Tipo,Motivo,Referencia,Entrada,Salida,Stock,CostoUnitario,CostoPromedio");

            string Escape(string s) => "\"" + (s ?? string.Empty).Replace("\"", "\"\"") + "\"";

            foreach (var k in kardexRecords)
            {
                var fecha = k.date.ToString("o", System.Globalization.CultureInfo.InvariantCulture);
                var tipoStr = k.type == "in" ? "Entrada" : k.type == "out" ? "Salida" : (k.type ?? string.Empty);
                var motivo = k.canceled ? "Cancelado" : (!string.IsNullOrEmpty(k.notes) ? k.notes : (k.type == "in" ? "Compra" : "Venta"));
                var referencia = k.id.ToString();
                var entrada = k.type == "in" && !k.canceled ? k.quantity.ToString() : "-";
                var salida = k.type == "out" && !k.canceled ? k.quantity.ToString() : "-";
                var stock = k.running_stock.ToString();
                var costoUnitario = k.unit_cost.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var costoPromedio = k.running_average_cost.ToString(System.Globalization.CultureInfo.InvariantCulture);

                sb.AppendLine(string.Join(",", new[] {
                    Escape(fecha), Escape(tipoStr), Escape(motivo), Escape(referencia), 
                    Escape(entrada), Escape(salida), Escape(stock), Escape(costoUnitario), Escape(costoPromedio)
                }));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}