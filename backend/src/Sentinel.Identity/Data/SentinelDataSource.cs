using Npgsql;
using Microsoft.Extensions.Configuration;

namespace Sentinel.Identity.Data;

public interface ISentinelDataSource
{
    NpgsqlConnection CreateConnection();
}

public class SentinelDataSource : ISentinelDataSource
{
    private readonly string _connectionString;

    public SentinelDataSource(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Postgres is missing. Check appsettings.json / environment variables.");
    }

    public NpgsqlConnection CreateConnection() => new(_connectionString);
}