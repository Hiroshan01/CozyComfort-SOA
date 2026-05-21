using CozyComfort.Application.DTOs;

namespace CozyComfort.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(Guid sellerId, CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderDto> UpdateOrderAsync(Guid actorId, Guid orderId, UpdateOrderRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<OrderDto>> GetOrdersAsync(Guid actorId, CancellationToken cancellationToken = default);
    Task<TrackOrderResponse> TrackOrderAsync(Guid actorId, Guid orderId, CancellationToken cancellationToken = default);
}
