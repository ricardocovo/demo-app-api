using LittleLeagueSoccer.Api.Data.EfCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LittleLeagueSoccer.Api.Data;

public static class DataServiceExtensions
{
    /// <summary>
    /// Registers the data layer. The provider is selected with the "Database:Provider" setting
    /// and the connection string with "Database:ConnectionString".
    /// </summary>
    public static IServiceCollection AddSoccerData(this IServiceCollection services)
    {
        services.AddDbContext<SoccerDbContext>((sp, options) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var env = sp.GetRequiredService<IHostEnvironment>();
            var provider = config["Database:Provider"] ?? "Sqlite";
            var connectionString = config["Database:ConnectionString"] ?? "Data Source=App_Data/soccer.db";

            switch (provider.ToLowerInvariant())
            {
                case "sqlite":
                    options.UseSqlite(ResolveSqlitePath(connectionString, env.ContentRootPath));
                    break;
                // To use a server database, add its EF Core provider package and a case here, e.g.:
                // case "sqlserver": options.UseSqlServer(connectionString); break;
                // case "postgres":  options.UseNpgsql(connectionString); break;
                default:
                    throw new NotSupportedException($"Database provider '{provider}' is not supported.");
            }
        });

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        return services;
    }

    public static async Task InitializeSoccerDataAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SoccerDbContext>();
        await db.Database.MigrateAsync();
    }

    private static string ResolveSqlitePath(string connectionString, string contentRoot)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;
        if (string.IsNullOrWhiteSpace(dataSource) || dataSource == ":memory:" ||
            builder.Mode == SqliteOpenMode.Memory)
            return connectionString;

        if (!Path.IsPathRooted(dataSource))
            dataSource = Path.Combine(contentRoot, dataSource);

        Directory.CreateDirectory(Path.GetDirectoryName(dataSource)!);
        builder.DataSource = dataSource;
        return builder.ToString();
    }
}
