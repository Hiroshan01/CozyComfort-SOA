using CozyComfort.Application.Interfaces;
using CozyComfort.Domain.Entities;
using CozyComfort.Domain.Enums;
using CozyComfort.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CozyComfort.Infrastructure.Seed;

public sealed class DataSeeder(CozyComfortDbContext dbContext, IPasswordHasher passwordHasher)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var admin = new User
        {
            FullName = "System Admin",
            Email = "admin@cozycomfort.local",
            PasswordHash = passwordHasher.HashPassword("Admin@123"),
            Role = UserRole.Admin
        };

        var manufacturer = new User
        {
            FullName = "Cozy Comfort Manufacturer",
            Email = "manufacturer@cozycomfort.local",
            PasswordHash = passwordHasher.HashPassword("Manufacturer@123"),
            Role = UserRole.Manufacturer
        };

        var distributor = new User
        {
            FullName = "Regional Distributor",
            Email = "distributor@cozycomfort.local",
            PasswordHash = passwordHasher.HashPassword("Distributor@123"),
            Role = UserRole.Distributor
        };

        var seller = new User
        {
            FullName = "City Seller",
            Email = "seller@cozycomfort.local",
            PasswordHash = passwordHasher.HashPassword("Seller@123"),
            Role = UserRole.Seller,
            AssignedDistributor = distributor
        };

        var blanket = new Blanket
        {
            ModelName = "Arctic Dream",
            Material = "Cotton",
            Size = "Queen",
            Color = "Blue",
            Price = 89.99m,
            ProductionCapacity = 25,
            CurrentStock = 120
        };

        dbContext.Users.AddRange(admin, manufacturer, distributor, seller);
        dbContext.Blankets.Add(blanket);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.InventoryRecords.AddRange(
            new InventoryRecord
            {
                BlanketId = blanket.Id,
                OwnerType = InventoryOwnerType.Manufacturer,
                OwnerUserId = manufacturer.Id,
                Quantity = 120
            },
            new InventoryRecord
            {
                BlanketId = blanket.Id,
                OwnerType = InventoryOwnerType.Distributor,
                OwnerUserId = distributor.Id,
                Quantity = 45
            },
            new InventoryRecord
            {
                BlanketId = blanket.Id,
                OwnerType = InventoryOwnerType.Seller,
                OwnerUserId = seller.Id,
                Quantity = 10
            });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
