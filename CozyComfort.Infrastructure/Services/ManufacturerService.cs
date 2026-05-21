using CozyComfort.Application.Common.Exceptions;
using CozyComfort.Application.DTOs;
using CozyComfort.Application.Interfaces;
using CozyComfort.Domain.Enums;
using CozyComfort.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CozyComfort.Infrastructure.Services;

public sealed class ManufacturerService(CozyComfortDbContext dbContext) : IManufacturerService
{
    public async Task<ProductionCapacityResponse> CheckProductionCapacityAsync(Guid blanketId, int requestedQuantity, CancellationToken cancellationToken = default)
    {
        var blanket = await dbContext.Blankets.FirstOrDefaultAsync(x => x.Id == blanketId, cancellationToken)
            ?? throw new ApiException("Blanket not found.", 404);

        return new ProductionCapacityResponse
        {
            BlanketId = blanket.Id,
            ModelName = blanket.ModelName,
            CurrentStock = blanket.CurrentStock,
            ProductionCapacity = blanket.ProductionCapacity,
            RequestedQuantity = requestedQuantity,
            CanFulfillFromStock = blanket.CurrentStock >= requestedQuantity,
            EstimatedLeadTimeDays = CalculateLeadTimeDays(blanket.CurrentStock, blanket.ProductionCapacity, requestedQuantity)
        };
    }

    public async Task<BlanketDto> UpdateProductionStatusAsync(Guid blanketId, UpdateProductionStatusRequest request, CancellationToken cancellationToken = default)
    {
        var blanket = await dbContext.Blankets.FirstOrDefaultAsync(x => x.Id == blanketId, cancellationToken)
            ?? throw new ApiException("Blanket not found.", 404);

        blanket.CurrentStock = request.CurrentStock;
        blanket.ProductionCapacity = request.ProductionCapacity;
        blanket.UpdatedAtUtc = DateTime.UtcNow;

        var manufacturerInventory = await dbContext.InventoryRecords
            .FirstOrDefaultAsync(
                x => x.BlanketId == blanketId && x.OwnerType == InventoryOwnerType.Manufacturer,
                cancellationToken);

        if (manufacturerInventory is not null)
        {
            manufacturerInventory.Quantity = request.CurrentStock;
            manufacturerInventory.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new BlanketDto
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

    public async Task<LeadTimeResponse> ProvideLeadTimeAsync(Guid blanketId, int requestedQuantity, CancellationToken cancellationToken = default)
    {
        var blanket = await dbContext.Blankets.FirstOrDefaultAsync(x => x.Id == blanketId, cancellationToken)
            ?? throw new ApiException("Blanket not found.", 404);

        return new LeadTimeResponse
        {
            BlanketId = blanket.Id,
            RequestedQuantity = requestedQuantity,
            EstimatedLeadTimeDays = CalculateLeadTimeDays(blanket.CurrentStock, blanket.ProductionCapacity, requestedQuantity)
        };
    }

    public static int CalculateLeadTimeDays(int currentStock, int productionCapacity, int requestedQuantity)
    {
        if (requestedQuantity <= currentStock)
        {
            return 0;
        }

        if (productionCapacity <= 0)
        {
            return 30;
        }

        var shortfall = requestedQuantity - currentStock;
        return (int)Math.Ceiling(shortfall / (double)Math.Max(productionCapacity, 1));
    }
}
