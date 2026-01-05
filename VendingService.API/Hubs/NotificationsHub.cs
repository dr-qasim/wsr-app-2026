using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace VendingService.API.Hubs;

[Authorize]
public sealed class NotificationsHub : Hub
{
    public const string ReceiveMethodName = "notification";
}
