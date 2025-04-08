using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace AntiFraudService.Infrastructure.Persistence;

/// <summary>
/// Fábrica para crear instancias de AntiFraudDbContext en tiempo de diseño (e.g., para migraciones).
/// Lee la configuración directamente de appsettings.json.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AntiFraudDbContext>
{
    public AntiFraudDbContext CreateDbContext(string[] args)
    {
        // El CWD para 'dotnet ef' suele ser el directorio del proyecto de inicio (-s)
        var basePath = Directory.GetCurrentDirectory();
        var configuration = new ConfigurationBuilder()
            //.SetBasePath(Directory.GetCurrentDirectory())
            .SetBasePath(Path.GetFullPath(Path.Combine(basePath, "../"))) // Subir un nivel desde API
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var builder = new DbContextOptionsBuilder<AntiFraudDbContext>();
        var connectionString = configuration.GetConnectionString("AntiFraudDatabase");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'AntiFraudDatabase' not found.");
        }
        Console.WriteLine($"DesignTimeDbContextFactory: Using connection string: {connectionString.Substring(0, connectionString.IndexOf("Password=") > 0 ? connectionString.IndexOf("Password=") : connectionString.Length)}..."); // Log sin password

        builder.UseNpgsql(connectionString);

        return new AntiFraudDbContext(builder.Options);
    }
} 