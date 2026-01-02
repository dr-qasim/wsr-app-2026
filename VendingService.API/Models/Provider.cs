namespace VendingService.API.Models;

public sealed class Provider
{
    public int ProviderId { get; set; }
    public required string Name { get; set; }
}

