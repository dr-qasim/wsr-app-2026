namespace VendingService.API.Models;

public sealed class TimeZoneEntry
{
    public int TimeZoneId { get; set; }
    public required string Name { get; set; }
    public short UtcOffsetMinutes { get; set; }
}

