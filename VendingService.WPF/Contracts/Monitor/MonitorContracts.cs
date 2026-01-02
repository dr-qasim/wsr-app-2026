namespace VendingService.WPF.Contracts.Monitor;

public sealed record MonitorLookupItem(int Id, string Name);

public sealed record MonitorAdditionalStatusItem(string Code, string Name);

public sealed record MonitorLookups(
    IReadOnlyList<MonitorLookupItem> ConnectionTypes,
    IReadOnlyList<MonitorAdditionalStatusItem> AdditionalStatuses,
    IReadOnlyList<MonitorLookupItem> VendingMachineStatuses);

public sealed record MonitorSnapshotResponse(
    DateTime GeneratedAt,
    int LastEventId,
    IReadOnlyList<MonitorVendingMachineItem> Items,
    IReadOnlyList<MonitorEventItem> NewEvents);

public sealed record MonitorVendingMachineItem(
    int VendingMachineId,
    string Name,
    string ManufacturerName,
    string ModelName,
    string? CompanyName,
    string? ProviderName,
    string Address,
    string Place,
    decimal MoneyOnAccount,
    int VendingMachineStatusId,
    string VendingMachineStatusName,
    int? ConnectionTypeId,
    string? ConnectionTypeName,
    bool IsOnline,
    int LoadOverallPercent,
    int LoadMinPercent,
    decimal CashCoins,
    decimal CashBills,
    decimal CashChange,
    int EquipmentOkCount,
    int EquipmentTotalCount,
    IReadOnlyList<string> AdditionalStatusCodes,
    IReadOnlyList<MonitorEventPreview> RecentEvents);

public sealed record MonitorEventPreview(
    int VendingMachineEventId,
    string EventTypeName,
    string SeverityName,
    DateTime OccurredAt);

public sealed record MonitorEventItem(
    int VendingMachineEventId,
    int VendingMachineId,
    string VendingMachineName,
    string EventTypeName,
    string SeverityName,
    string? Message,
    DateTime OccurredAt);
