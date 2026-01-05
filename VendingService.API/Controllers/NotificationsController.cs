using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using VendingService.API.Contracts.Notifications;
using VendingService.API.Data;
using VendingService.API.Hubs;
using VendingService.API.Models;

namespace VendingService.API.Controllers;

[ApiController]
[Authorize]
[Route("notifications")]
public sealed class NotificationsController(VendingServiceDbContext db, IHubContext<NotificationsHub> hub) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<NotificationEventItem>> Create(
        [FromBody] CreateNotificationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.VendingMachineId <= 0 || request.EventTypeId <= 0)
        {
            return BadRequest(new { Message = "VendingMachineId and EventTypeId must be positive." });
        }

        var machine = await db.VendingMachines
            .AsNoTracking()
            .Where(x => x.VendingMachineId == request.VendingMachineId)
            .Select(x => new { x.VendingMachineId, x.Name })
            .SingleOrDefaultAsync(cancellationToken);

        if (machine is null)
        {
            return BadRequest(new { Message = "Unknown vending machine id." });
        }

        var eventType = await db.EventTypes
            .AsNoTracking()
            .Include(x => x.EventSeverity)
            .SingleOrDefaultAsync(x => x.EventTypeId == request.EventTypeId, cancellationToken);

        if (eventType is null)
        {
            return BadRequest(new { Message = "Unknown event type id." });
        }

        var message = string.IsNullOrWhiteSpace(request.Message) ? eventType.Name : request.Message.Trim();
        var entity = new VendingMachineEvent
        {
            VendingMachineId = request.VendingMachineId,
            EventTypeId = request.EventTypeId,
            OccurredAt = DateTime.Now,
            Message = message
        };

        db.VendingMachineEvents.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        var item = new NotificationEventItem(
            entity.VendingMachineEventId,
            entity.VendingMachineId,
            machine.Name,
            eventType.Name,
            eventType.EventSeverity?.Name ?? string.Empty,
            entity.Message,
            entity.OccurredAt);

        await hub.Clients.All.SendAsync(NotificationsHub.ReceiveMethodName, item, cancellationToken);

        return Ok(item);
    }
}
