using DAL.DatabaseLayer.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace DAL.DatabaseLayer.DataContext;

public class WebContextDb : IdentityDbContext<AppUser>
{

    public WebContextDb(DbContextOptions<WebContextDb> options) : base(options)
    {
        var databaseCreator = Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;

        if (databaseCreator != null && !databaseCreator.CanConnect())
        {
            databaseCreator.Create();
            databaseCreator.CreateTables();
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
