using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingService.API.Contracts.Monitor;
using VendingService.API.Data;
using VendingService.API.Models;

namespace VendingService.API.Controllers;

[ApiController]
[Authorize]
[Route("monitor")]
public sealed class MonitorController(VendingServiceDbContext db) : ControllerBase
{
    private static readonly Dictionary<int, DateTimeOffset> LastGeneratedEventAtUtc = new();
    private static readonly object LastGeneratedEventAtUtcLock = new();

    private sealed record MonitorMachineSeed(
        int VendingMachineId,
        string Name,
        string ManufacturerName,
        string ModelName,
        string? CompanyName,
        string? ProviderName,
        string Address,
        string Place,
        int VendingMachineStatusId,
        string VendingMachineStatusName,
        int? ConnectionTypeId,
        string? ConnectionTypeName);

    private static readonly MonitorAdditionalStatusItem[] AdditionalStatuses =
    [
        new("NoConnection", "Нет связи"),
        new("NoChange", "Нет сдачи"),
        new("LowStock", "Низкий запас"),
        new("ServiceNeeded", "Нужно обслуживание"),
        new("ProductJam", "Замятие товара"),
        new("Overheat", "Перегрев")
    ];

    [HttpGet("lookups")]
    public async Task<ActionResult<MonitorLookups>> GetLookups(CancellationToken cancellationToken)
    {
        var connectionTypes = await db.ConnectionTypes
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new MonitorLookupItem(x.ConnectionTypeId, x.Name))
            .ToListAsync(cancellationToken);

