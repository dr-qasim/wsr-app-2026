namespace VendingService.WPF.Contracts.Lookups;

public sealed record LookupItem(int Id, string Name);

public sealed record VendingMachineModelItem(
    int Id,
    string Name,
    int ManufacturerId,
    string ManufacturerName);

public sealed record ModemItem(
    int Id,
    string ModemNumber,
    int? ProviderId,
    string? ProviderName,
    int? ConnectionTypeId,
    string? ConnectionTypeName);

public sealed record VendingMachineCreateLookups(
    IReadOnlyList<LookupItem> Countries,
    IReadOnlyList<LookupItem> TimeZones,
    IReadOnlyList<LookupItem> WorkModes,
    IReadOnlyList<LookupItem> ServicePriorities,
    IReadOnlyList<LookupItem> VendingMachineStatuses,
    IReadOnlyList<LookupItem> ProductMatrices,
    IReadOnlyList<LookupItem> CriticalValuesTemplates,
    IReadOnlyList<LookupItem> NotificationTemplates,
    IReadOnlyList<LookupItem> PaymentSystems,
    IReadOnlyList<LookupItem> Companies,
    IReadOnlyList<ModemItem> Modems,
    IReadOnlyList<LookupItem> Manufacturers,
    IReadOnlyList<VendingMachineModelItem> Models);

