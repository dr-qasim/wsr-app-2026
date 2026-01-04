using System.ComponentModel.DataAnnotations;

namespace VendingService.API.Contracts.Maintenance;

public sealed record MaintenanceListItem(
    int MaintenanceId,
    int VendingMachineId,
    string VendingMachineName,
    DateOnly MaintenanceDate,
    string? WorkDescription,
    string? Problems,
    int? ExecutorUserAccountId,
    string? ExecutorName);

public sealed record MaintenanceDetails(
    int MaintenanceId,
    int VendingMachineId,
    string VendingMachineName,
    DateOnly MaintenanceDate,
    string? WorkDescription,
    string? Problems,
    int? ExecutorUserAccountId,
    string? ExecutorName,
    DateTime CreatedAt);

public sealed record CreateMaintenanceRequest(
    [Required] int VendingMachineId,
    [Required] DateOnly MaintenanceDate,
    string? WorkDescription,
    string? Problems,
    int? ExecutorUserAccountId);

public sealed record UpdateMaintenanceRequest(
    [Required] int VendingMachineId,
    [Required] DateOnly MaintenanceDate,
    string? WorkDescription,
    string? Problems,
    int? ExecutorUserAccountId);
