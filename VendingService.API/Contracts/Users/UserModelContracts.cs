using System.ComponentModel.DataAnnotations;

namespace VendingService.API.Contracts.Users;

public sealed record UserModelItem(
    int VendingMachineModelId,
    string ModelName,
    int ManufacturerId,
    string ManufacturerName);

public sealed record UpdateUserModelsRequest(
    [Required] List<int> VendingMachineModelIds);
