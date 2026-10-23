using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace JDB_Portfolio_API.Persistence.Contexts;

public class PortfolioContextFactory : IDesignTimeDbContextFactory<PortfolioContext>
{
    /// <summary>
    /// Environment variable holding the user-secrets id of the API project. The id is
    /// machine specific, so it is never hardcoded here.
    /// </summary>
    private const string UserSecretsIdVariable = "JDB_USER_SECRETS_ID";

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

        var userSecretsId = Environment.GetEnvironmentVariable(UserSecretsIdVariable);
        if (!string.IsNullOrWhiteSpace(userSecretsId))
        {
            configurationBuilder.AddUserSecrets(userSecretsId);
        }

        IConfigurationRoot configuration = configurationBuilder.Build();

        var optionsBuilder = new DbContextOptionsBuilder<PortfolioContext>();

        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringVariable)
            ?? configuration.GetConnectionString("JDBConnection");

        optionsBuilder.UseNpgsql(connectionString);

        return new PortfolioContext(optionsBuilder.Options);
    }
}
