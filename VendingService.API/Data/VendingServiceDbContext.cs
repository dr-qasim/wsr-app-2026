using Microsoft.EntityFrameworkCore;
using VendingService.API.Models;

namespace VendingService.API.Data;

public sealed class VendingServiceDbContext(DbContextOptions<VendingServiceDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<ConnectionType> ConnectionTypes => Set<ConnectionType>();
    public DbSet<CriticalValuesTemplate> CriticalValuesTemplates => Set<CriticalValuesTemplate>();
    public DbSet<EquipmentType> EquipmentTypes => Set<EquipmentType>();
    public DbSet<EventSeverity> EventSeverities => Set<EventSeverity>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<Maintenance> Maintenances => Set<Maintenance>();
    public DbSet<Modem> Modems => Set<Modem>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<PaymentSystem> PaymentSystems => Set<PaymentSystem>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductMatrix> ProductMatrices => Set<ProductMatrix>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SalePaymentMethod> SalePaymentMethods => Set<SalePaymentMethod>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<ServiceRequestStatus> ServiceRequestStatuses => Set<ServiceRequestStatus>();
    public DbSet<ServiceRequestStatusHistory> ServiceRequestStatusHistories => Set<ServiceRequestStatusHistory>();
    public DbSet<ServiceRequestType> ServiceRequestTypes => Set<ServiceRequestType>();
    public DbSet<ServicePriority> ServicePriorities => Set<ServicePriority>();
    public DbSet<TimeZoneEntry> TimeZones => Set<TimeZoneEntry>();
    public DbSet<UserAccountVendingMachineModel> UserAccountVendingMachineModels => Set<UserAccountVendingMachineModel>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<VendingMachine> VendingMachines => Set<VendingMachine>();
    public DbSet<VendingMachineEquipment> VendingMachineEquipments => Set<VendingMachineEquipment>();
    public DbSet<VendingMachineEvent> VendingMachineEvents => Set<VendingMachineEvent>();
    public DbSet<VendingMachineManufacturer> VendingMachineManufacturers => Set<VendingMachineManufacturer>();
    public DbSet<VendingMachineModel> VendingMachineModels => Set<VendingMachineModel>();
    public DbSet<VendingMachineProduct> VendingMachineProducts => Set<VendingMachineProduct>();
    public DbSet<VendingMachinePaymentSystem> VendingMachinePaymentSystems => Set<VendingMachinePaymentSystem>();
    public DbSet<VendingMachineStatus> VendingMachineStatuses => Set<VendingMachineStatus>();
    public DbSet<VendingMachineStatusHistory> VendingMachineStatusHistories => Set<VendingMachineStatusHistory>();
    public DbSet<WorkMode> WorkModes => Set<WorkMode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dbo");

        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("Company");
            entity.HasKey(x => x.CompanyId);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Phone).HasMaxLength(32);
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.Address).HasMaxLength(300);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRole");
            entity.HasKey(x => x.UserRoleId);
            entity.Property(x => x.Name).HasMaxLength(50);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<UserAccountVendingMachineModel>(entity =>
        {
            entity.ToTable("UserAccountVendingMachineModel");
            entity.HasKey(x => new { x.UserAccountId, x.VendingMachineModelId });

            entity.HasOne(x => x.UserAccount)
                .WithMany()
                .HasForeignKey(x => x.UserAccountId);

            entity.HasOne(x => x.VendingMachineModel)
                .WithMany()
                .HasForeignKey(x => x.VendingMachineModelId);
        });

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("UserAccount");
            entity.HasKey(x => x.UserAccountId);
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.Phone).HasMaxLength(32);
            entity.Property(x => x.LastName).HasMaxLength(100);
            entity.Property(x => x.FirstName).HasMaxLength(100);
            entity.Property(x => x.Patronymic).HasMaxLength(100);
            entity.Property(x => x.PasswordHash).HasMaxLength(255);
            entity.Property(x => x.PasswordSalt).HasMaxLength(255);
            entity.Property(x => x.PhotoUrl).HasMaxLength(500);

            entity.HasIndex(x => x.Email).IsUnique();

            entity.HasOne(x => x.UserRole)
                .WithMany()
                .HasForeignKey(x => x.UserRoleId);

            entity.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId);
        });

        modelBuilder.Entity<Country>(entity =>
        {
            entity.ToTable("Country");
            entity.HasKey(x => x.CountryId);
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.IsoCode).HasMaxLength(2);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<TimeZoneEntry>(entity =>
        {
            entity.ToTable("TimeZone");
            entity.HasKey(x => x.TimeZoneId);
            entity.Property(x => x.Name).HasMaxLength(50);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<WorkMode>(entity =>
        {
            entity.ToTable("WorkMode");
            entity.HasKey(x => x.WorkModeId);
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<ServicePriority>(entity =>
        {
            entity.ToTable("ServicePriority");
            entity.HasKey(x => x.ServicePriorityId);
            entity.Property(x => x.Name).HasMaxLength(50);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<ServiceRequestType>(entity =>
        {
            entity.ToTable("ServiceRequestType");
            entity.HasKey(x => x.ServiceRequestTypeId);
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<ServiceRequestStatus>(entity =>
        {
            entity.ToTable("ServiceRequestStatus");
            entity.HasKey(x => x.ServiceRequestStatusId);
            entity.Property(x => x.Name).HasMaxLength(50);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            entity.ToTable("ServiceRequest");
            entity.HasKey(x => x.ServiceRequestId);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.DeclineReason).HasMaxLength(500);

            entity.HasOne(x => x.VendingMachine)
                .WithMany()
                .HasForeignKey(x => x.VendingMachineId);

            entity.HasOne(x => x.ServiceRequestType)
                .WithMany()
                .HasForeignKey(x => x.ServiceRequestTypeId);

            entity.HasOne(x => x.ServiceRequestStatus)
                .WithMany()
                .HasForeignKey(x => x.ServiceRequestStatusId);

            entity.HasOne(x => x.AssignedUserAccount)
                .WithMany()
                .HasForeignKey(x => x.AssignedUserAccountId);
        });

        modelBuilder.Entity<ServiceRequestStatusHistory>(entity =>
        {
            entity.ToTable("ServiceRequestStatusHistory");
            entity.HasKey(x => x.ServiceRequestStatusHistoryId);

            entity.HasOne(x => x.ServiceRequest)
                .WithMany()
                .HasForeignKey(x => x.ServiceRequestId);

            entity.HasOne(x => x.ServiceRequestStatus)
                .WithMany()
                .HasForeignKey(x => x.ServiceRequestStatusId);

            entity.HasOne(x => x.ChangedByUserAccount)
                .WithMany()
                .HasForeignKey(x => x.ChangedByUserAccountId);
        });

        modelBuilder.Entity<VendingMachineStatus>(entity =>
        {
            entity.ToTable("VendingMachineStatus");
            entity.HasKey(x => x.VendingMachineStatusId);
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<VendingMachineManufacturer>(entity =>
        {
            entity.ToTable("VendingMachineManufacturer");
            entity.HasKey(x => x.VendingMachineManufacturerId);
            entity.Property(x => x.Name).HasMaxLength(150);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<VendingMachineModel>(entity =>
        {
            entity.ToTable("VendingMachineModel");
            entity.HasKey(x => x.VendingMachineModelId);
            entity.Property(x => x.Name).HasMaxLength(150);
            entity.HasIndex(x => new { x.VendingMachineManufacturerId, x.Name }).IsUnique();

            entity.HasOne(x => x.VendingMachineManufacturer)
                .WithMany()
                .HasForeignKey(x => x.VendingMachineManufacturerId);
        });

        modelBuilder.Entity<ProductMatrix>(entity =>
        {
            entity.ToTable("ProductMatrix");
            entity.HasKey(x => x.ProductMatrixId);
            entity.Property(x => x.Name).HasMaxLength(150);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<CriticalValuesTemplate>(entity =>
        {
            entity.ToTable("CriticalValuesTemplate");
            entity.HasKey(x => x.CriticalValuesTemplateId);
            entity.Property(x => x.Name).HasMaxLength(150);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.ToTable("NotificationTemplate");
            entity.HasKey(x => x.NotificationTemplateId);
            entity.Property(x => x.Name).HasMaxLength(150);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<PaymentSystem>(entity =>
        {
            entity.ToTable("PaymentSystem");
            entity.HasKey(x => x.PaymentSystemId);
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Provider>(entity =>
        {
            entity.ToTable("Provider");
            entity.HasKey(x => x.ProviderId);
            entity.Property(x => x.Name).HasMaxLength(150);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<ConnectionType>(entity =>
        {
            entity.ToTable("ConnectionType");
            entity.HasKey(x => x.ConnectionTypeId);
            entity.Property(x => x.Name).HasMaxLength(50);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Modem>(entity =>
        {
            entity.ToTable("Modem");
            entity.HasKey(x => x.ModemId);
            entity.Property(x => x.ModemNumber).HasMaxLength(50);
            entity.Property(x => x.Imei).HasMaxLength(32);
            entity.Property(x => x.SimPhoneNumber).HasMaxLength(32);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.HasIndex(x => x.ModemNumber).IsUnique();
            entity.HasIndex(x => x.Imei).IsUnique();

            entity.HasOne(x => x.Provider)
                .WithMany()
                .HasForeignKey(x => x.ProviderId);

            entity.HasOne(x => x.ConnectionType)
                .WithMany()
                .HasForeignKey(x => x.ConnectionTypeId);
        });

        modelBuilder.Entity<VendingMachine>(entity =>
        {
            entity.ToTable("VendingMachine");
            entity.HasKey(x => x.VendingMachineId);

            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Address).HasMaxLength(300);
            entity.Property(x => x.Place).HasMaxLength(200);
            entity.Property(x => x.InventoryNumber).HasMaxLength(50);
            entity.Property(x => x.SerialNumber).HasMaxLength(50);
            entity.Property(x => x.KitOnlineCashRegisterId).HasMaxLength(50);
            entity.Property(x => x.Notes).HasMaxLength(1000);

            entity.HasIndex(x => x.InventoryNumber).IsUnique();
            entity.HasIndex(x => x.SerialNumber).IsUnique();

            entity.Property(x => x.NextVerificationDate)
                .HasComputedColumnSql("CASE WHEN [LastVerificationDate] IS NULL OR [VerificationIntervalMonths] IS NULL THEN NULL ELSE DATEADD(MONTH, [VerificationIntervalMonths], [LastVerificationDate]) END", stored: false);

            entity.HasOne(x => x.VendingMachineModel)
                .WithMany()
                .HasForeignKey(x => x.VendingMachineModelId);

            entity.HasOne(x => x.WorkMode)
                .WithMany()
                .HasForeignKey(x => x.WorkModeId);

            entity.HasOne(x => x.TimeZone)
                .WithMany()
                .HasForeignKey(x => x.TimeZoneId);

            entity.HasOne(x => x.VendingMachineStatus)
                .WithMany()
                .HasForeignKey(x => x.VendingMachineStatusId);

            entity.HasOne(x => x.ServicePriority)
                .WithMany()
                .HasForeignKey(x => x.ServicePriorityId);

            entity.HasOne(x => x.ProductMatrix)
                .WithMany()
                .HasForeignKey(x => x.ProductMatrixId);

            entity.HasOne(x => x.Country)
                .WithMany()
                .HasForeignKey(x => x.CountryId);

            entity.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId);

            entity.HasOne(x => x.Modem)
                .WithMany()
                .HasForeignKey(x => x.ModemId);

            entity.HasOne(x => x.CriticalValuesTemplate)
                .WithMany()
                .HasForeignKey(x => x.CriticalValuesTemplateId);

            entity.HasOne(x => x.NotificationTemplate)
                .WithMany()
                .HasForeignKey(x => x.NotificationTemplateId);

            entity.HasOne(x => x.LastVerificationUserAccount)
                .WithMany()
                .HasForeignKey(x => x.LastVerificationUserAccountId);

            entity.HasOne(x => x.ManagerUserAccount)
                .WithMany()
                .HasForeignKey(x => x.ManagerUserAccountId);

            entity.HasOne(x => x.EngineerUserAccount)
                .WithMany()
                .HasForeignKey(x => x.EngineerUserAccountId);

            entity.HasOne(x => x.TechnicianOperatorUserAccount)
                .WithMany()
                .HasForeignKey(x => x.TechnicianOperatorUserAccountId);
        });

        modelBuilder.Entity<VendingMachinePaymentSystem>(entity =>
        {
            entity.ToTable("VendingMachinePaymentSystem");
            entity.HasKey(x => new { x.VendingMachineId, x.PaymentSystemId });

            entity.HasOne(x => x.VendingMachine)
                .WithMany(x => x.PaymentSystems)
                .HasForeignKey(x => x.VendingMachineId);

            entity.HasOne(x => x.PaymentSystem)
                .WithMany()
                .HasForeignKey(x => x.PaymentSystemId);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Product");
            entity.HasKey(x => x.ProductId);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<VendingMachineProduct>(entity =>
        {
            entity.ToTable("VendingMachineProduct");
            entity.HasKey(x => new { x.VendingMachineId, x.ProductId });
            entity.Property(x => x.AverageDailySales).HasPrecision(10, 2);

            entity.HasOne(x => x.VendingMachine)
                .WithMany()
                .HasForeignKey(x => x.VendingMachineId);

            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId);
        });

        modelBuilder.Entity<SalePaymentMethod>(entity =>
        {
            entity.ToTable("SalePaymentMethod");
            entity.HasKey(x => x.SalePaymentMethodId);
            entity.Property(x => x.Name).HasMaxLength(50);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.ToTable("Sale");
            entity.HasKey(x => x.SaleId);

            entity.HasOne(x => x.VendingMachine)
                .WithMany()
                .HasForeignKey(x => x.VendingMachineId);

            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId);

            entity.HasOne(x => x.SalePaymentMethod)
                .WithMany()
                .HasForeignKey(x => x.SalePaymentMethodId);
        });

        modelBuilder.Entity<EquipmentType>(entity =>
        {
            entity.ToTable("EquipmentType");
            entity.HasKey(x => x.EquipmentTypeId);
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<VendingMachineEquipment>(entity =>
        {
            entity.ToTable("VendingMachineEquipment");
            entity.HasKey(x => new { x.VendingMachineId, x.EquipmentTypeId });

            entity.HasOne(x => x.VendingMachine)
                .WithMany()
                .HasForeignKey(x => x.VendingMachineId);

            entity.HasOne(x => x.EquipmentType)
                .WithMany()
                .HasForeignKey(x => x.EquipmentTypeId);
        });

        modelBuilder.Entity<EventSeverity>(entity =>
        {
            entity.ToTable("EventSeverity");
            entity.HasKey(x => x.EventSeverityId);
            entity.Property(x => x.Name).HasMaxLength(50);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<EventType>(entity =>
        {
            entity.ToTable("EventType");
            entity.HasKey(x => x.EventTypeId);
            entity.Property(x => x.Name).HasMaxLength(150);
            entity.HasIndex(x => x.Name).IsUnique();

            entity.HasOne(x => x.EventSeverity)
                .WithMany()
                .HasForeignKey(x => x.EventSeverityId);
        });

        modelBuilder.Entity<VendingMachineEvent>(entity =>
        {
            entity.ToTable("VendingMachineEvent");
            entity.HasKey(x => x.VendingMachineEventId);
            entity.Property(x => x.Message).HasMaxLength(500);

            entity.HasOne(x => x.VendingMachine)
                .WithMany()
                .HasForeignKey(x => x.VendingMachineId);

            entity.HasOne(x => x.EventType)
                .WithMany()
                .HasForeignKey(x => x.EventTypeId);
        });

        modelBuilder.Entity<VendingMachineStatusHistory>(entity =>
        {
            entity.ToTable("VendingMachineStatusHistory");
            entity.HasKey(x => x.VendingMachineStatusHistoryId);

            entity.HasOne(x => x.VendingMachine)
                .WithMany()
                .HasForeignKey(x => x.VendingMachineId);

            entity.HasOne(x => x.VendingMachineStatus)
                .WithMany()
                .HasForeignKey(x => x.VendingMachineStatusId);

            entity.HasOne(x => x.ChangedByUserAccount)
                .WithMany()
                .HasForeignKey(x => x.ChangedByUserAccountId);
        });

        modelBuilder.Entity<Maintenance>(entity =>
        {
            entity.ToTable("Maintenance");
            entity.HasKey(x => x.MaintenanceId);
            entity.Property(x => x.WorkDescription).HasMaxLength(1000);
            entity.Property(x => x.Problems).HasMaxLength(1000);

            entity.HasOne(x => x.VendingMachine)
                .WithMany()
                .HasForeignKey(x => x.VendingMachineId);

            entity.HasOne(x => x.ExecutorUserAccount)
                .WithMany()
                .HasForeignKey(x => x.ExecutorUserAccountId);
        });
    }
}
