using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using BlazorInventario.Data;
using Microsoft.AspNetCore.Http;

namespace BlazorInventario.Repositories
{
    public class ProductsRepository : IProductsRepository
    {
        private readonly IDbConnectionFactory _factory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ProductsRepository(IDbConnectionFactory factory, IHttpContextAccessor httpContextAccessor)
        {
            _factory = factory;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<int> CreateAsync(ProductRecord product)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();

            const string sql = @"INSERT INTO products (name, category_id, stock_current, stock_minimum, average_cost, created_at) VALUES (@Name, @CategoryId, @StockCurrent, @StockMinimum, @AverageCost, NOW()); SELECT LAST_INSERT_ID();";
            var id = await conn.ExecuteScalarAsync<int>(sql, new { product.name, CategoryId = product.category_id, StockCurrent = product.stock_current, StockMinimum = product.stock_minimum, AverageCost = product.average_cost });
            return id;
        }

        public async Task<ProductRecord?> GetByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();

            const string sql = @"SELECT id, name, category_id, stock_current, stock_minimum, average_cost, is_deleted, status, created_at, updated_at FROM products WHERE id = @Id LIMIT 1;";
            return await conn.QuerySingleOrDefaultAsync<ProductRecord>(sql, new { Id = id });
        }

        public async Task<IEnumerable<ProductRecord>> GetAllAsync()
        {
            using var conn = _factory.CreateConnection();
            conn.Open();

            // Only return non-deleted products
            const string sql = @"SELECT id, name, category_id, stock_current, stock_minimum, average_cost, is_deleted, status, created_at, updated_at FROM products WHERE IFNULL(is_deleted,0) = 0 ORDER BY name;";
            return await conn.QueryAsync<ProductRecord>(sql);
        }

        public async Task UpdateAsync(ProductRecord product)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();

            // Do not allow changing average_cost on update here; average cost is only set on product creation or via inventory operations
            const string sql = @"UPDATE products SET name = @Name, category_id = @CategoryId, stock_current = @StockCurrent, stock_minimum = @StockMinimum, status = @Status, is_deleted = @IsDeleted, updated_at = NOW() WHERE id = @Id;";
            await conn.ExecuteAsync(sql, new { Id = product.id, product.name, CategoryId = product.category_id, StockCurrent = product.stock_current, StockMinimum = product.stock_minimum, Status = product.status, IsDeleted = product.is_deleted });
        }

        public async Task DeleteAsync(int id)
        {
            // Authorization: only Admin users can delete products
            var user = _httpContextAccessor?.HttpContext?.User;
            if (user == null || !user.IsInRole("Admin"))
            {
                throw new UnauthorizedAccessException("Only Admin users can delete products.");
            }

            using var conn = _factory.CreateConnection();
            conn.Open();

            // Do not allow hard delete if there are movements
            const string checkSql = @"SELECT COUNT(1) FROM movements WHERE product_id = @Id;";
            var cnt = await conn.ExecuteScalarAsync<int>(checkSql, new { Id = id });
            if (cnt > 0) throw new InvalidOperationException("No se puede eliminar un producto con movimientos. Marque como dado de baja si desea inhabilitarlo.");

            const string sql = @"DELETE FROM products WHERE id = @Id;";
            await conn.ExecuteAsync(sql, new { Id = id });
        }

        public async Task MarkAsInactiveAsync(int id)
        {
            // Authorization: only Admin users can mark products as inactive
            var user = _httpContextAccessor?.HttpContext?.User;
            if (user == null || !user.IsInRole("Admin"))
            {
                throw new UnauthorizedAccessException("Only Admin users can mark products as inactive.");
            }

            using var conn = _factory.CreateConnection();
            conn.Open();

            // use status 'I' (inactivo) to represent dado de baja; 'A' = activo
            const string sql = @"UPDATE products SET is_deleted = 1, status = 'I', updated_at = NOW() WHERE id = @Id;";
            await conn.ExecuteAsync(sql, new { Id = id });
        }

        public async Task UpdateStockAndAverageCostAsync(int productId, int newStock, decimal newAverageCost, IDbTransaction? tx = null)
        {
            if (tx != null)
            {
                var transConn = tx.Connection;
                const string sqlTx = @"UPDATE products SET stock_current = @Stock, average_cost = @Avg, updated_at = NOW() WHERE id = @Id;";
                await transConn.ExecuteAsync(sqlTx, new { Id = productId, Stock = newStock, Avg = newAverageCost }, tx);
                return;
            }

            using var conn = _factory.CreateConnection();
            conn.Open();
            const string sql = @"UPDATE products SET stock_current = @Stock, average_cost = @Avg, updated_at = NOW() WHERE id = @Id;";
            await conn.ExecuteAsync(sql, new { Id = productId, Stock = newStock, Avg = newAverageCost });
        }
    }
}
