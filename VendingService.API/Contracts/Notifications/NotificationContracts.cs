namespace VendingService.API.Contracts.Notifications;

public sealed record CreateNotificationRequest(
    int VendingMachineId,
    int EventTypeId,
    string? Message);

public sealed record NotificationEventItem(
    int VendingMachineEventId,
    int VendingMachineId,
    string VendingMachineName,
    string EventTypeName,
    string SeverityName,
    string? Message,
    DateTime OccurredAt);
