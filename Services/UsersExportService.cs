using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorInventario.Repositories;

namespace BlazorInventario.Services
{
    public class UsersExportService : IUsersExportService
    {
        private readonly IUserRepository _userRepository;

        public UsersExportService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<byte[]> GenerateUsersCsvAsync()
        {
            var users = (await _userRepository.GetAllAsync()).ToList();

            var sb = new StringBuilder();
            // Header
            sb.AppendLine("Nombre,Email,Rol,Activo,CreatedAt");

            string Escape(string s) => '"' + (s ?? string.Empty).Replace("\"", "\"\"") + '"';

            foreach (var u in users)
            {
                var nombre = u.name ?? string.Empty;
                var email = u.email ?? string.Empty;
                var role = u.role ?? string.Empty;
                var activo = u.is_active ? "Sí" : "No";

                sb.AppendLine(string.Join(",", new[] {
                    Escape(nombre), Escape(email), Escape(role), Escape(activo), Escape("")
                }));
            }

            // Return UTF8 bytes
            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
