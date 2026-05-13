using System.Data;
using System.Threading.Tasks;
using Dapper;
using BlazorInventario.Data;

namespace BlazorInventario.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _factory;

        public UserRepository(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<UserRecord?> GetByEmailAsync(string email)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();

            const string sql = @"SELECT id, name, email, password_hash, role, is_active FROM users WHERE email = @Email LIMIT 1;";
            return await conn.QuerySingleOrDefaultAsync<UserRecord>(sql, new { Email = email });
        }

        public async Task<IEnumerable<UserRecord>> GetAllAsync()
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            const string sql = @"SELECT id, name, email, role, is_active FROM users ORDER BY name;";
            return await conn.QueryAsync<UserRecord>(sql);
        }

        public async Task<UserRecord?> GetByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            const string sql = @"SELECT id, name, email, role, is_active FROM users WHERE id = @Id LIMIT 1;";
            return await conn.QuerySingleOrDefaultAsync<UserRecord>(sql, new { Id = id });
        }

        public async Task<int> CreateAsync(UserRecord user)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            const string sql = @"INSERT INTO users (name, email, password_hash, role, is_active, created_at) VALUES (@Name, @Email, @PasswordHash, @Role, @IsActive, NOW()); SELECT LAST_INSERT_ID();";
            var id = await conn.ExecuteScalarAsync<int>(sql, new { user.name, user.email, PasswordHash = user.password_hash, Role = user.role, IsActive = user.is_active });
            return id;
        }

        public async Task UpdateAsync(UserRecord user)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();

            // Si password_hash es null, no actualizar el password (por si editamos sin cambiar contraseña)
            if (string.IsNullOrEmpty(user.password_hash))
            {
                const string sqlWithoutPassword = @"UPDATE users SET name = @Name, email = @Email, role = @Role, is_active = @IsActive, updated_at = NOW() WHERE id = @Id;";
                await conn.ExecuteAsync(sqlWithoutPassword, new { Id = user.id, user.name, user.email, Role = user.role, IsActive = user.is_active });
            }
            else
            {
                const string sqlWithPassword = @"UPDATE users SET name = @Name, email = @Email, password_hash = @PasswordHash, role = @Role, is_active = @IsActive, updated_at = NOW() WHERE id = @Id;";
                await conn.ExecuteAsync(sqlWithPassword, new { Id = user.id, user.name, user.email, PasswordHash = user.password_hash, Role = user.role, IsActive = user.is_active });
            }
        }

        public async Task DeactivateAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            conn.Open();
            const string sql = @"UPDATE users SET is_active = 0, updated_at = NOW() WHERE id = @Id;";
            await conn.ExecuteAsync(sql, new { Id = id });
        }
    }
}
