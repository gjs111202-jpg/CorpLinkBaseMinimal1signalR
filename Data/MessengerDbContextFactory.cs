using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CorpLinkBaseMinimal.Data;

public class MessengerDbContextFactory : IDesignTimeDbContextFactory<MessengerDbContext>
{
    public MessengerDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<MessengerDbContext>();
        var connectionString = configuration.GetConnectionString("MessengerCorpLink");
        optionsBuilder.UseNpgsql(connectionString);

        return new MessengerDbContext(optionsBuilder.Options);
    }
}