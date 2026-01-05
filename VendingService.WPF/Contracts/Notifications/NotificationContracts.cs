namespace VendingService.WPF.Contracts.Notifications;

public sealed record NotificationEventItem(
    int VendingMachineEventId,
    int VendingMachineId,
    string VendingMachineName,
    string EventTypeName,
    string SeverityName,
    string? Message,
    DateTime OccurredAt);
