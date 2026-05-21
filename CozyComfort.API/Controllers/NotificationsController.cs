using CozyComfort.API.Extensions;
using CozyComfort.Application.DTOs;
using CozyComfort.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CozyComfort.API.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<NotificationDto>>> GetNotifications(CancellationToken cancellationToken)
        => Ok(await notificationService.GetNotificationsAsync(User.GetUserId(), cancellationToken));

    [HttpPatch("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid notificationId, CancellationToken cancellationToken)
    {
        await notificationService.MarkAsReadAsync(User.GetUserId(), notificationId, cancellationToken);
        return NoContent();
    }
}
