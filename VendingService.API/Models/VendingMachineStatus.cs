namespace VendingService.API.Models;

public sealed class VendingMachineStatus
{
    public int VendingMachineStatusId { get; set; }
    public required string Name { get; set; }
    public int SortOrder { get; set; }
}

