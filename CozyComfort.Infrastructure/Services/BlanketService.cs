using CozyComfort.Application.Common.Exceptions;
using CozyComfort.Application.DTOs;
using CozyComfort.Application.Interfaces;
using CozyComfort.Domain.Entities;
using CozyComfort.Domain.Enums;
using CozyComfort.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CozyComfort.Infrastructure.Services;

public sealed class BlanketService(CozyComfortDbContext dbContext) : IBlanketService
{
    public async Task<IReadOnlyCollection<BlanketDto>> GetBlanketsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Blankets
            .OrderBy(x => x.ModelName)
            .Select(x => Map(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<BlanketDto> GetBlanketByIdAsync(Guid blanketId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Blankets
            .Where(x => x.Id == blanketId)
            .Select(x => Map(x))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ApiException("Blanket not found.", 404);
    }

    public async Task<BlanketDto> CreateBlanketAsync(UpsertBlanketRequest request, CancellationToken cancellationToken = default)
    {
        var blanket = new Blanket
        {
            ModelName = request.ModelName.Trim(),
            Material = request.Material.Trim(),
            Size = request.Size.Trim(),
            Color = request.Color.Trim(),
            Price = request.Price,
            ProductionCapacity = request.ProductionCapacity,
            CurrentStock = request.CurrentStock
        };

        dbContext.Blankets.Add(blanket);
        await dbContext.SaveChangesAsync(cancellationToken);

        var manufacturer = await dbContext.Users.FirstOrDefaultAsync(x => x.Role == UserRole.Manufacturer, cancellationToken);
        if (manufacturer is not null)
        {
            dbContext.InventoryRecords.Add(new InventoryRecord
            {
                BlanketId = blanket.Id,
                OwnerType = InventoryOwnerType.Manufacturer,
                OwnerUserId = manufacturer.Id,
                Quantity = blanket.CurrentStock
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await GetBlanketByIdAsync(blanket.Id, cancellationToken);
    }

    public async Task<BlanketDto> UpdateBlanketAsync(Guid blanketId, UpsertBlanketRequest request, CancellationToken cancellationToken = default)
    {
        var blanket = await dbContext.Blankets.FirstOrDefaultAsync(x => x.Id == blanketId, cancellationToken)
            ?? throw new ApiException("Blanket not found.", 404);

        blanket.ModelName = request.ModelName.Trim();
        blanket.Material = request.Material.Trim();
        blanket.Size = request.Size.Trim();
        blanket.Color = request.Color.Trim();
        blanket.Price = request.Price;
        blanket.ProductionCapacity = request.ProductionCapacity;
        blanket.CurrentStock = request.CurrentStock;
        blanket.UpdatedAtUtc = DateTime.UtcNow;

        var manufacturerInventory = await dbContext.InventoryRecords
            .FirstOrDefaultAsync(x => x.BlanketId == blanket.Id && x.OwnerType == InventoryOwnerType.Manufacturer, cancellationToken);

        if (manufacturerInventory is not null)
        {
            manufacturerInventory.Quantity = request.CurrentStock;
            manufacturerInventory.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetBlanketByIdAsync(blanket.Id, cancellationToken);
    }

    public async Task DeleteBlanketAsync(Guid blanketId, CancellationToken cancellationToken = default)
    {
        var blanket = await dbContext.Blankets.FirstOrDefaultAsync(x => x.Id == blanketId, cancellationToken)
            ?? throw new ApiException("Blanket not found.", 404);

        dbContext.Blankets.Remove(blanket);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static BlanketDto Map(Blanket blanket) => new()
    {
        Id = blanket.Id,
        ModelName = blanket.ModelName,
        Material = blanket.Material,
        Size = blanket.Size,
        Color = blanket.Color,
        Price = blanket.Price,
        ProductionCapacity = blanket.ProductionCapacity,
        CurrentStock = blanket.CurrentStock,
        CreatedAtUtc = blanket.CreatedAtUtc,
        UpdatedAtUtc = blanket.UpdatedAtUtc
    };
}
