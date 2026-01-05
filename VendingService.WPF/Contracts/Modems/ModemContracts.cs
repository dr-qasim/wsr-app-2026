namespace VendingService.WPF.Contracts.Modems;

public sealed record ModemListItem(
    int ModemId,
    string ModemNumber,
    string? Imei,
    string? SimPhoneNumber,
    int? ProviderId,
    string? ProviderName,
    int? ConnectionTypeId,
    string? ConnectionTypeName);
