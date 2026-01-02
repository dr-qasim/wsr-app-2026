using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingService.API.Contracts.Dashboard;
using VendingService.API.Data;

namespace VendingService.API.Controllers;

[ApiController]
[Authorize]
[Route("dashboard")]
public sealed class DashboardController(VendingServiceDbContext db) : ControllerBase
{
    private static readonly DashboardNewsItem[] DemoNews =
    [
        new DashboardNewsItem(new DateTime(2025, 1, 29), "Терминалы KitPos получили эквайринг от Сбера"),
        new DashboardNewsItem(new DateTime(2024, 12, 31), "Новогоднее поздравление от KIT Vending / KIT Shop"),
        new DashboardNewsItem(new DateTime(2024, 12, 28), "Ставки НДС 5% и 7% для УСН"),
        new DashboardNewsItem(new DateTime(2024, 12, 4), "Релиз новой CRM-системы KIT Shop"),
        new DashboardNewsItem(new DateTime(2024, 11, 20), "Получение сертификата PCI DSS 4.0.1")
    ];

    [HttpGet("overview")]
    public async Task<ActionResult<DashboardOverviewResponse>> GetOverview(CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var yesterday = today.AddDays(-1);
        var start10Days = today.AddDays(-9);

        var statuses = await db.VendingMachineStatuses
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new { x.VendingMachineStatusId, x.Name })
            .ToListAsync(cancellationToken);

        var machineCountsByStatus = await db.VendingMachines
            .AsNoTracking()
            .GroupBy(x => x.VendingMachineStatusId)
            .Select(g => new { StatusId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var statusCountMap = machineCountsByStatus.ToDictionary(x => x.StatusId, x => x.Count);

        var totalMachines = statusCountMap.Values.Sum();

        var workingStatusId = await db.VendingMachineStatuses
            .AsNoTracking()
            .Where(x => x.Name == "Работает")
            .Select(x => x.VendingMachineStatusId)
            .FirstOrDefaultAsync(cancellationToken);

        var workingMachines = workingStatusId != 0 && statusCountMap.TryGetValue(workingStatusId, out var c) ? c : 0;
        var workingPercent = totalMachines <= 0 ? 0 : (int)Math.Round(workingMachines * 100.0 / totalMachines);

        var networkStatuses = statuses
            .Select(s => new DashboardStatusSlice(
                s.VendingMachineStatusId,
                s.Name,
                statusCountMap.TryGetValue(s.VendingMachineStatusId, out var count) ? count : 0))
            .ToList();

        var totalIncome = await db.Sales
            .AsNoTracking()
            .Select(x => (decimal?)x.TotalAmount)
            .SumAsync(cancellationToken) ?? 0m;

        var incomeToday = await db.Sales
            .AsNoTracking()
            .Where(x => x.SoldAt >= today && x.SoldAt < tomorrow)
            .Select(x => (decimal?)x.TotalAmount)
            .SumAsync(cancellationToken) ?? 0m;

        var incomeYesterday = await db.Sales
            .AsNoTracking()
            .Where(x => x.SoldAt >= yesterday && x.SoldAt < today)
            .Select(x => (decimal?)x.TotalAmount)
            .SumAsync(cancellationToken) ?? 0m;

        var serviceEventTypeId = await db.EventTypes
            .AsNoTracking()
            .Where(x => x.Name == "Необходимость обслуживания")
            .Select(x => x.EventTypeId)
            .FirstOrDefaultAsync(cancellationToken);

        var servicedMachinesToday = 0;
        var servicedMachinesYesterday = 0;

        if (serviceEventTypeId != 0)
        {
            servicedMachinesToday = await db.VendingMachineEvents
                .AsNoTracking()
                .Where(x => x.EventTypeId == serviceEventTypeId && x.OccurredAt >= today && x.OccurredAt < tomorrow)
                .Select(x => x.VendingMachineId)
                .Distinct()
                .CountAsync(cancellationToken);

            servicedMachinesYesterday = await db.VendingMachineEvents
                .AsNoTracking()
                .Where(x => x.EventTypeId == serviceEventTypeId && x.OccurredAt >= yesterday && x.OccurredAt < today)
                .Select(x => x.VendingMachineId)
                .Distinct()
                .CountAsync(cancellationToken);
        }

        var salesByDate = await db.Sales
            .AsNoTracking()
            .Where(x => x.SoldAt >= start10Days && x.SoldAt < tomorrow)
            .GroupBy(x => x.SoldAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalAmount = g.Sum(x => x.TotalAmount),
                TotalQuantity = g.Sum(x => x.Quantity)
            })
            .ToListAsync(cancellationToken);

        var salesMap = salesByDate.ToDictionary(x => x.Date, x => (x.TotalAmount, x.TotalQuantity));

        var salesLast10Days = new List<DashboardSalesPoint>();
        for (var d = start10Days; d <= today; d = d.AddDays(1))
        {
            var (amount, quantity) = salesMap.TryGetValue(d, out var v) ? v : (0m, 0);
            salesLast10Days.Add(new DashboardSalesPoint(d, amount, quantity));
        }

        var response = new DashboardOverviewResponse(
            GeneratedAt: now,
            Efficiency: new DashboardEfficiency(totalMachines, workingMachines, workingPercent),
            NetworkStatuses: networkStatuses,
            Summary: new DashboardSummary(
                MoneyInMachines: totalIncome,
                ChangeInMachines: 0m,
                IncomeToday: incomeToday,
                IncomeYesterday: incomeYesterday,
                CashInToday: 0m,
                CashInYesterday: 0m,
                ServicedMachinesToday: servicedMachinesToday,
                ServicedMachinesYesterday: servicedMachinesYesterday),
            SalesLast10Days: salesLast10Days,
            News: DemoNews);

        return Ok(response);
    }
}

