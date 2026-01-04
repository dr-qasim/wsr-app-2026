namespace VendingService.API.Models;

public sealed class Maintenance
{
    public int MaintenanceId { get; set; }

    public int VendingMachineId { get; set; }
    public VendingMachine? VendingMachine { get; set; }

    public DateOnly MaintenanceDate { get; set; }
    public string? WorkDescription { get; set; }
    public string? Problems { get; set; }

    public int? ExecutorUserAccountId { get; set; }
    public UserAccount? ExecutorUserAccount { get; set; }

    public DateTime CreatedAt { get; set; }
}
