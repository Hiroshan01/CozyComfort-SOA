using CozyComfort.Application.Common.Exceptions;
using CozyComfort.Application.DTOs;
using CozyComfort.Application.Interfaces;
using CozyComfort.Domain.Enums;
using CozyComfort.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CozyComfort.Infrastructure.Services;

public sealed class DistributorService(
    CozyComfortDbContext dbContext,
    IInventoryService inventoryService,
    INotificationService notificationService) : IDistributorService
{
    public async Task<IReadOnlyCollection<AssignedSellerDto>> GetAssignedSellersAsync(Guid distributorId, CancellationToken cancellationToken = default)
    {
        await EnsureDistributorAsync(distributorId, cancellationToken);

        return await dbContext.Users
            .Where(x => x.AssignedDistributorId == distributorId && x.Role == UserRole.Seller)
            .OrderBy(x => x.FullName)
            .Select(x => new AssignedSellerDto
            {
                SellerId = x.Id,
                SellerName = x.FullName,
                SellerEmail = x.Email
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<OrderDto>> GetDistributorOrdersAsync(Guid distributorId, CancellationToken cancellationToken = default)
    {
        await EnsureDistributorAsync(distributorId, cancellationToken);

        var orders = await dbContext.Orders
            .Include(x => x.Items).ThenInclude(x => x.Blanket)
            .Include(x => x.Seller)
            .Include(x => x.Distributor)
            .Include(x => x.Manufacturer)
            .Where(x => x.DistributorId == distributorId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return orders.Select(OrderService.MapOrder).ToList();
    }

    public async Task<OrderDto> FulfillSellerRequestAsync(Guid distributorId, Guid orderId, CancellationToken cancellationToken = default)
    {
        await EnsureDistributorAsync(distributorId, cancellationToken);

        var order = await dbContext.Orders
            .Include(x => x.Items).ThenInclude(x => x.Blanket)
            .Include(x => x.Seller)
            .Include(x => x.Distributor)
            .Include(x => x.Manufacturer)
            .FirstOrDefaultAsync(x => x.Id == orderId && x.DistributorId == distributorId, cancellationToken)
            ?? throw new ApiException("Order not found.", 404);

        if (order.Status is OrderStatus.Cancelled or OrderStatus.Delivered)
        {
            throw new ApiException("This order cannot be fulfilled in its current status.");
        }

        foreach (var item in order.Items)
        {
            var distributorStock = await dbContext.InventoryRecords.FirstOrDefaultAsync(
                x => x.BlanketId == item.BlanketId && x.OwnerType == InventoryOwnerType.Distributor && x.OwnerUserId == distributorId,
                cancellationToken);

            var distributorQuantity = distributorStock?.Quantity ?? 0;
            if (distributorQuantity < item.Quantity)
            {
                var manufacturerId = order.ManufacturerId
                    ?? await dbContext.Users.Where(x => x.Role == UserRole.Manufacturer).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken)
                    ?? throw new ApiException("Manufacturer is not configured.");

                var capacity = ManufacturerService.CalculateLeadTimeDays(item.Blanket.CurrentStock, item.Blanket.ProductionCapacity, item.Quantity);
                order.Status = OrderStatus.Processing;
                order.EstimatedLeadTimeDays = capacity;
                order.Notes = $"Manufacturer replenishment required for blanket {item.Blanket.ModelName}.";
                order.UpdatedAtUtc = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);

                await notificationService.CreateNotificationAsync(
                    manufacturerId,
                    "Manufacturer fulfillment request",
                    $"Order {order.Id} requires {item.Quantity} unit(s) of {item.Blanket.ModelName}.",
                    NotificationType.OrderStatus,
                    cancellationToken);

                await notificationService.CreateNotificationAsync(
                    order.SellerId,
                    "Order processing",
                    $"Order {order.Id} is waiting on manufacturer replenishment.",
                    NotificationType.OrderStatus,
                    cancellationToken);

                return OrderService.MapOrder(order);
            }
        }

        foreach (var item in order.Items)
        {
            await inventoryService.TransferInventoryAsync(new TransferInventoryRequest
            {
                BlanketId = item.BlanketId,
                FromOwnerType = InventoryOwnerType.Distributor,
                FromOwnerUserId = distributorId,
                ToOwnerType = InventoryOwnerType.Seller,
                ToOwnerUserId = order.SellerId,
                Quantity = item.Quantity
            }, cancellationToken);
        }

        order.Status = OrderStatus.Shipped;
        order.Notes = "Distributor fulfilled the seller request.";
        order.EstimatedLeadTimeDays = 0;
        order.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await notificationService.CreateNotificationAsync(
            order.SellerId,
            "Order shipped",
            $"Order {order.Id} has been shipped by the distributor.",
            NotificationType.OrderStatus,
            cancellationToken);

        return OrderService.MapOrder(order);
    }

    private async Task EnsureDistributorAsync(Guid distributorId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Users.AnyAsync(x => x.Id == distributorId && x.Role == UserRole.Distributor, cancellationToken);
        if (!exists)
        {
            throw new ApiException("Distributor not found.", 404);
        }
    }
}
