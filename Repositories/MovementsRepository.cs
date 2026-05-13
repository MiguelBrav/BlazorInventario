using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using BlazorInventario.Data;

namespace BlazorInventario.Repositories
{
    public class MovementsRepository : IMovementsRepository
    {
        private readonly IDbConnectionFactory _factory;

        public MovementsRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<long> CreateAsync(MovementRecord movement, IDbTransaction? tx = null)
        {
            const string sql = @"INSERT INTO movements (product_id, type, quantity, unit_cost, supplier_id, date, user_id, notes, created_at)
                                 VALUES (@ProductId, @Type, @Quantity, @UnitCost, @SupplierId, @Date, @UserId, @Notes, NOW()); SELECT LAST_INSERT_ID();";

            if (tx != null)
            {
                var transConn = tx.Connection;
                var id = await transConn.ExecuteScalarAsync<long>(sql, new
                {
                    ProductId = movement.product_id,
                    Type = movement.type,
                    Quantity = movement.quantity,
                    UnitCost = movement.unit_cost,
                    SupplierId = movement.supplier_id,
                    Date = movement.date,
                    UserId = movement.user_id,
                    Notes = movement.notes
                }, tx);
                return id;
            }

            using var conn = _factory.CreateConnection();
            conn.Open();
            var newId = await conn.ExecuteScalarAsync<long>(sql, new
            {
                ProductId = movement.product_id,
                Type = movement.type,
                Quantity = movement.quantity,
                UnitCost = movement.unit_cost,
                SupplierId = movement.supplier_id,
                Date = movement.date,
                UserId = movement.user_id,
                Notes = movement.notes
            });

            return newId;
        }

        public async Task<MovementRecord?> GetByIdAsync(long id)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();

            const string sql = @"SELECT m.id, m.product_id, m.type, m.quantity, m.unit_cost, m.supplier_id, m.date, m.user_id, u.name AS user_name, m.notes, m.created_at, IFNULL(m.canceled,0) AS canceled
                                 FROM movements m
                                 LEFT JOIN users u ON m.user_id = u.id
                                 WHERE m.id = @Id LIMIT 1;";
            return await conn.QuerySingleOrDefaultAsync<MovementRecord>(sql, new { Id = id });
        }

        public async Task<IEnumerable<MovementRecord>> GetByFiltersAsync(DateTime? from, DateTime? to, int? productId, string? type)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            // Only return movements for products that are not logically deleted and have status 'A'
            var sql = @"SELECT m.id, m.product_id, m.type, m.quantity, m.unit_cost, m.supplier_id, m.date, m.user_id, u.name AS user_name, m.notes, m.created_at, IFNULL(m.canceled,0) AS canceled
                        FROM movements m
                        LEFT JOIN users u ON m.user_id = u.id
                        JOIN products p ON m.product_id = p.id
                        WHERE IFNULL(p.is_deleted,0) = 0 AND IFNULL(p.status,'A') = 'A'";
            var parameters = new DynamicParameters();

            if (from.HasValue)
            {
                sql += " AND m.date >= @From";
                parameters.Add("From", from.Value);
            }
            if (to.HasValue)
            {
                sql += " AND m.date <= @To";
                parameters.Add("To", to.Value);
            }
            if (productId.HasValue)
            {
                sql += " AND m.product_id = @ProductId";
                parameters.Add("ProductId", productId.Value);
            }
            if (!string.IsNullOrEmpty(type))
            {
                sql += " AND m.type = @Type";
                parameters.Add("Type", type);
            }

            sql += " ORDER BY m.date DESC";

