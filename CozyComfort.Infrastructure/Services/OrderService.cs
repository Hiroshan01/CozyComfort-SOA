using CozyComfort.Application.Common.Exceptions;
using CozyComfort.Application.DTOs;
using CozyComfort.Application.Interfaces;
using CozyComfort.Domain.Entities;
using CozyComfort.Domain.Enums;
using CozyComfort.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CozyComfort.Infrastructure.Services;

public sealed class OrderService(
    CozyComfortDbContext dbContext,
    INotificationService notificationService) : IOrderService
{
    public async Task<OrderDto> CreateOrderAsync(Guid sellerId, CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var seller = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == sellerId, cancellationToken)
            ?? throw new ApiException("Seller not found.", 404);

        if (seller.Role != UserRole.Seller)
        {
            throw new ApiException("Only sellers can create orders.", 403);
        }

        if (!seller.AssignedDistributorId.HasValue)
        {
            throw new ApiException("Seller is not assigned to a distributor.");
        }

        if (request.Items.Count == 0)
        {
            throw new ApiException("At least one order item is required.");
        }

        var blanketIds = request.Items.Select(x => x.BlanketId).Distinct().ToList();
        var blankets = await dbContext.Blankets
            .Where(x => blanketIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (blankets.Count != blanketIds.Count)
        {
            throw new ApiException("One or more blankets were not found.", 404);
        }

        var manufacturer = await dbContext.Users.FirstOrDefaultAsync(x => x.Role == UserRole.Manufacturer, cancellationToken);
        var order = new Order
        {
            SellerId = seller.Id,
            DistributorId = seller.AssignedDistributorId.Value,
            ManufacturerId = manufacturer?.Id,
            CustomerName = request.CustomerName.Trim(),
            DeliveryAddress = request.DeliveryAddress.Trim(),
            Status = OrderStatus.Pending,
            Notes = "Awaiting distributor review.",
            EstimatedLeadTimeDays = 0,
            Items = request.Items.Select(item => new OrderItem
            {
                BlanketId = item.BlanketId,
                Quantity = item.Quantity,
                UnitPrice = blankets[item.BlanketId].Price
            }).ToList()
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        await notificationService.CreateNotificationAsync(
            order.DistributorId,
            "New seller order",
            $"Seller {seller.FullName} created order {order.Id}.",
            NotificationType.OrderStatus,
            cancellationToken);

        return await GetOrderAsync(order.Id, cancellationToken);
    }

    public async Task<OrderDto> UpdateOrderAsync(Guid actorId, Guid orderId, UpdateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var actor = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == actorId, cancellationToken)
            ?? throw new ApiException("User not found.", 404);

        var order = await dbContext.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new ApiException("Order not found.", 404);

        if (actor.Role == UserRole.Seller && order.SellerId != actorId)
        {
            throw new ApiException("You do not have access to this order.", 403);
        }

        if (request.Status.HasValue)
        {
            order.Status = request.Status.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            order.Notes = request.Notes.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.DeliveryAddress))
        {
            order.DeliveryAddress = request.DeliveryAddress.Trim();
        }

        order.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await notificationService.CreateNotificationAsync(
            order.SellerId,
            "Order updated",
            $"Order {order.Id} changed to {order.Status}.",
            NotificationType.OrderStatus,
            cancellationToken);

        return await GetOrderAsync(order.Id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<OrderDto>> GetOrdersAsync(Guid actorId, CancellationToken cancellationToken = default)
    {
        var actor = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == actorId, cancellationToken)
            ?? throw new ApiException("User not found.", 404);

        var query = dbContext.Orders
            .Include(x => x.Items).ThenInclude(x => x.Blanket)
            .Include(x => x.Seller)
            .Include(x => x.Distributor)
            .Include(x => x.Manufacturer)
            .AsQueryable();

        query = actor.Role switch
        {
            UserRole.Admin => query,
            UserRole.Manufacturer => query.Where(x => x.ManufacturerId == actorId),
            UserRole.Distributor => query.Where(x => x.DistributorId == actorId),
            UserRole.Seller => query.Where(x => x.SellerId == actorId),
            _ => query.Where(_ => false)
        };

        var orders = await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        return orders.Select(MapOrder).ToList();
    }

    public async Task<TrackOrderResponse> TrackOrderAsync(Guid actorId, Guid orderId, CancellationToken cancellationToken = default)
    {
        var orders = await GetOrdersAsync(actorId, cancellationToken);
        var order = orders.FirstOrDefault(x => x.Id == orderId)
            ?? throw new ApiException("Order not found.", 404);

        return new TrackOrderResponse
        {
            OrderId = order.Id,
            Status = order.Status,
            Notes = order.Notes,
            EstimatedLeadTimeDays = order.EstimatedLeadTimeDays,
            UpdatedAtUtc = order.UpdatedAtUtc
        };
    }

    internal async Task<OrderDto> GetOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await dbContext.Orders
            .Include(x => x.Items).ThenInclude(x => x.Blanket)
            .Include(x => x.Seller)
            .Include(x => x.Distributor)
            .Include(x => x.Manufacturer)
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken)
            ?? throw new ApiException("Order not found.", 404);

        return MapOrder(order);
    }

    internal static OrderDto MapOrder(Order order)
    {
        var itemDtos = order.Items.Select(item => new OrderItemDto
        {
            BlanketId = item.BlanketId,
            BlanketModelName = item.Blanket?.ModelName ?? string.Empty,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            LineTotal = item.Quantity * item.UnitPrice
        }).ToList();

        return new OrderDto
        {
            Id = order.Id,
            SellerId = order.SellerId,
            SellerName = order.Seller.FullName,
            DistributorId = order.DistributorId,
            DistributorName = order.Distributor.FullName,
            ManufacturerId = order.ManufacturerId,
            ManufacturerName = order.Manufacturer?.FullName,
            CustomerName = order.CustomerName,
            DeliveryAddress = order.DeliveryAddress,
            Status = order.Status,
            Notes = order.Notes,
            EstimatedLeadTimeDays = order.EstimatedLeadTimeDays,
            TotalAmount = itemDtos.Sum(x => x.LineTotal),
            CreatedAtUtc = order.CreatedAtUtc,
            UpdatedAtUtc = order.UpdatedAtUtc,
            Items = itemDtos
        };
    }
}
