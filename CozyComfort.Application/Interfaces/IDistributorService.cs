using CozyComfort.Application.DTOs;

namespace CozyComfort.Application.Interfaces;

public interface IDistributorService
{
    Task<IReadOnlyCollection<AssignedSellerDto>> GetAssignedSellersAsync(Guid distributorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<OrderDto>> GetDistributorOrdersAsync(Guid distributorId, CancellationToken cancellationToken = default);
    Task<OrderDto> FulfillSellerRequestAsync(Guid distributorId, Guid orderId, CancellationToken cancellationToken = default);
}
