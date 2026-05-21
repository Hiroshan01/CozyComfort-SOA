using CozyComfort.Domain.Enums;

namespace CozyComfort.Application.DTOs;

public sealed class UserProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? AssignedDistributorId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class UpdateUserRoleRequest
{
    public UserRole Role { get; set; }
    public Guid? AssignedDistributorId { get; set; }
}
