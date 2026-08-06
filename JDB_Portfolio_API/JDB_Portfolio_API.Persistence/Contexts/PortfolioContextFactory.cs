using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace JDB_Portfolio_API.Persistence.Contexts;
public class PortfolioContextFactory : IDesignTimeDbContextFactory<PortfolioContext>
{
    public PortfolioContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Development.json")
                //unique to local machine per init
                .AddUserSecrets("57d40eab-5101-40e4-9e11-4895f9a689f0")
                .Build();

        var optionsBuilder = new DbContextOptionsBuilder<PortfolioContext>();

        optionsBuilder.UseNpgsql(configuration.GetConnectionString("JDBConnection"));

        return new PortfolioContext(optionsBuilder.Options);
    }
}
