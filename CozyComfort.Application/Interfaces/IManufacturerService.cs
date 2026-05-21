using CozyComfort.Application.DTOs;

namespace CozyComfort.Application.Interfaces;

public interface IManufacturerService
{
    Task<ProductionCapacityResponse> CheckProductionCapacityAsync(Guid blanketId, int requestedQuantity, CancellationToken cancellationToken = default);
    Task<BlanketDto> UpdateProductionStatusAsync(Guid blanketId, UpdateProductionStatusRequest request, CancellationToken cancellationToken = default);
    Task<LeadTimeResponse> ProvideLeadTimeAsync(Guid blanketId, int requestedQuantity, CancellationToken cancellationToken = default);
}
