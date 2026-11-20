using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Portfolio.Persistence.Contexts;

public class PortfolioContextFactory : IDesignTimeDbContextFactory<PortfolioContext>
{
    // Must match the UserSecretsId in Portfolio.csproj.
    private const string UserSecretsId = "57d40eab-5101-40e4-9e11-4895f9a689f0";

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
