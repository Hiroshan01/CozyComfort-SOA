using CozyComfort.Domain.Entities;
using CozyComfort.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CozyComfort.Infrastructure.Persistence;

public sealed class CozyComfortDbContext(DbContextOptions<CozyComfortDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Blanket> Blankets => Set<Blanket>();
    public DbSet<InventoryRecord> InventoryRecords => Set<InventoryRecord>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var enumToString = new[]
        {
            typeof(UserRole),
            typeof(InventoryOwnerType),
            typeof(OrderStatus),
            typeof(NotificationType)
        };

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (enumToString.Contains(property.ClrType))
                {
                    property.SetMaxLength(40);
                }
            }
        }

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(x => x.Role).HasConversion<string>();
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.FullName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(150).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.HasOne(x => x.AssignedDistributor)
                .WithMany(x => x.AssignedSellers)
                .HasForeignKey(x => x.AssignedDistributorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Blanket>(entity =>
        {
            entity.Property(x => x.ModelName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Material).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Size).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Color).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Price).HasPrecision(18, 2);
        });

        modelBuilder.Entity<InventoryRecord>(entity =>
        {
            entity.Property(x => x.OwnerType).HasConversion<string>();
            entity.HasIndex(x => new { x.BlanketId, x.OwnerType, x.OwnerUserId }).IsUnique();
            entity.HasOne(x => x.OwnerUser)
                .WithMany()
                .HasForeignKey(x => x.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(x => x.Status).HasConversion<string>();
            entity.Property(x => x.CustomerName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DeliveryAddress).HasMaxLength(250).IsRequired();
            entity.HasOne(x => x.Seller).WithMany().HasForeignKey(x => x.SellerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Distributor).WithMany().HasForeignKey(x => x.DistributorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Manufacturer).WithMany().HasForeignKey(x => x.ManufacturerId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(x => x.UnitPrice).HasPrecision(18, 2);
            entity.HasOne(x => x.Order).WithMany(x => x.Items).HasForeignKey(x => x.OrderId);
            entity.HasOne(x => x.Blanket).WithMany(x => x.OrderItems).HasForeignKey(x => x.BlanketId);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(x => x.Type).HasConversion<string>();
            entity.Property(x => x.Title).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(500).IsRequired();
            entity.HasOne(x => x.RecipientUser)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.RecipientUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
