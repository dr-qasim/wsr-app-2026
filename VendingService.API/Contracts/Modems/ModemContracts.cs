using System.ComponentModel.DataAnnotations;

namespace VendingService.API.Contracts.Modems;

public sealed record ModemListItem(
    int ModemId,
    string ModemNumber,
    string? Imei,
    string? SimPhoneNumber,
    int? ProviderId,
    string? ProviderName,
    int? ConnectionTypeId,
    string? ConnectionTypeName);

public sealed record ModemDetails(
    int ModemId,
    string ModemNumber,
    string? Imei,
    string? SimPhoneNumber,
    int? ProviderId,
    string? ProviderName,
    int? ConnectionTypeId,
    string? ConnectionTypeName,
    string? Notes,
    DateTime CreatedAt);

public sealed record CreateModemRequest(
    [Required] string ModemNumber,
    string? Imei,
    string? SimPhoneNumber,
    int? ProviderId,
    int? ConnectionTypeId,
    string? Notes);

public sealed record UpdateModemRequest(
    [Required] string ModemNumber,
    string? Imei,
    string? SimPhoneNumber,
    int? ProviderId,
    int? ConnectionTypeId,
    string? Notes);
