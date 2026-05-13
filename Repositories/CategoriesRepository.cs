using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using BlazorInventario.Data;
using Microsoft.AspNetCore.Http;

namespace BlazorInventario.Repositories
{
    public class CategoriesRepository : ICategoriesRepository
    {
        private readonly IDbConnectionFactory _factory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CategoriesRepository(IDbConnectionFactory factory, IHttpContextAccessor httpContextAccessor)
        {
            _factory = factory;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<int> CreateAsync(CategoryRecord category)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();

            const string sql = @"INSERT INTO categories (name, description, created_at) VALUES (@Name, @Description, NOW()); SELECT LAST_INSERT_ID();";
            var id = await conn.ExecuteScalarAsync<int>(sql, new { category.name, category.description });
            return id;
        }

        public async Task<CategoryRecord?> GetByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();

            const string sql = @"SELECT id, name, description, created_at, updated_at FROM categories WHERE id = @Id LIMIT 1;";
            return await conn.QuerySingleOrDefaultAsync<CategoryRecord>(sql, new { Id = id });
        }

        public async Task<IEnumerable<CategoryRecord>> GetAllAsync()
        {
            using var conn = _factory.CreateConnection();
            conn.Open();

            const string sql = @"SELECT id, name, description, created_at, updated_at FROM categories ORDER BY name;";
            return await conn.QueryAsync<CategoryRecord>(sql);
        }

        public async Task UpdateAsync(CategoryRecord category)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();

            const string sql = @"UPDATE categories SET name = @Name, description = @Description, updated_at = NOW() WHERE id = @Id;";
            await conn.ExecuteAsync(sql, new { Id = category.id, category.name, category.description });
        }

        public async Task DeleteAsync(int id)
        {
            // Authorization: only Admin users can delete categories
            var user = _httpContextAccessor?.HttpContext?.User;
            if (user == null || !user.IsInRole("Admin"))
            {
                throw new UnauthorizedAccessException("Only Admin users can delete categories.");
            }

            using var conn = _factory.CreateConnection();
            conn.Open();

            const string sql = @"DELETE FROM categories WHERE id = @Id;";
            await conn.ExecuteAsync(sql, new { Id = id });
        }
    }
}
