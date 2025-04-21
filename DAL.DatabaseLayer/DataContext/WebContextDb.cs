using DAL.DatabaseLayer.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace DAL.DatabaseLayer.DataContext;

public class WebContextDb : IdentityDbContext<AppUser>
{

    private readonly ILogger<WebContextDb>? _logger;

    public WebContextDb(DbContextOptions<WebContextDb> options, ILogger<WebContextDb>? logger = null) : base(options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        try
        {
            var databaseCreator = Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
            if (databaseCreator != null && !databaseCreator.CanConnect())
            {
                _logger?.LogWarning("🔹 Database not connected. Creating Database...");
                databaseCreator.Create();
                databaseCreator.CreateTables();
                _logger?.LogInformation("✅ Database and tables created.");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "❌ Database initialization error.");
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(u => u.CNIC).IsUnique().HasDatabaseName("IDX_IcNumber");
            entity.HasIndex(u => u.Email).IsUnique().HasDatabaseName("IDX_Email");
            entity.HasIndex(u => u.UserName).IsUnique().HasDatabaseName("IDX_UserName");

            entity.Property(e => e.CNIC)
                  .IsRequired()
                  .HasMaxLength(13);

            entity.Property(e => e.DateCreated)
                  .HasDefaultValueSql("GETUTCDATE()") // Ensures DB defaulting
                  .IsRequired();

            entity.Property(e => e.UpdatedDate)
                  .HasDefaultValueSql("NULL"); // Ensures first-time NULL
        });
    }

    public DbSet<Employee> Employees => Set<Employee>();
}
