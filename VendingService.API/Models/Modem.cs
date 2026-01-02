namespace VendingService.API.Models;

public sealed class Modem
{
    public int ModemId { get; set; }
    public required string ModemNumber { get; set; }

    public string? Imei { get; set; }
    public string? SimPhoneNumber { get; set; }

    public int? ProviderId { get; set; }
    public Provider? Provider { get; set; }

    public int? ConnectionTypeId { get; set; }
    public ConnectionType? ConnectionType { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

