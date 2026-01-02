namespace VendingService.API.Models;

public sealed class CriticalValuesTemplate
{
    public int CriticalValuesTemplateId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

