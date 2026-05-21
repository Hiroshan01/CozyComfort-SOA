using CozyComfort.Application.DTOs;

namespace CozyComfort.Application.Interfaces;

public interface IBlanketService
{
    Task<IReadOnlyCollection<BlanketDto>> GetBlanketsAsync(CancellationToken cancellationToken = default);
    Task<BlanketDto> GetBlanketByIdAsync(Guid blanketId, CancellationToken cancellationToken = default);
    Task<BlanketDto> CreateBlanketAsync(UpsertBlanketRequest request, CancellationToken cancellationToken = default);
    Task<BlanketDto> UpdateBlanketAsync(Guid blanketId, UpsertBlanketRequest request, CancellationToken cancellationToken = default);
    Task DeleteBlanketAsync(Guid blanketId, CancellationToken cancellationToken = default);
}
