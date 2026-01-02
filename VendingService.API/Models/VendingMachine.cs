namespace VendingService.API.Models;

public sealed class VendingMachine
{
    public int VendingMachineId { get; set; }
    public required string Name { get; set; }

    public int VendingMachineModelId { get; set; }
    public VendingMachineModel? VendingMachineModel { get; set; }

    public int WorkModeId { get; set; }
    public WorkMode? WorkMode { get; set; }

    public int TimeZoneId { get; set; }
    public TimeZoneEntry? TimeZone { get; set; }

    public int VendingMachineStatusId { get; set; }
    public VendingMachineStatus? VendingMachineStatus { get; set; }

    public int ServicePriorityId { get; set; }
    public ServicePriority? ServicePriority { get; set; }

    public int ProductMatrixId { get; set; }
    public ProductMatrix? ProductMatrix { get; set; }

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public int? ModemId { get; set; }
    public Modem? Modem { get; set; }

    public required string Address { get; set; }
    public required string Place { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public required string InventoryNumber { get; set; }
    public required string SerialNumber { get; set; }

    public DateOnly ManufactureDate { get; set; }
    public DateOnly CommissioningDate { get; set; }
    public DateOnly? LastVerificationDate { get; set; }
    public int? VerificationIntervalMonths { get; set; }
    public DateOnly? NextVerificationDate { get; private set; }
    public int? ResourceHours { get; set; }
    public DateOnly? NextServiceDate { get; set; }
    public byte? ServiceDurationHours { get; set; }
    public DateOnly? InventoryDate { get; set; }

    public int CountryId { get; set; }
    public Country? Country { get; set; }

    public int? LastVerificationUserAccountId { get; set; }
    public UserAccount? LastVerificationUserAccount { get; set; }

    public TimeOnly? WorkingTimeFrom { get; set; }
    public TimeOnly? WorkingTimeTo { get; set; }

    public int? CriticalValuesTemplateId { get; set; }
    public CriticalValuesTemplate? CriticalValuesTemplate { get; set; }

    public int? NotificationTemplateId { get; set; }
    public NotificationTemplate? NotificationTemplate { get; set; }

    public int? ManagerUserAccountId { get; set; }
    public UserAccount? ManagerUserAccount { get; set; }

    public int? EngineerUserAccountId { get; set; }
    public UserAccount? EngineerUserAccount { get; set; }

    public int? TechnicianOperatorUserAccountId { get; set; }
    public UserAccount? TechnicianOperatorUserAccount { get; set; }

    public string? KitOnlineCashRegisterId { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<VendingMachinePaymentSystem> PaymentSystems { get; set; } = [];
}

