using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Portfolio.Persistence.Contexts;

public class PortfolioContextFactory : IDesignTimeDbContextFactory<PortfolioContext>
{
    // Must match the UserSecretsId in JGBackend.csproj, the EF startup project.
    private const string UserSecretsId = "fdb65bad-802e-400f-9ae5-791c0484bea5";

    /// <summary>
    /// Environment variable holding the connection string, in the same shape the
    /// application host uses: <c>ConnectionStrings__JDBConnection</c>.
    /// </summary>
    private const string ConnectionStringVariable = "ConnectionStrings__JDBConnection";

    public PortfolioContext CreateDbContext(string[] args)
    {
        var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Development.json", optional: true);

        configurationBuilder.AddUserSecrets(UserSecretsId);

        IConfigurationRoot configuration = configurationBuilder.Build();

        var optionsBuilder = new DbContextOptionsBuilder<PortfolioContext>();

        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable)
            ?? configuration.GetConnectionString("JDBConnection");

        optionsBuilder.UseNpgsql(connectionString);

        return new PortfolioContext(optionsBuilder.Options);
    }
}
