namespace VendingService.API.Models;

public sealed class VendingMachineEvent
{
    public int VendingMachineEventId { get; set; }

    public int VendingMachineId { get; set; }
    public VendingMachine? VendingMachine { get; set; }

    public int EventTypeId { get; set; }
    public EventType? EventType { get; set; }

    public DateTime OccurredAt { get; set; }
    public string? Message { get; set; }
}

