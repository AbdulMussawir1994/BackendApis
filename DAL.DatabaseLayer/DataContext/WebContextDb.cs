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

        //builder.Entity<AppUser>()
        //   .HasQueryFilter(user => user.IsActive);

        //builder.Entity<Employee>()
        //    .HasQueryFilter(emp => emp.IsActive);

        builder.Entity<UserNotification>(entity =>
        {
            entity.Property(e => e.CreatedBy).HasMaxLength(450);

            entity.Property(e => e.CreatedDate).HasColumnType("datetime");

            entity.Property(e => e.DeletedBy).HasMaxLength(450);

            entity.Property(e => e.DeletedDate).HasColumnType("datetime");

            entity.Property(e => e.DeviceId)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.Property(e => e.DeviceToken)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.Property(e => e.ModifiedBy).HasMaxLength(450);

            entity.Property(e => e.ModifiedDate).HasColumnType("datetime");

            entity.Property(e => e.NotificationTime).HasColumnType("datetime");

            entity.Property(e => e.ReadCount).HasDefaultValueSql("((0))");

            entity.Property(e => e.UserId).HasMaxLength(450);

            entity.HasOne(d => d.Notification)
                .WithMany(p => p.UserNotifications)
                .HasForeignKey(d => d.NotificationId)
                .HasConstraintName("FK_UserNotifications_Master.Notifications");
        });
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<MasterNotification> MasterNotifications => Set<MasterNotification>();
}
