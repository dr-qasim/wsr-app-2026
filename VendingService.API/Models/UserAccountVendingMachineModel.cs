namespace VendingService.API.Models;

public sealed class UserAccountVendingMachineModel
{
    public int UserAccountId { get; set; }
    public UserAccount? UserAccount { get; set; }

    public int VendingMachineModelId { get; set; }
    public VendingMachineModel? VendingMachineModel { get; set; }
}
