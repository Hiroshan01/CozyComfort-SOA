using CozyComfort.Application.DTOs;

namespace CozyComfort.Application.Interfaces;

public interface IUserService
{
    Task<AuthResponse> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserProfileDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<UserProfileDto> UpdateRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default);
}
