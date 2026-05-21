using CozyComfort.Application.Common.Exceptions;
using CozyComfort.Application.DTOs;
using CozyComfort.Application.Interfaces;
using CozyComfort.Domain.Entities;
using CozyComfort.Domain.Enums;
using CozyComfort.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CozyComfort.Infrastructure.Services;

public sealed class NotificationService(CozyComfortDbContext dbContext) : INotificationService
{
    public async Task<IReadOnlyCollection<NotificationDto>> GetNotificationsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Notifications
            .Where(x => x.RecipientUserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new NotificationDto
            {
                Id = x.Id,
                Title = x.Title,
                Message = x.Message,
                Type = x.Type,
                IsRead = x.IsRead,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(x => x.Id == notificationId && x.RecipientUserId == userId, cancellationToken)
            ?? throw new ApiException("Notification not found.", 404);

        notification.IsRead = true;
        notification.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateNotificationAsync(Guid recipientUserId, string title, string message, NotificationType type, CancellationToken cancellationToken = default)
    {
        dbContext.Notifications.Add(new Notification
        {
            RecipientUserId = recipientUserId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
