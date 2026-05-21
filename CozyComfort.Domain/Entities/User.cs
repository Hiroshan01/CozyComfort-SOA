using CozyComfort.Domain.Common;
using CozyComfort.Domain.Enums;

namespace CozyComfort.Domain.Entities;

public sealed class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? AssignedDistributorId { get; set; }

    public User? AssignedDistributor { get; set; }
    public ICollection<User> AssignedSellers { get; set; } = new List<User>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
