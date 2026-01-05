namespace VendingService.API.Models;

public sealed class ServiceRequestStatus
{
    public int ServiceRequestStatusId { get; set; }
    public required string Name { get; set; }
    public int SortOrder { get; set; }
}
