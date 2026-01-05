namespace VendingService.API.Models;

public sealed class ServiceRequest
{
    public int ServiceRequestId { get; set; }

    public int VendingMachineId { get; set; }
    public VendingMachine? VendingMachine { get; set; }

    public int ServiceRequestTypeId { get; set; }
    public ServiceRequestType? ServiceRequestType { get; set; }

    public int ServiceRequestStatusId { get; set; }
    public ServiceRequestStatus? ServiceRequestStatus { get; set; }

    public DateOnly PlannedDate { get; set; }
    public int? AssignedUserAccountId { get; set; }
    public UserAccount? AssignedUserAccount { get; set; }
    public int? SortOrder { get; set; }

    public string? Notes { get; set; }
    public string? DeclineReason { get; set; }

    public DateTime CreatedAt { get; set; }
}
