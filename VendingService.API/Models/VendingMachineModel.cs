namespace VendingService.API.Models;

public sealed class VendingMachineModel
{
    public int VendingMachineModelId { get; set; }
    public int VendingMachineManufacturerId { get; set; }
    public VendingMachineManufacturer? VendingMachineManufacturer { get; set; }
    public required string Name { get; set; }
}

