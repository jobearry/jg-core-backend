using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Features;
using Portfolio.Persistence.Contexts;

namespace Portfolio.Persistence.Tests;

/// <summary>
/// Reads use portfolio_app and writes use portfolio_writer in production, but local
/// development may point both settings at the same database user. These tests pin
/// the wiring without opening a connection.
/// </summary>
public sealed class DatabaseConnectionRegistrationTests
{
    private const string ReadConnection = "Host=read;Database=read;Username=read;Password=read";
    private const string WriteConnection = "Host=write;Database=write;Username=write;Password=write";

    [Fact]
    public void AddPortfolioFeatures_RegistersSeparateReadAndWriteContexts()
    {
        var services = BuildServices();

        var contextTypes = services
            .Where(descriptor => typeof(DbContext).IsAssignableFrom(descriptor.ServiceType))
            .Select(descriptor => descriptor.ServiceType)
            .Distinct()
            .ToList();

        Assert.Equal(2, contextTypes.Count);
    }

    [Fact]
    public void AddPortfolioFeatures_UsesJDBConnectionForReadsAndJDBConnectionWriteForWrites()
    {
        var services = BuildServices();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var read = (DbContext)scope.ServiceProvider.GetRequiredService(typeof(PortfolioContext));
        var writeType = services
            .Where(descriptor => typeof(DbContext).IsAssignableFrom(descriptor.ServiceType)
                && descriptor.ServiceType != typeof(PortfolioContext))
            .Select(descriptor => descriptor.ServiceType)
            .Distinct()
            .Single();
        var write = (DbContext)scope.ServiceProvider.GetRequiredService(writeType);

        Assert.Equal(ReadConnection, read.Database.GetConnectionString());
        Assert.Equal(WriteConnection, write.Database.GetConnectionString());
    }

    private static IServiceCollection BuildServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:JDBConnection"] = ReadConnection,
                ["ConnectionStrings:JDBConnectionWrite"] = WriteConnection
            })
            .Build();

        var services = new ServiceCollection();
        services.AddPortfolioFeatures(configuration);
        return services;
    }
}
