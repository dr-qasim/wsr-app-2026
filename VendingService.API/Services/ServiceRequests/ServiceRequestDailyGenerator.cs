using Microsoft.EntityFrameworkCore;
using VendingService.API.Data;
using VendingService.API.Models;

namespace VendingService.API.Services.ServiceRequests;

public sealed class ServiceRequestDailyGenerator : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ServiceRequestDailyGenerator(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await GenerateAsync(DateOnly.FromDateTime(DateTime.Today), stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await GenerateAsync(DateOnly.FromDateTime(DateTime.Today), stoppingToken);
        }
    }

    private async Task GenerateAsync(DateOnly targetDate, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VendingServiceDbContext>();

        var planTypeId = await db.ServiceRequestTypes
            .AsNoTracking()
            .Where(x => x.Name == "Плановое техническое обслуживание")
            .Select(x => x.ServiceRequestTypeId)
            .FirstOrDefaultAsync(cancellationToken);
        var newStatusId = await db.ServiceRequestStatuses
            .AsNoTracking()
            .Where(x => x.Name == "Новая")
            .Select(x => x.ServiceRequestStatusId)
            .FirstOrDefaultAsync(cancellationToken);
        var inProgressStatusId = await db.ServiceRequestStatuses
            .AsNoTracking()
            .Where(x => x.Name == "В работе")
            .Select(x => x.ServiceRequestStatusId)
            .FirstOrDefaultAsync(cancellationToken);

        if (planTypeId == 0 || newStatusId == 0 || inProgressStatusId == 0)
        {
            return;
        }

        var openMachineIds = await db.ServiceRequests
            .AsNoTracking()
            .Where(x => x.ServiceRequestStatusId == newStatusId || x.ServiceRequestStatusId == inProgressStatusId)
            .Select(x => x.VendingMachineId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var machines = await db.VendingMachines
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dueMachines = machines
            .Select(x => new { x.VendingMachineId, Planned = GetPlannedDate(x) })
            .Where(x => x.Planned.HasValue && x.Planned.Value <= targetDate)
            .ToList();

        var toCreate = dueMachines
            .Where(x => !openMachineIds.Contains(x.VendingMachineId))
            .ToList();

        if (toCreate.Count == 0)
        {
            return;
        }

        var now = DateTime.Now;
        var newRequests = new List<ServiceRequest>();

        foreach (var vm in toCreate)
        {
            var planned = vm.Planned ?? targetDate;
            var entity = new ServiceRequest
            {
                VendingMachineId = vm.VendingMachineId,
                ServiceRequestTypeId = planTypeId,
                ServiceRequestStatusId = newStatusId,
                PlannedDate = planned,
                AssignedUserAccountId = null,
                SortOrder = null,
                Notes = null,
                DeclineReason = null,
                CreatedAt = now
            };

            db.ServiceRequests.Add(entity);
            newRequests.Add(entity);
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var entity in newRequests)
        {
            db.ServiceRequestStatusHistories.Add(new ServiceRequestStatusHistory
            {
                ServiceRequestId = entity.ServiceRequestId,
                ServiceRequestStatusId = newStatusId,
                ChangedAt = now,
                ChangedByUserAccountId = null
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static DateOnly? GetPlannedDate(VendingMachine machine)
    {
        var planned = machine.NextVerificationDate;
        if (!planned.HasValue && machine.LastVerificationDate.HasValue && machine.VerificationIntervalMonths.HasValue)
        {
            planned = machine.LastVerificationDate.Value.AddMonths(machine.VerificationIntervalMonths.Value);
        }

        if (machine.ResourceHours.HasValue)
        {
            var created = machine.CreatedAt.Date;
            var usedHours = (DateTime.Today - created).TotalDays * 8;
            var threshold = machine.ResourceHours.Value * 0.85;
            var remaining = Math.Max(0, threshold - usedHours);
            var daysLeft = remaining / 8;
            var resourceDue = DateOnly.FromDateTime(DateTime.Today.AddDays(daysLeft));

            if (!planned.HasValue || resourceDue < planned.Value)
            {
                planned = resourceDue;
            }
        }

        return planned;
    }
}