        var statuses = await db.VendingMachineStatuses
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new MonitorLookupItem(x.VendingMachineStatusId, x.Name))
            .ToListAsync(cancellationToken);

        return Ok(new MonitorLookups(connectionTypes, AdditionalStatuses, statuses));
    }

    [HttpGet("snapshot")]
    public async Task<ActionResult<MonitorSnapshotResponse>> GetSnapshot(
        [FromQuery] int afterEventId = 0,
        CancellationToken cancellationToken = default)
    {
        await EnsureDemoDataAsync(cancellationToken);

        var machines = await db.VendingMachines
            .AsNoTracking()
            .Select(x => new MonitorMachineSeed(
                x.VendingMachineId,
                x.Name,
                x.VendingMachineModel != null && x.VendingMachineModel.VendingMachineManufacturer != null
                    ? x.VendingMachineModel.VendingMachineManufacturer.Name
                    : string.Empty,
                x.VendingMachineModel != null ? x.VendingMachineModel.Name : string.Empty,
                x.Company != null ? x.Company.Name : null,
                x.Modem != null && x.Modem.Provider != null ? x.Modem.Provider.Name : null,
                x.Address,
                x.Place,
                x.VendingMachineStatusId,
                x.VendingMachineStatus != null ? x.VendingMachineStatus.Name : string.Empty,
                x.Modem != null ? x.Modem.ConnectionTypeId : null,
                x.Modem != null && x.Modem.ConnectionType != null ? x.Modem.ConnectionType.Name : null))
            .ToListAsync(cancellationToken);

        var machineIds = machines.Select(x => x.VendingMachineId).ToList();

        await EnsureEquipmentForMachinesAsync(machineIds, cancellationToken);
        await EnsureInitialSalesForMachinesAsync(machineIds, cancellationToken);

        var moneyOnAccount = await db.Sales
            .AsNoTracking()
            .Where(x => machineIds.Contains(x.VendingMachineId))
            .GroupBy(x => x.VendingMachineId)
            .Select(g => new { VendingMachineId = g.Key, Total = g.Sum(x => x.TotalAmount) })
            .ToDictionaryAsync(x => x.VendingMachineId, x => x.Total, cancellationToken);

        var equipmentCounts = await db.VendingMachineEquipments
            .AsNoTracking()
            .Where(x => machineIds.Contains(x.VendingMachineId))
            .GroupBy(x => x.VendingMachineId)
            .Select(g => new
            {
                VendingMachineId = g.Key,
                Total = g.Count(),
                Ok = g.Count(x => x.IsOperational)
            })
            .ToDictionaryAsync(x => x.VendingMachineId, x => (x.Ok, x.Total), cancellationToken);

        var eventTypes = await db.EventTypes
            .AsNoTracking()
            .Include(x => x.EventSeverity)
            .ToListAsync(cancellationToken);

        var eventTypeByName = eventTypes.ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);

        var newGeneratedEvents = GenerateAndSaveEvents(machineIds, eventTypeByName);
        if (newGeneratedEvents.Count > 0)
        {
            db.VendingMachineEvents.AddRange(newGeneratedEvents);
            await db.SaveChangesAsync(cancellationToken);
        }

        var lastEventId = await db.VendingMachineEvents
            .AsNoTracking()
            .MaxAsync(x => (int?)x.VendingMachineEventId, cancellationToken) ?? 0;

        var newEvents = await db.VendingMachineEvents
            .AsNoTracking()
            .Where(x => x.VendingMachineEventId > afterEventId)
            .OrderBy(x => x.VendingMachineEventId)
            .Take(100)
            .Select(x => new MonitorEventItem(
                x.VendingMachineEventId,
                x.VendingMachineId,
                x.VendingMachine!.Name,
                x.EventType!.Name,
                x.EventType!.EventSeverity!.Name,
                x.Message,
                x.OccurredAt))
            .ToListAsync(cancellationToken);

        var recentEventsRaw = await db.VendingMachineEvents
            .AsNoTracking()
            .Where(x => machineIds.Contains(x.VendingMachineId))
            .OrderByDescending(x => x.VendingMachineEventId)
            .Take(500)
            .Select(x => new
            {
                x.VendingMachineId,
                x.VendingMachineEventId,
                EventTypeName = x.EventType!.Name,
                SeverityName = x.EventType!.EventSeverity!.Name,
                x.OccurredAt
            })
            .ToListAsync(cancellationToken);

        var recentEventsByMachineId = recentEventsRaw
            .GroupBy(x => x.VendingMachineId)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderByDescending(x => x.VendingMachineEventId)
                    .Take(3)
                    .Select(x => new MonitorEventPreview(
                        x.VendingMachineEventId,
                        x.EventTypeName,
                        x.SeverityName,
                        x.OccurredAt))
                    .ToList());

        var now = DateTime.Now;
        var items = machines
            .Select(m =>
            {
                var seed = HashCode.Combine(m.VendingMachineId, now.Year, now.DayOfYear, now.Hour, now.Minute);
                var rng = new Random(seed);

                var isOnline = rng.NextDouble() >= 0.15;
                var loads = new[]
                {
                    rng.Next(0, 101),
                    rng.Next(0, 101),
                    rng.Next(0, 101),
                    rng.Next(0, 101),
                    rng.Next(0, 101)
                };

                var loadOverall = (int)Math.Round(loads.Average());
                var loadMin = loads.Min();

                var cashCoins = Math.Round((decimal)rng.NextDouble() * 8000m, 2);
                var cashBills = Math.Round((decimal)rng.NextDouble() * 8000m, 2);
                var cashChange = Math.Round((decimal)rng.NextDouble() * 1000m, 2);

                var additionalStatusCodes = BuildAdditionalStatuses(isOnline, loadMin, cashChange, rng);

                var (equipmentOk, equipmentTotal) = equipmentCounts.TryGetValue(m.VendingMachineId, out var eq)
                    ? eq
                    : (0, 0);

                var money = moneyOnAccount.TryGetValue(m.VendingMachineId, out var totalIncome)
                    ? totalIncome
                    : 0m;

                var recentEvents = recentEventsByMachineId.TryGetValue(m.VendingMachineId, out var previews)
                    ? previews
                    : [];

                return new MonitorVendingMachineItem(
                    m.VendingMachineId,
                    m.Name,
                    m.ManufacturerName,
                    m.ModelName,
                    m.CompanyName,
                    m.ProviderName,
                    m.Address,
                    m.Place,
                    money,
                    m.VendingMachineStatusId,
                    m.VendingMachineStatusName,
                    m.ConnectionTypeId,
                    m.ConnectionTypeName,
                    isOnline,
                    loadOverall,
                    loadMin,
                    cashCoins,
                    cashBills,
                    cashChange,
                    equipmentOk,
                    equipmentTotal,
                    additionalStatusCodes,
                    recentEvents);
            })
            .ToList();

        return Ok(new MonitorSnapshotResponse(now, lastEventId, items, newEvents));
    }

    private async Task EnsureDemoDataAsync(CancellationToken cancellationToken)
    {
        if (!await db.Products.AsNoTracking().AnyAsync(cancellationToken))
        {
            db.Products.AddRange(
                new Product { Name = "Кофе", Description = "Напиток", Price = 150, CreatedAt = DateTime.Now },
                new Product { Name = "Чай", Description = "Напиток", Price = 120, CreatedAt = DateTime.Now },
                new Product { Name = "Шоколад", Description = "Снек", Price = 90, CreatedAt = DateTime.Now });
        }

        if (!await db.EquipmentTypes.AsNoTracking().AnyAsync(cancellationToken))
        {
            db.EquipmentTypes.AddRange(
                new EquipmentType { Name = "Кофемодуль" },
                new EquipmentType { Name = "Монетоприемник" },
                new EquipmentType { Name = "Купюроприемник" },
                new EquipmentType { Name = "Диспенсер стаканов" },
                new EquipmentType { Name = "Термодатчик" });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureEquipmentForMachinesAsync(List<int> vendingMachineIds, CancellationToken cancellationToken)
    {
        if (vendingMachineIds.Count == 0)
        {
            return;
        }

        var equipmentTypeIds = await db.EquipmentTypes
            .AsNoTracking()
            .Select(x => x.EquipmentTypeId)
            .ToListAsync(cancellationToken);

        if (equipmentTypeIds.Count == 0)
        {
            return;
        }

        var existingPairs = await db.VendingMachineEquipments
            .AsNoTracking()
            .Where(x => vendingMachineIds.Contains(x.VendingMachineId))
            .Select(x => new { x.VendingMachineId, x.EquipmentTypeId })
            .ToListAsync(cancellationToken);

        var existingSet = new HashSet<(int VendingMachineId, int EquipmentTypeId)>(
            existingPairs.Select(x => (x.VendingMachineId, x.EquipmentTypeId)));

        var now = DateTime.Now;
        var toAdd = new List<VendingMachineEquipment>();

        foreach (var vendingMachineId in vendingMachineIds)
        {
            foreach (var equipmentTypeId in equipmentTypeIds)
            {
                if (existingSet.Contains((vendingMachineId, equipmentTypeId)))
                {
                    continue;
                }

                toAdd.Add(new VendingMachineEquipment
                {
                    VendingMachineId = vendingMachineId,
                    EquipmentTypeId = equipmentTypeId,
                    IsOperational = true,
                    UpdatedAt = now
                });
            }
        }

        if (toAdd.Count > 0)
        {
            db.VendingMachineEquipments.AddRange(toAdd);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureInitialSalesForMachinesAsync(List<int> vendingMachineIds, CancellationToken cancellationToken)
    {
        if (vendingMachineIds.Count == 0)
        {
            return;
        }

        var productIds = await db.Products
            .AsNoTracking()
            .Select(x => x.ProductId)
            .ToListAsync(cancellationToken);

        if (productIds.Count == 0)
        {
            return;
        }

        var paymentMethodId = await db.SalePaymentMethods
            .AsNoTracking()
            .OrderBy(x => x.SalePaymentMethodId)
            .Select(x => x.SalePaymentMethodId)
            .FirstOrDefaultAsync(cancellationToken);

        if (paymentMethodId == 0)
        {
            return;
        }

        var machinesWithSales = await db.Sales
            .AsNoTracking()
            .Where(x => vendingMachineIds.Contains(x.VendingMachineId))
            .Select(x => x.VendingMachineId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var machinesWithoutSales = vendingMachineIds.Except(machinesWithSales).ToList();
        if (machinesWithoutSales.Count == 0)
        {
            return;
        }

        var now = DateTime.Now;
        foreach (var vendingMachineId in machinesWithoutSales)
        {
            var seed = HashCode.Combine(vendingMachineId, now.Year, now.DayOfYear);
            var rng = new Random(seed);
            var productId = productIds[rng.Next(0, productIds.Count)];
            var quantity = rng.Next(1, 4);
            var total = Math.Round((decimal)rng.NextDouble() * 1500m + 100m, 2);

            db.Sales.Add(new Sale
            {
                VendingMachineId = vendingMachineId,
                ProductId = productId,
                Quantity = quantity,
                TotalAmount = total,
                SoldAt = now.AddMinutes(-rng.Next(5, 240)),
                SalePaymentMethodId = paymentMethodId
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<string> BuildAdditionalStatuses(bool isOnline, int loadMinPercent, decimal cashChange, Random rng)
    {
        var list = new List<string>();

        if (!isOnline)
        {
            list.Add("NoConnection");
        }

        if (cashChange < 50)
        {
            list.Add("NoChange");
        }

        if (loadMinPercent < 15)
        {
            list.Add("LowStock");
        }

        if (rng.NextDouble() < 0.02)
        {
            list.Add("ProductJam");
        }

        if (rng.NextDouble() < 0.01)
        {
            list.Add("Overheat");
        }

        if (rng.NextDouble() < 0.02)
        {
            list.Add("ServiceNeeded");
        }

        return list;
    }

    private List<VendingMachineEvent> GenerateAndSaveEvents(
        IReadOnlyList<int> vendingMachineIds,
        IReadOnlyDictionary<string, EventType> eventTypeByName)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var nowLocal = DateTime.Now;

        var events = new List<VendingMachineEvent>();

        foreach (var vendingMachineId in vendingMachineIds)
        {
            var seed = HashCode.Combine(vendingMachineId, nowUtc.Year, nowUtc.DayOfYear, nowUtc.Hour, nowUtc.Minute);
            var rng = new Random(seed);

            if (!CanGenerateEvent(vendingMachineId, nowUtc))
            {
                continue;
            }

            // Small chance to generate an event.
            if (rng.NextDouble() > 0.06)
            {
                continue;
            }

            var eventOptions = new[]
            {
                ("Нет сдачи", "Ошибка: Нет сдачи. Используйте точную сумму."),
                ("Замятие товара", "Ошибка: Замятие товара. Обратитесь в поддержку."),
                ("Перегрев", "Ошибка: Перегрев. Требуется обслуживание."),
                ("Низкий запас товара", $"Внимание: Заканчивается товар A1 (осталось {rng.Next(1, 4)} шт.)"),
                ("Необходимость обслуживания", "Внимание: Требуется техническое обслуживание."),
                ("Успешная оплата", "Успешно: Оплата прошла."),
                ("Выдача товара", "Успешно: Товар выдан. Спасибо за покупку!")
            };

            var choice = eventOptions[rng.Next(0, eventOptions.Length)];
            if (!eventTypeByName.TryGetValue(choice.Item1, out var eventType))
            {
                continue;
            }

            events.Add(new VendingMachineEvent
            {
                VendingMachineId = vendingMachineId,
                EventTypeId = eventType.EventTypeId,
                OccurredAt = nowLocal,
                Message = choice.Item2
            });
        }

        return events;
    }

    private static bool CanGenerateEvent(int vendingMachineId, DateTimeOffset nowUtc)
    {
        lock (LastGeneratedEventAtUtcLock)
        {
            if (LastGeneratedEventAtUtc.TryGetValue(vendingMachineId, out var lastUtc))
            {
                if (nowUtc - lastUtc < TimeSpan.FromSeconds(20))
                {
                    return false;
                }
            }

            LastGeneratedEventAtUtc[vendingMachineId] = nowUtc;
            return true;
        }
    }
}
