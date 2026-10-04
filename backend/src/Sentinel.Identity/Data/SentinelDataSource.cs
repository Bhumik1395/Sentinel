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
        _connectionString = configuration.GetConnectionString("Sentinel")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Sentinel is missing. Set it with user-secrets or the " +
                "ConnectionStrings__Sentinel environment variable.");
    }

    public NpgsqlConnection CreateConnection() => new(_connectionString);
}