            return await conn.QueryAsync<MovementRecord>(sql, parameters);
        }

        public async Task<IEnumerable<MovementRecord>> GetRecentAsync(int limit)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();

            const string sql = @"SELECT m.id, m.product_id, m.type, m.quantity, m.unit_cost, m.supplier_id, m.date, m.user_id, u.name AS user_name, m.notes, m.created_at, IFNULL(m.canceled,0) AS canceled
                                  FROM movements m
                                  LEFT JOIN users u ON m.user_id = u.id
                                  JOIN products p ON m.product_id = p.id
                                  WHERE IFNULL(p.is_deleted,0) = 0 AND IFNULL(p.status,'A') = 'A'
                                  ORDER BY m.date DESC LIMIT @Limit;";
            return await conn.QueryAsync<MovementRecord>(sql, new { Limit = limit });
        }

        public async Task CancelAsync(long id, IDbTransaction? tx = null)
        {
            // We need to mark the movement as canceled and then recalculate
            // product stock and average cost, and update unit_cost of non-canceled 'out' movements.
            // Perform all operations in a single transaction.

            if (tx != null)
            {
                var transConn = tx.Connection;
                // mark canceled
                await transConn.ExecuteAsync("UPDATE movements SET canceled = 1 WHERE id = @Id;", new { Id = id }, tx);

                // perform recalculation for the related product
                var mv = await transConn.QuerySingleOrDefaultAsync<(int product_id, string type, int quantity)>("SELECT product_id, type, quantity FROM movements WHERE id = @Id LIMIT 1;", new { Id = id }, tx);
                if (mv.product_id != 0)
                {
                    // recompute totals from non-canceled movements
                    var totals = await transConn.QuerySingleAsync<dynamic>(
                        @"SELECT IFNULL(SUM(CASE WHEN type = 'in' THEN quantity ELSE 0 END),0) AS total_in_qty,
                                  IFNULL(SUM(CASE WHEN type = 'in' THEN quantity * unit_cost ELSE 0 END),0) AS total_in_cost,
                                  IFNULL(SUM(CASE WHEN type = 'in' THEN quantity ELSE -quantity END),0) AS stock
                          FROM movements
                          WHERE product_id = @Pid AND IFNULL(canceled,0) = 0;",
                        new { Pid = mv.product_id }, tx);

                    decimal totalInQty = (decimal)totals.total_in_qty;
                    decimal totalInCost = (decimal)totals.total_in_cost;
                    int stock = (int)totals.stock;

                    decimal newAvg = 0m;
                    if (totalInQty > 0) newAvg = totalInCost / totalInQty;

                    await transConn.ExecuteAsync("UPDATE products SET stock_current = @Stock, average_cost = @Avg, updated_at = NOW() WHERE id = @Pid;", new { Stock = stock, Avg = newAvg, Pid = mv.product_id }, tx);

                    // update unit_cost for existing non-canceled 'out' movements to reflect new average
                    await transConn.ExecuteAsync("UPDATE movements SET unit_cost = @Avg WHERE product_id = @Pid AND type = 'out' AND IFNULL(canceled,0) = 0;", new { Avg = newAvg, Pid = mv.product_id }, tx);
                }

                return;
            }

            using var conn = _factory.CreateConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();
            try
            {
                await conn.ExecuteAsync("UPDATE movements SET canceled = 1 WHERE id = @Id;", new { Id = id }, transaction);

                var mv = await conn.QuerySingleOrDefaultAsync<(int product_id, string type, int quantity)>("SELECT product_id, type, quantity FROM movements WHERE id = @Id LIMIT 1;", new { Id = id }, transaction);
                if (mv.product_id != 0)
                {
                    var totals = await conn.QuerySingleAsync<dynamic>(
                        @"SELECT IFNULL(SUM(CASE WHEN type = 'in' THEN quantity ELSE 0 END),0) AS total_in_qty,
                                  IFNULL(SUM(CASE WHEN type = 'in' THEN quantity * unit_cost ELSE 0 END),0) AS total_in_cost,
                                  IFNULL(SUM(CASE WHEN type = 'in' THEN quantity ELSE -quantity END),0) AS stock
                          FROM movements
                          WHERE product_id = @Pid AND IFNULL(canceled,0) = 0;",
                        new { Pid = mv.product_id }, transaction);

                    decimal totalInQty = (decimal)totals.total_in_qty;
                    decimal totalInCost = (decimal)totals.total_in_cost;
                    int stock = (int)totals.stock;

                    decimal newAvg = 0m;
                    if (totalInQty > 0) newAvg = totalInCost / totalInQty;

                    await conn.ExecuteAsync("UPDATE products SET stock_current = @Stock, average_cost = @Avg, updated_at = NOW() WHERE id = @Pid;", new { Stock = stock, Avg = newAvg, Pid = mv.product_id }, transaction);

                    await conn.ExecuteAsync("UPDATE movements SET unit_cost = @Avg WHERE product_id = @Pid AND type = 'out' AND IFNULL(canceled,0) = 0;", new { Avg = newAvg, Pid = mv.product_id }, transaction);
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> HasMovementsAsync(int productId)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            const string sql = @"SELECT COUNT(1) FROM movements WHERE product_id = @ProductId;";
            var cnt = await conn.ExecuteScalarAsync<int>(sql, new { ProductId = productId });
            return cnt > 0;
        }
    }
}
