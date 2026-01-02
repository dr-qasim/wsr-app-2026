namespace VendingService.API.Models;

public sealed class VendingMachineEquipment
{
    public int VendingMachineId { get; set; }
    public VendingMachine? VendingMachine { get; set; }

    public int EquipmentTypeId { get; set; }
    public EquipmentType? EquipmentType { get; set; }

    public bool IsOperational { get; set; }
    public DateTime UpdatedAt { get; set; }
}

