namespace VendingService.API.Models;

public sealed class ServiceRequestType
{
    public int ServiceRequestTypeId { get; set; }
    public required string Name { get; set; }
}
