namespace VendingService.API.Models;

public sealed class ServicePriority
{
    public int ServicePriorityId { get; set; }
    public required string Name { get; set; }
    public int SortOrder { get; set; }
}

