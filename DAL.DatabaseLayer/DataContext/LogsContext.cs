using DAL.DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace DAL.DatabaseLayer.DataContext
{
    public partial class LogsContext : DbContext
    {
        //private readonly ILogger<LogsContext>? _logger;
        public LogsContext(DbContextOptions<LogsContext> options, ILogger<LogsContext>? logger = null) : base(options)
        {
            //_logger = logger ?? throw new ArgumentNullException(nameof(logger));

            try
            {
                var databaseCreator = Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
                if (databaseCreator != null && !databaseCreator.CanConnect())
                {
                    // _logger?.LogWarning("🔹 Database not connected. Creating Database...");
                    databaseCreator.Create();
                    databaseCreator.CreateTables();
                    //  _logger?.LogInformation("✅ Database and tables created.");
                }
            }
            catch (Exception ex)
            {
                //_logger?.LogError(ex, "❌ Database initialization error.");
            }
        }

        public virtual DbSet<Log> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Your entity configurations (if needed)
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }

}
