using CozyComfort.Application.Common.Exceptions;
using CozyComfort.Application.DTOs;
using CozyComfort.Application.Interfaces;
using CozyComfort.Domain.Entities;
using CozyComfort.Domain.Enums;
using CozyComfort.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CozyComfort.Infrastructure.Services;

public sealed class InventoryService(CozyComfortDbContext dbContext, INotificationService notificationService) : IInventoryService
{
    public async Task<IReadOnlyCollection<InventoryDto>> GetInventoryAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.InventoryRecords
            .Include(x => x.Blanket)
            .Include(x => x.OwnerUser)
            .OrderBy(x => x.OwnerType)
            .ThenBy(x => x.OwnerUser.FullName)
            .Select(x => new InventoryDto
            {
                Id = x.Id,
                BlanketId = x.BlanketId,
                BlanketModelName = x.Blanket.ModelName,
                OwnerType = x.OwnerType,
                OwnerUserId = x.OwnerUserId,
                OwnerName = x.OwnerUser.FullName,
                Quantity = x.Quantity,
                UpdatedAtUtc = x.UpdatedAtUtc ?? x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<StockCheckResponse> CheckStockAsync(Guid blanketId, InventoryOwnerType ownerType, Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        var record = await dbContext.InventoryRecords
            .FirstOrDefaultAsync(x => x.BlanketId == blanketId && x.OwnerType == ownerType && x.OwnerUserId == ownerUserId, cancellationToken);

        var quantity = record?.Quantity ?? 0;
        return new StockCheckResponse
        {
            BlanketId = blanketId,
            OwnerType = ownerType,
            OwnerUserId = ownerUserId,
            AvailableQuantity = quantity,
            IsAvailable = quantity > 0
        };
    }

    public async Task<InventoryDto> UpdateInventoryAsync(UpdateInventoryRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateOwnerAsync(request.OwnerUserId, request.OwnerType, cancellationToken);
        await EnsureBlanketExistsAsync(request.BlanketId, cancellationToken);

        var record = await dbContext.InventoryRecords
            .Include(x => x.Blanket)
            .Include(x => x.OwnerUser)
            .FirstOrDefaultAsync(
                x => x.BlanketId == request.BlanketId && x.OwnerType == request.OwnerType && x.OwnerUserId == request.OwnerUserId,
                cancellationToken);

        if (record is null)
        {
            record = new InventoryRecord
            {
                BlanketId = request.BlanketId,
                OwnerType = request.OwnerType,
                OwnerUserId = request.OwnerUserId,
                Quantity = request.Quantity
            };

            dbContext.InventoryRecords.Add(record);
        }
        else
        {
            record.Quantity = request.Quantity;
            record.UpdatedAtUtc = DateTime.UtcNow;
        }

        await SyncManufacturerStockAsync(request.BlanketId, request.OwnerType, request.Quantity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var updated = await dbContext.InventoryRecords
            .Include(x => x.Blanket)
            .Include(x => x.OwnerUser)
            .FirstAsync(x => x.BlanketId == request.BlanketId && x.OwnerType == request.OwnerType && x.OwnerUserId == request.OwnerUserId, cancellationToken);

        return new InventoryDto
        {
            Id = updated.Id,
            BlanketId = updated.BlanketId,
            BlanketModelName = updated.Blanket.ModelName,
            OwnerType = updated.OwnerType,
            OwnerUserId = updated.OwnerUserId,
            OwnerName = updated.OwnerUser.FullName,
            Quantity = updated.Quantity,
            UpdatedAtUtc = updated.UpdatedAtUtc ?? updated.CreatedAtUtc
        };
    }

    public async Task TransferInventoryAsync(TransferInventoryRequest request, CancellationToken cancellationToken = default)
    {
        if (request.FromOwnerUserId == request.ToOwnerUserId && request.FromOwnerType == request.ToOwnerType)
        {
            throw new ApiException("Transfer source and destination cannot be the same.");
        }

        await ValidateOwnerAsync(request.FromOwnerUserId, request.FromOwnerType, cancellationToken);
        await ValidateOwnerAsync(request.ToOwnerUserId, request.ToOwnerType, cancellationToken);
        await EnsureBlanketExistsAsync(request.BlanketId, cancellationToken);

        var source = await GetOrCreateInventoryAsync(request.BlanketId, request.FromOwnerType, request.FromOwnerUserId, cancellationToken);
        if (source.Quantity < request.Quantity)
        {
            throw new ApiException("Insufficient stock for transfer.");
        }

        var destination = await GetOrCreateInventoryAsync(request.BlanketId, request.ToOwnerType, request.ToOwnerUserId, cancellationToken);
        source.Quantity -= request.Quantity;
        source.UpdatedAtUtc = DateTime.UtcNow;
        destination.Quantity += request.Quantity;
        destination.UpdatedAtUtc = DateTime.UtcNow;

        await SyncManufacturerStockAsync(request.BlanketId, request.FromOwnerType, source.Quantity, cancellationToken);
        await SyncManufacturerStockAsync(request.BlanketId, request.ToOwnerType, destination.Quantity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await notificationService.CreateNotificationAsync(
            request.ToOwnerUserId,
            "Inventory transfer received",
            $"{request.Quantity} unit(s) of blanket {request.BlanketId} were transferred to your inventory.",
            NotificationType.StockAlert,
            cancellationToken);
    }

    private async Task<InventoryRecord> GetOrCreateInventoryAsync(Guid blanketId, InventoryOwnerType ownerType, Guid ownerUserId, CancellationToken cancellationToken)
    {
        var record = await dbContext.InventoryRecords
            .FirstOrDefaultAsync(x => x.BlanketId == blanketId && x.OwnerType == ownerType && x.OwnerUserId == ownerUserId, cancellationToken);

        if (record is not null)
        {
            return record;
        }

        record = new InventoryRecord
        {
            BlanketId = blanketId,
            OwnerType = ownerType,
            OwnerUserId = ownerUserId,
            Quantity = 0
        };

        dbContext.InventoryRecords.Add(record);
        return record;
    }

    private async Task ValidateOwnerAsync(Guid ownerUserId, InventoryOwnerType ownerType, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == ownerUserId, cancellationToken)
            ?? throw new ApiException("Inventory owner not found.", 404);

        var expectedRole = ownerType switch
        {
            InventoryOwnerType.Manufacturer => UserRole.Manufacturer,
            InventoryOwnerType.Distributor => UserRole.Distributor,
            InventoryOwnerType.Seller => UserRole.Seller,
            _ => throw new ApiException("Invalid inventory owner type.")
        };

        if (user.Role != expectedRole)
        {
            throw new ApiException("Inventory owner type does not match the user's role.");
        }
    }

    private async Task EnsureBlanketExistsAsync(Guid blanketId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Blankets.AnyAsync(x => x.Id == blanketId, cancellationToken);
        if (!exists)
        {
            throw new ApiException("Blanket not found.", 404);
        }
    }

    private async Task SyncManufacturerStockAsync(Guid blanketId, InventoryOwnerType ownerType, int quantity, CancellationToken cancellationToken)
    {
        if (ownerType != InventoryOwnerType.Manufacturer)
        {
            return;
        }

        var blanket = await dbContext.Blankets.FirstAsync(x => x.Id == blanketId, cancellationToken);
        blanket.CurrentStock = quantity;
        blanket.UpdatedAtUtc = DateTime.UtcNow;
    }
}
