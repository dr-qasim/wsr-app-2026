namespace VendingService.API.Models;

public sealed class EventSeverity
{
    public int EventSeverityId { get; set; }
    public required string Name { get; set; }
    public int SortOrder { get; set; }
}

