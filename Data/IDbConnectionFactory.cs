using System.Data;

namespace BlazorInventario.Data
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
