using DAL.DatabaseLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace DAL.DatabaseLayer.DataContext
{
    public partial class LogsContext : DbContext
    {
        public LogsContext(DbContextOptions<LogsContext> options, ILogger<LogsContext>? logger = null) : base(options)
        {
            var databaseCreator = Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
            if (databaseCreator != null && !databaseCreator.CanConnect())
            {
                databaseCreator.Create();
                databaseCreator.CreateTables();
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
