using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingService.API.Contracts.Lookups;
using VendingService.API.Data;

namespace VendingService.API.Controllers;

[ApiController]
[Authorize]
[Route("lookups")]
public sealed class LookupsController(VendingServiceDbContext db) : ControllerBase
{
    [HttpGet("vending-machine-create")]
    public async Task<ActionResult<VendingMachineCreateLookups>> GetVendingMachineCreateLookups(CancellationToken cancellationToken)
    {
        var countries = await db.Countries
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new LookupItem(x.CountryId, x.Name))
            .ToListAsync(cancellationToken);

        var timeZones = await db.TimeZones
            .AsNoTracking()
            .OrderBy(x => x.UtcOffsetMinutes)
            .ThenBy(x => x.Name)
            .Select(x => new LookupItem(x.TimeZoneId, x.Name))
            .ToListAsync(cancellationToken);

        var workModes = await db.WorkModes
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new LookupItem(x.WorkModeId, x.Name))
            .ToListAsync(cancellationToken);

        var servicePriorities = await db.ServicePriorities
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new LookupItem(x.ServicePriorityId, x.Name))
            .ToListAsync(cancellationToken);

        var vendingMachineStatuses = await db.VendingMachineStatuses
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new LookupItem(x.VendingMachineStatusId, x.Name))
            .ToListAsync(cancellationToken);

        var productMatrices = await db.ProductMatrices
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new LookupItem(x.ProductMatrixId, x.Name))
            .ToListAsync(cancellationToken);

        var criticalValuesTemplates = await db.CriticalValuesTemplates
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new LookupItem(x.CriticalValuesTemplateId, x.Name))
            .ToListAsync(cancellationToken);

        var notificationTemplates = await db.NotificationTemplates
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new LookupItem(x.NotificationTemplateId, x.Name))
            .ToListAsync(cancellationToken);

        var paymentSystems = await db.PaymentSystems
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new LookupItem(x.PaymentSystemId, x.Name))
            .ToListAsync(cancellationToken);

        var companies = await db.Companies
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new LookupItem(x.CompanyId, x.Name))
            .ToListAsync(cancellationToken);

        var modems = await db.Modems
            .AsNoTracking()
            .OrderBy(x => x.ModemNumber)
            .Select(x => new ModemItem(
                x.ModemId,
                x.ModemNumber,
                x.ProviderId,
                x.Provider != null ? x.Provider.Name : null,
                x.ConnectionTypeId,
                x.ConnectionType != null ? x.ConnectionType.Name : null))
            .ToListAsync(cancellationToken);

        var manufacturers = await db.VendingMachineManufacturers
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new LookupItem(x.VendingMachineManufacturerId, x.Name))
            .ToListAsync(cancellationToken);

        var models = await db.VendingMachineModels
            .AsNoTracking()
            .OrderBy(x => x.VendingMachineManufacturer != null ? x.VendingMachineManufacturer.Name : string.Empty)
            .ThenBy(x => x.Name)
            .Select(x => new VendingMachineModelItem(
                x.VendingMachineModelId,
                x.Name,
                x.VendingMachineManufacturerId,
                x.VendingMachineManufacturer != null ? x.VendingMachineManufacturer.Name : string.Empty))
            .ToListAsync(cancellationToken);

        return Ok(new VendingMachineCreateLookups(
            countries,
            timeZones,
            workModes,
            servicePriorities,
            vendingMachineStatuses,
            productMatrices,
            criticalValuesTemplates,
            notificationTemplates,
            paymentSystems,
            companies,
            modems,
            manufacturers,
            models));
    }
}

