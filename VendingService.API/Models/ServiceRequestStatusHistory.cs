namespace VendingService.API.Models;

public sealed class ServiceRequestStatusHistory
{
    public int ServiceRequestStatusHistoryId { get; set; }

    public int ServiceRequestId { get; set; }
    public ServiceRequest? ServiceRequest { get; set; }

    public int ServiceRequestStatusId { get; set; }
    public ServiceRequestStatus? ServiceRequestStatus { get; set; }

    public int? ChangedByUserAccountId { get; set; }
    public UserAccount? ChangedByUserAccount { get; set; }

    public DateTime ChangedAt { get; set; }
}
