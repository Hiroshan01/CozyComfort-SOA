using CozyComfort.Application.DTOs;
using CozyComfort.Domain.Enums;

namespace CozyComfort.Application.Interfaces;

public interface INotificationService
{
    Task<IReadOnlyCollection<NotificationDto>> GetNotificationsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default);
    Task CreateNotificationAsync(Guid recipientUserId, string title, string message, NotificationType type, CancellationToken cancellationToken = default);
}
