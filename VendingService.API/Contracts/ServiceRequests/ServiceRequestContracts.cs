using System.ComponentModel.DataAnnotations;

namespace VendingService.API.Contracts.ServiceRequests;

public sealed record ServiceRequestListItem(
    int ServiceRequestId,
    int VendingMachineId,
    string VendingMachineName,
    int ServiceRequestTypeId,
    string ServiceRequestTypeName,
    int ServiceRequestStatusId,
    string ServiceRequestStatusName,
    DateOnly PlannedDate,
    int? AssignedUserAccountId,
    string? AssignedUserName,
    int? SortOrder);

public sealed record ServiceRequestDetails(
    int ServiceRequestId,
    int VendingMachineId,
    string VendingMachineName,
    int ServiceRequestTypeId,
    string ServiceRequestTypeName,
    int ServiceRequestStatusId,
    string ServiceRequestStatusName,
    DateOnly PlannedDate,
    int? AssignedUserAccountId,
    string? AssignedUserName,
    int? SortOrder,
    string? Notes,
    string? DeclineReason,
    DateTime CreatedAt);

public sealed record CreateServiceRequestRequest(
    [Required] int VendingMachineId,
    [Required] int ServiceRequestTypeId,
    [Required] DateOnly PlannedDate,
    int? AssignedUserAccountId,
    int? SortOrder,
    string? Notes);

public sealed record UpdateServiceRequestRequest(
    [Required] int VendingMachineId,
    [Required] int ServiceRequestTypeId,
    [Required] DateOnly PlannedDate,
    int? AssignedUserAccountId,
    int? SortOrder,
    string? Notes);

public sealed record UpdateServiceRequestScheduleRequest(
    [Required] DateOnly PlannedDate,
    int? AssignedUserAccountId,
    int? SortOrder);

public sealed record ChangeServiceRequestStatusRequest(
    [Required] int ServiceRequestStatusId,
    int? ChangedByUserAccountId,
    string? DeclineReason);

public sealed record ServiceRequestLookupItem(int Id, string Name);

public sealed record ServiceRequestLookups(
    IReadOnlyList<ServiceRequestLookupItem> Types,
    IReadOnlyList<ServiceRequestLookupItem> Statuses);
