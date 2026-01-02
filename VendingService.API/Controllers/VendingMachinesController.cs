using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VendingService.API.Contracts.Common;
using VendingService.API.Contracts.VendingMachines;
using VendingService.API.Data;
using VendingService.API.Models;

namespace VendingService.API.Controllers;

[ApiController]
[Authorize]
[Route("vending-machines")]
public sealed class VendingMachinesController(VendingServiceDbContext db) : ControllerBase
{
    private static int? NormalizeOptionalId(int? value)
        => value is > 0 ? value : null;

    [HttpGet]
    public async Task<ActionResult<PagedResult<VendingMachineListItem>>> GetList(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = pageSize switch
        {
            <= 0 => 50,
            > 200 => 200,
            _ => pageSize
        };

        var query = db.VendingMachines.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(x => x.Name.Contains(s));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new VendingMachineListItem(
                x.VendingMachineId,
                x.Name,
                x.VendingMachineModel != null ? x.VendingMachineModel.Name : string.Empty,
                x.VendingMachineModel != null && x.VendingMachineModel.VendingMachineManufacturer != null
                    ? x.VendingMachineModel.VendingMachineManufacturer.Name
                    : string.Empty,
                x.CompanyId,
                x.Company != null ? x.Company.Name : null,
                x.ModemId ?? -1,
                x.Modem != null ? x.Modem.ModemNumber : null,
                x.Address,
                x.Place,
                x.CommissioningDate))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<VendingMachineListItem>(items, totalCount, page, pageSize));
    }

    [HttpGet("{vendingMachineId:int}")]
    public async Task<ActionResult<VendingMachineDetails>> GetById(int vendingMachineId, CancellationToken cancellationToken)
    {
        var vm = await db.VendingMachines
            .AsNoTracking()
            .Include(x => x.PaymentSystems)
            .SingleOrDefaultAsync(x => x.VendingMachineId == vendingMachineId, cancellationToken);

        if (vm is null)
        {
            return NotFound();
        }

        return Ok(ToDetails(vm));
    }

    [HttpPost]
    public async Task<ActionResult<VendingMachineDetails>> Create([FromBody] CreateVendingMachineRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.Address)
            || string.IsNullOrWhiteSpace(request.Place)
            || string.IsNullOrWhiteSpace(request.InventoryNumber)
            || string.IsNullOrWhiteSpace(request.SerialNumber))
        {
            return BadRequest(new { Message = "Name/Address/Place/InventoryNumber/SerialNumber must be provided." });
        }

        if (request.VendingMachineModelId <= 0
            || request.WorkModeId <= 0
            || request.TimeZoneId <= 0
            || request.VendingMachineStatusId <= 0
            || request.ServicePriorityId <= 0
            || request.ProductMatrixId <= 0
            || request.CountryId <= 0)
        {
            return BadRequest(new { Message = "One or more required lookup ids are invalid." });
        }

        if (request.PaymentSystemIds is not { Count: > 0 })
        {
            return BadRequest(new { Message = "PaymentSystemIds must contain at least 1 item." });
        }

        var paymentIds = request.PaymentSystemIds.Distinct().ToList();
        if (paymentIds.Any(x => x <= 0))
        {
            return BadRequest(new { Message = "PaymentSystemIds contains invalid ids (must be > 0)." });
        }

        var knownPaymentCount = await db.PaymentSystems.CountAsync(x => paymentIds.Contains(x.PaymentSystemId), cancellationToken);
        if (knownPaymentCount != paymentIds.Count)
        {
            return BadRequest(new { Message = "PaymentSystemIds contains unknown ids." });
        }

        var vm = new VendingMachine
        {
            Name = request.Name.Trim(),
            VendingMachineModelId = request.VendingMachineModelId,
            WorkModeId = request.WorkModeId,
            TimeZoneId = request.TimeZoneId,
            VendingMachineStatusId = request.VendingMachineStatusId,
            ServicePriorityId = request.ServicePriorityId,
            ProductMatrixId = request.ProductMatrixId,
            CompanyId = NormalizeOptionalId(request.CompanyId),
            ModemId = NormalizeOptionalId(request.ModemId),
            Address = request.Address.Trim(),
            Place = request.Place.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            InventoryNumber = request.InventoryNumber.Trim(),
            SerialNumber = request.SerialNumber.Trim(),
            ManufactureDate = request.ManufactureDate,
            CommissioningDate = request.CommissioningDate,
            LastVerificationDate = request.LastVerificationDate,
            VerificationIntervalMonths = request.VerificationIntervalMonths,
            ResourceHours = request.ResourceHours,
            NextServiceDate = request.NextServiceDate,
            ServiceDurationHours = request.ServiceDurationHours,
            InventoryDate = request.InventoryDate,
            CountryId = request.CountryId,
            LastVerificationUserAccountId = NormalizeOptionalId(request.LastVerificationUserAccountId),
            WorkingTimeFrom = request.WorkingTimeFrom,
            WorkingTimeTo = request.WorkingTimeTo,
            CriticalValuesTemplateId = NormalizeOptionalId(request.CriticalValuesTemplateId),
            NotificationTemplateId = NormalizeOptionalId(request.NotificationTemplateId),
            ManagerUserAccountId = NormalizeOptionalId(request.ManagerUserAccountId),
            EngineerUserAccountId = NormalizeOptionalId(request.EngineerUserAccountId),
            TechnicianOperatorUserAccountId = NormalizeOptionalId(request.TechnicianOperatorUserAccountId),
            KitOnlineCashRegisterId = string.IsNullOrWhiteSpace(request.KitOnlineCashRegisterId) ? null : request.KitOnlineCashRegisterId.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = DateTime.Now,
            PaymentSystems = paymentIds.Select(id => new VendingMachinePaymentSystem { PaymentSystemId = id }).ToList()
        };

        db.VendingMachines.Add(vm);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Conflict(new { Message = "InventoryNumber or SerialNumber already exists." });
        }
        catch (DbUpdateException ex) when (IsCheckOrForeignKeyViolation(ex))
        {
            return BadRequest(new { Message = "Invalid data. Check constraints or references failed.", Details = ex.InnerException?.Message });
        }

        var savedVm = await db.VendingMachines
            .AsNoTracking()
            .Include(x => x.PaymentSystems)
            .SingleAsync(x => x.VendingMachineId == vm.VendingMachineId, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { vendingMachineId = savedVm.VendingMachineId }, ToDetails(savedVm));
    }

    [HttpPut("{vendingMachineId:int}")]
    public async Task<ActionResult<VendingMachineDetails>> Update(int vendingMachineId, [FromBody] UpdateVendingMachineRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.Address)
            || string.IsNullOrWhiteSpace(request.Place)
            || string.IsNullOrWhiteSpace(request.InventoryNumber)
            || string.IsNullOrWhiteSpace(request.SerialNumber))
        {
            return BadRequest(new { Message = "Name/Address/Place/InventoryNumber/SerialNumber must be provided." });
        }

        if (request.VendingMachineModelId <= 0
            || request.WorkModeId <= 0
            || request.TimeZoneId <= 0
            || request.VendingMachineStatusId <= 0
            || request.ServicePriorityId <= 0
            || request.ProductMatrixId <= 0
            || request.CountryId <= 0)
        {
            return BadRequest(new { Message = "One or more required lookup ids are invalid." });
        }

        if (request.PaymentSystemIds is not { Count: > 0 })
        {
            return BadRequest(new { Message = "PaymentSystemIds must contain at least 1 item." });
        }

        var paymentIds = request.PaymentSystemIds.Distinct().ToList();
        if (paymentIds.Any(x => x <= 0))
        {
            return BadRequest(new { Message = "PaymentSystemIds contains invalid ids (must be > 0)." });
        }

        var knownPaymentCount = await db.PaymentSystems.CountAsync(x => paymentIds.Contains(x.PaymentSystemId), cancellationToken);
        if (knownPaymentCount != paymentIds.Count)
        {
            return BadRequest(new { Message = "PaymentSystemIds contains unknown ids." });
        }

        var vm = await db.VendingMachines
            .Include(x => x.PaymentSystems)
            .SingleOrDefaultAsync(x => x.VendingMachineId == vendingMachineId, cancellationToken);

        if (vm is null)
        {
            return NotFound();
        }

        vm.Name = request.Name.Trim();
        vm.VendingMachineModelId = request.VendingMachineModelId;
        vm.WorkModeId = request.WorkModeId;
        vm.TimeZoneId = request.TimeZoneId;
        vm.VendingMachineStatusId = request.VendingMachineStatusId;
        vm.ServicePriorityId = request.ServicePriorityId;
        vm.ProductMatrixId = request.ProductMatrixId;
        vm.CompanyId = NormalizeOptionalId(request.CompanyId);
        vm.ModemId = NormalizeOptionalId(request.ModemId);
        vm.Address = request.Address.Trim();
        vm.Place = request.Place.Trim();
        vm.Latitude = request.Latitude;
        vm.Longitude = request.Longitude;
        vm.InventoryNumber = request.InventoryNumber.Trim();
        vm.SerialNumber = request.SerialNumber.Trim();
        vm.ManufactureDate = request.ManufactureDate;
        vm.CommissioningDate = request.CommissioningDate;
        vm.LastVerificationDate = request.LastVerificationDate;
        vm.VerificationIntervalMonths = request.VerificationIntervalMonths;
        vm.ResourceHours = request.ResourceHours;
        vm.NextServiceDate = request.NextServiceDate;
        vm.ServiceDurationHours = request.ServiceDurationHours;
        vm.InventoryDate = request.InventoryDate;
        vm.CountryId = request.CountryId;
        vm.LastVerificationUserAccountId = NormalizeOptionalId(request.LastVerificationUserAccountId);
        vm.WorkingTimeFrom = request.WorkingTimeFrom;
        vm.WorkingTimeTo = request.WorkingTimeTo;
        vm.CriticalValuesTemplateId = NormalizeOptionalId(request.CriticalValuesTemplateId);
        vm.NotificationTemplateId = NormalizeOptionalId(request.NotificationTemplateId);
        vm.ManagerUserAccountId = NormalizeOptionalId(request.ManagerUserAccountId);
        vm.EngineerUserAccountId = NormalizeOptionalId(request.EngineerUserAccountId);
        vm.TechnicianOperatorUserAccountId = NormalizeOptionalId(request.TechnicianOperatorUserAccountId);
        vm.KitOnlineCashRegisterId = string.IsNullOrWhiteSpace(request.KitOnlineCashRegisterId) ? null : request.KitOnlineCashRegisterId.Trim();
        vm.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        vm.PaymentSystems.RemoveAll(x => !paymentIds.Contains(x.PaymentSystemId));
        foreach (var paymentId in paymentIds)
        {
            if (!vm.PaymentSystems.Any(x => x.PaymentSystemId == paymentId))
            {
                vm.PaymentSystems.Add(new VendingMachinePaymentSystem { PaymentSystemId = paymentId, VendingMachineId = vm.VendingMachineId });
            }
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Conflict(new { Message = "InventoryNumber or SerialNumber already exists." });
        }
        catch (DbUpdateException ex) when (IsCheckOrForeignKeyViolation(ex))
        {
            return BadRequest(new { Message = "Invalid data. Check constraints or references failed.", Details = ex.InnerException?.Message });
        }

        var updatedVm = await db.VendingMachines
            .AsNoTracking()
            .Include(x => x.PaymentSystems)
            .SingleAsync(x => x.VendingMachineId == vm.VendingMachineId, cancellationToken);

        return Ok(ToDetails(updatedVm));
    }

    [HttpPost("{vendingMachineId:int}/detach-modem")]
    public async Task<IActionResult> DetachModem(int vendingMachineId, CancellationToken cancellationToken)
    {
        var vm = await db.VendingMachines.SingleOrDefaultAsync(x => x.VendingMachineId == vendingMachineId, cancellationToken);
        if (vm is null)
        {
            return NotFound();
        }

        vm.ModemId = null;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { Message = "Modem detached.", ModemId = -1 });
    }

    [HttpDelete("{vendingMachineId:int}")]
    public async Task<IActionResult> Delete(int vendingMachineId, CancellationToken cancellationToken)
    {
        var vm = await db.VendingMachines.SingleOrDefaultAsync(x => x.VendingMachineId == vendingMachineId, cancellationToken);
        if (vm is null)
        {
            return NotFound();
        }

        db.VendingMachines.Remove(vm);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { Message = "Cannot delete vending machine because it is referenced by other entities." });
        }

        return NoContent();
    }

    private static VendingMachineDetails ToDetails(VendingMachine vm)
    {
        var paymentIds = vm.PaymentSystems.Select(x => x.PaymentSystemId).ToList();

        return new VendingMachineDetails(
            vm.VendingMachineId,
            vm.Name,
            vm.VendingMachineModelId,
            vm.WorkModeId,
            vm.TimeZoneId,
            vm.VendingMachineStatusId,
            vm.ServicePriorityId,
            vm.ProductMatrixId,
            vm.CompanyId,
            vm.ModemId ?? -1,
            vm.Address,
            vm.Place,
            vm.Latitude,
            vm.Longitude,
            vm.InventoryNumber,
            vm.SerialNumber,
            vm.ManufactureDate,
            vm.CommissioningDate,
            vm.LastVerificationDate,
            vm.VerificationIntervalMonths,
            vm.NextVerificationDate,
            vm.ResourceHours,
            vm.NextServiceDate,
            vm.ServiceDurationHours,
            vm.InventoryDate,
            vm.CountryId,
            vm.LastVerificationUserAccountId,
            vm.WorkingTimeFrom,
            vm.WorkingTimeTo,
            vm.CriticalValuesTemplateId,
            vm.NotificationTemplateId,
            vm.ManagerUserAccountId,
            vm.EngineerUserAccountId,
            vm.TechnicianOperatorUserAccountId,
            vm.KitOnlineCashRegisterId,
            vm.Notes,
            paymentIds,
            vm.CreatedAt);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => ex.InnerException is SqlException { Number: 2601 or 2627 };

    private static bool IsCheckOrForeignKeyViolation(DbUpdateException ex)
        => ex.InnerException is SqlException { Number: 547 };
}
