namespace VendingService.API.Models;

public sealed class EventType
{
    public int EventTypeId { get; set; }
    public int EventSeverityId { get; set; }
    public EventSeverity? EventSeverity { get; set; }
    public required string Name { get; set; }
}

