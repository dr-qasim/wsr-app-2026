namespace VendingService.API.Models;

public sealed class Country
{
    public int CountryId { get; set; }
    public required string Name { get; set; }
    public string? IsoCode { get; set; }
}

