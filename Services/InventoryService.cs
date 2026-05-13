using System;
using System.Data;
using System.Threading.Tasks;
using BlazorInventario.Data;
using BlazorInventario.Repositories;
using Dapper;

namespace BlazorInventario.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IDbConnectionFactory _factory;
        private readonly IMovementsRepository _movementsRepository;
        private readonly IProductsRepository _productsRepository;
        private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

        public InventoryService(IDbConnectionFactory factory, IMovementsRepository movementsRepository, IProductsRepository productsRepository, Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
        {
            _factory = factory;
            _movementsRepository = movementsRepository;
            _productsRepository = productsRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Calcula el nuevo promedio de costo al agregar cantidad a stock existente.
        /// </summary>
        private static decimal CalculateNewAverageCost(int oldStock, decimal oldAvg, int quantityAdded, decimal newUnitCost)
        {
            var newStock = oldStock + quantityAdded;
            if (newStock <= 0) return 0m;
            return ((oldStock * oldAvg) + (quantityAdded * newUnitCost)) / newStock;
        }

        /// <summary>
        /// Calcula el nuevo promedio de costo al restar cantidad del stock existente.
        /// </summary>
        private static decimal CalculateAverageCostAfterExit(int oldStock, decimal oldAvg, int quantityRemoved, decimal exitUnitCost)
        {
            var newStock = oldStock - quantityRemoved;
            if (newStock < 0) throw new InvalidOperationException("Stock negativo no permitido");
            if (newStock == 0) return 0m;
            // Revert the cost contribution of the exit
            return ((oldStock * oldAvg) - (quantityRemoved * exitUnitCost)) / newStock;
        }

        public async Task<long> CreateEntryAsync(MovementRecord movement)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                // read product within same connection/transaction
                var product = await Dapper.SqlMapper.QuerySingleOrDefaultAsync<ProductRecord>(conn, "SELECT id, stock_current, average_cost FROM products WHERE id = @Id FOR UPDATE;", new { Id = movement.product_id }, tx);
                if (product == null) throw new InvalidOperationException("Product not found");

                var newStock = product.stock_current + movement.quantity;
                var newAvg = CalculateNewAverageCost(product.stock_current, product.average_cost, movement.quantity, movement.unit_cost);

                var id = await _movementsRepository.CreateAsync(movement, tx);
                await _productsRepository.UpdateStockAndAverageCostAsync(movement.product_id, newStock, newAvg, tx);

                // Recalculate all subsequent movements for this product to ensure costs are correct
                await RecalculateProductMovementsAsync(conn, tx, movement.product_id);

                tx.Commit();
                return id;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task<long> CreateExitAsync(MovementRecord movement)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                var product = await Dapper.SqlMapper.QuerySingleOrDefaultAsync<ProductRecord>(conn, "SELECT id, stock_current, average_cost FROM products WHERE id = @Id FOR UPDATE;", new { Id = movement.product_id }, tx);
                if (product == null) throw new InvalidOperationException("Product not found");

                if (product.stock_current < movement.quantity) throw new InvalidOperationException("Insufficient stock");

                // For exits, register unit_cost using the product's current average cost
                movement.unit_cost = product.average_cost;
                var newStock = product.stock_current - movement.quantity;

                var id = await _movementsRepository.CreateAsync(movement, tx);
                await _productsRepository.UpdateStockAndAverageCostAsync(movement.product_id, newStock, product.average_cost, tx);

                tx.Commit();
                return id;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public async Task CancelMovementAsync(long movementId)
        {
            // Authorization: only Admin users can cancel movements
            var httpUser = _httpContextAccessor?.HttpContext?.User;
            if (httpUser == null || !httpUser.IsInRole("Admin"))
            {
                throw new UnauthorizedAccessException("Only Admin users can cancel movements.");
            }

            using var conn = _factory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                var movement = await _movementsRepository.GetByIdAsync(movementId);
                if (movement == null) throw new InvalidOperationException("Movimiento no encontrado");
                if (movement.canceled) throw new InvalidOperationException("El movimiento ya fue cancelado");

                var product = await Dapper.SqlMapper.QuerySingleOrDefaultAsync<ProductRecord>(conn, "SELECT id, stock_current, average_cost FROM products WHERE id = @Id FOR UPDATE;", new { Id = movement.product_id }, tx);
                if (product == null) throw new InvalidOperationException("Producto no encontrado");

                // Depending on type, revert stock and average cost
                if (movement.type == "in")
                {
                    // Revert an entry: subtract quantity and remove cost contribution
                    var newStock = product.stock_current - movement.quantity;
                    if (newStock < 0) throw new InvalidOperationException("No se puede cancelar la entrada porque produciría stock negativo");

                    var newAvg = CalculateAverageCostAfterExit(product.stock_current, product.average_cost, movement.quantity, movement.unit_cost);

                    await _movementsRepository.CancelAsync(movementId, tx);
                    await _productsRepository.UpdateStockAndAverageCostAsync(movement.product_id, newStock, newAvg, tx);
                }
                else if (movement.type == "out")
                {
                    // Revert an exit: add quantity back and restore cost contribution
                    var newStock = product.stock_current + movement.quantity;
                    var newAvg = CalculateNewAverageCost(product.stock_current, product.average_cost, movement.quantity, movement.unit_cost);

                    await _movementsRepository.CancelAsync(movementId, tx);
                    await _productsRepository.UpdateStockAndAverageCostAsync(movement.product_id, newStock, newAvg, tx);
                }
                else
                {
                    throw new InvalidOperationException("Tipo de movimiento desconocido");
                }

                // Recalculate all movements for this product after cancellation
                await RecalculateProductMovementsAsync(conn, tx, movement.product_id);

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        /// <summary>
        /// Recalcula todos los movimientos de un producto en orden cronológico.
        /// Esto asegura que al agregar/cancelar una entrada, los costos de salidas posteriores se actualicen.
        /// </summary>
        private async Task RecalculateProductMovementsAsync(IDbConnection conn, IDbTransaction tx, int productId)
        {
            // Get all non-canceled movements for this product, ordered by date
            var movements = await Dapper.SqlMapper.QueryAsync<MovementRecord>(conn,
                "SELECT id, product_id, type, quantity, unit_cost, date, canceled FROM movements WHERE product_id = @ProductId AND canceled = 0 ORDER BY date ASC;",
                new { ProductId = productId }, tx);

            var movementList = movements.ToList();
            if (!movementList.Any()) return;

            int currentStock = 0;
            decimal currentAvg = 0m;

            foreach (var movement in movementList)
            {
                if (movement.type == "in")
                {
                    // For entries, recalculate average cost
                    currentAvg = CalculateNewAverageCost(currentStock, currentAvg, movement.quantity, movement.unit_cost);
                    currentStock += (int)movement.quantity;
                }
                else if (movement.type == "out")
                {
                    // For exits, update the unit_cost to the current average and adjust stock
                    await Dapper.SqlMapper.ExecuteAsync(conn,
                        "UPDATE movements SET unit_cost = @AvgCost WHERE id = @Id;",
                        new { AvgCost = currentAvg, Id = movement.id }, tx);

                    currentStock -= (int)movement.quantity;
                }
            }

            // Update product's final stock and average cost
            await _productsRepository.UpdateStockAndAverageCostAsync(productId, currentStock, currentAvg, tx);
        }

        public async Task RecalculateProductCostsAsync(int productId)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                await RecalculateProductMovementsAsync(conn, tx, productId);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}
