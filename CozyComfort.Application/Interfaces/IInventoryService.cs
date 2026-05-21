using CozyComfort.Application.DTOs;
using CozyComfort.Domain.Enums;

namespace CozyComfort.Application.Interfaces;

public interface IInventoryService
{
    Task<IReadOnlyCollection<InventoryDto>> GetInventoryAsync(CancellationToken cancellationToken = default);
    Task<StockCheckResponse> CheckStockAsync(Guid blanketId, InventoryOwnerType ownerType, Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<InventoryDto> UpdateInventoryAsync(UpdateInventoryRequest request, CancellationToken cancellationToken = default);
    Task TransferInventoryAsync(TransferInventoryRequest request, CancellationToken cancellationToken = default);
}
