using CozyComfort.Application.Common.Exceptions;
using CozyComfort.Application.DTOs;
using CozyComfort.Application.Interfaces;
using CozyComfort.Domain.Entities;
using CozyComfort.Domain.Enums;
using CozyComfort.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CozyComfort.Infrastructure.Services;

public sealed class UserService(
    CozyComfortDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IUserService
{
    public async Task<AuthResponse> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken))
        {
            throw new ApiException("A user with this email already exists.");
        }

        if (!request.AssignedDistributorId.HasValue)
        {
            throw new ApiException("Sellers must be assigned to a distributor.");
        }

        if (request.AssignedDistributorId.HasValue)
        {
            await EnsureDistributorExistsAsync(request.AssignedDistributorId.Value, cancellationToken);
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = passwordHasher.HashPassword(request.Password),
            Role = UserRole.Seller,
            AssignedDistributorId = request.AssignedDistributorId
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken)
            ?? throw new ApiException("Invalid email or password.", 401);

        if (!passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new ApiException("Invalid email or password.", 401);
        }

        return CreateAuthResponse(user);
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .Where(x => x.Id == userId)
            .Select(x => MapProfile(x))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ApiException("User not found.", 404);
    }

    public async Task<IReadOnlyCollection<UserProfileDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .OrderBy(x => x.Role)
            .ThenBy(x => x.FullName)
            .Select(x => MapProfile(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserProfileDto> UpdateRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new ApiException("User not found.", 404);

        if (request.Role == UserRole.Seller && !request.AssignedDistributorId.HasValue)
        {
            throw new ApiException("Sellers must be assigned to a distributor.");
        }

        if (request.AssignedDistributorId.HasValue)
        {
            await EnsureDistributorExistsAsync(request.AssignedDistributorId.Value, cancellationToken);
        }

        user.Role = request.Role;
        user.AssignedDistributorId = request.Role == UserRole.Seller ? request.AssignedDistributorId : null;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetProfileAsync(userId, cancellationToken);
    }

    private async Task EnsureDistributorExistsAsync(Guid distributorId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Users.AnyAsync(x => x.Id == distributorId && x.Role == UserRole.Distributor, cancellationToken);
        if (!exists)
        {
            throw new ApiException("Assigned distributor was not found.", 404);
        }
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var (token, expiresAtUtc) = tokenService.CreateToken(user);
        return new AuthResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            User = new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                AssignedDistributorId = user.AssignedDistributorId,
                CreatedAtUtc = user.CreatedAtUtc
            }
        };
    }

    private static UserProfileDto MapProfile(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        AssignedDistributorId = user.AssignedDistributorId,
        CreatedAtUtc = user.CreatedAtUtc
    };
}
