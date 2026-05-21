using CozyComfort.Domain.Common;
using CozyComfort.Domain.Enums;

namespace CozyComfort.Domain.Entities;

public sealed class Notification : BaseEntity
{
    public Guid RecipientUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }

    public User RecipientUser { get; set; } = null!;
}
