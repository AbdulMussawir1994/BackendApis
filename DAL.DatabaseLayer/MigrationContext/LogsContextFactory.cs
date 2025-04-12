using DAL.DatabaseLayer.DataContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DAL.DatabaseLayer.MigrationContext;

public class LogsContextFactory : IDesignTimeDbContextFactory<LogsContext>
{
    public LogsContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var connectionString = configuration.GetConnectionString("LogsConnection");

        var optionsBuilder = new DbContextOptionsBuilder<LogsContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new LogsContext(optionsBuilder.Options);
    }
}
