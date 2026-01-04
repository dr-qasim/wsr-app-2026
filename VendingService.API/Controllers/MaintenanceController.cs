using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VendingService.API.Contracts.Common;
using VendingService.API.Contracts.Maintenance;
using VendingService.API.Data;
using VendingService.API.Models;

namespace VendingService.API.Controllers;

[ApiController]
[Authorize]
[Route("maintenance")]
public sealed class MaintenanceController(VendingServiceDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<MaintenanceListItem>>> GetList(
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

        var query = db.Maintenances.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.MaintenanceDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new MaintenanceListItem(
                x.MaintenanceId,
                x.VendingMachineId,
                x.VendingMachine != null ? x.VendingMachine.Name : string.Empty,
                x.MaintenanceDate,
                x.WorkDescription,
                x.Problems,
                x.ExecutorUserAccountId,
                x.ExecutorUserAccount != null
                    ? BuildFullName(x.ExecutorUserAccount.LastName, x.ExecutorUserAccount.FirstName, x.ExecutorUserAccount.Patronymic)
                    : null))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<MaintenanceListItem>(items, totalCount, page, pageSize));
    }

    [HttpGet("{maintenanceId:int}")]
    public async Task<ActionResult<MaintenanceDetails>> GetById(int maintenanceId, CancellationToken cancellationToken)
    {
        var item = await db.Maintenances
            .AsNoTracking()
            .Where(x => x.MaintenanceId == maintenanceId)
            .Select(x => new MaintenanceDetails(
                x.MaintenanceId,
                x.VendingMachineId,
                x.VendingMachine != null ? x.VendingMachine.Name : string.Empty,
                x.MaintenanceDate,
                x.WorkDescription,
                x.Problems,
                x.ExecutorUserAccountId,
                x.ExecutorUserAccount != null
                    ? BuildFullName(x.ExecutorUserAccount.LastName, x.ExecutorUserAccount.FirstName, x.ExecutorUserAccount.Patronymic)
                    : null,
                x.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<MaintenanceDetails>> Create([FromBody] CreateMaintenanceRequest request, CancellationToken cancellationToken)
    {
        var entity = new Maintenance
        {
            VendingMachineId = request.VendingMachineId,
            MaintenanceDate = request.MaintenanceDate,
            WorkDescription = string.IsNullOrWhiteSpace(request.WorkDescription) ? null : request.WorkDescription.Trim(),
            Problems = string.IsNullOrWhiteSpace(request.Problems) ? null : request.Problems.Trim(),
            ExecutorUserAccountId = request.ExecutorUserAccountId,
            CreatedAt = DateTime.Now
        };

        db.Maintenances.Add(entity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            return BadRequest(new { Message = "Invalid vending machine or executor id." });
        }

        var dto = await db.Maintenances
            .AsNoTracking()
            .Where(x => x.MaintenanceId == entity.MaintenanceId)
            .Select(x => new MaintenanceDetails(
                x.MaintenanceId,
                x.VendingMachineId,
                x.VendingMachine != null ? x.VendingMachine.Name : string.Empty,
                x.MaintenanceDate,
                x.WorkDescription,
                x.Problems,
                x.ExecutorUserAccountId,
                x.ExecutorUserAccount != null
                    ? BuildFullName(x.ExecutorUserAccount.LastName, x.ExecutorUserAccount.FirstName, x.ExecutorUserAccount.Patronymic)
                    : null,
                x.CreatedAt))
            .SingleAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { maintenanceId = entity.MaintenanceId }, dto);
    }

    [HttpPut("{maintenanceId:int}")]
    public async Task<ActionResult<MaintenanceDetails>> Update(int maintenanceId, [FromBody] UpdateMaintenanceRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.Maintenances.SingleOrDefaultAsync(x => x.MaintenanceId == maintenanceId, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.VendingMachineId = request.VendingMachineId;
        entity.MaintenanceDate = request.MaintenanceDate;
        entity.WorkDescription = string.IsNullOrWhiteSpace(request.WorkDescription) ? null : request.WorkDescription.Trim();
        entity.Problems = string.IsNullOrWhiteSpace(request.Problems) ? null : request.Problems.Trim();
        entity.ExecutorUserAccountId = request.ExecutorUserAccountId;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            return BadRequest(new { Message = "Invalid vending machine or executor id." });
        }

        var dto = await db.Maintenances
            .AsNoTracking()
            .Where(x => x.MaintenanceId == entity.MaintenanceId)
            .Select(x => new MaintenanceDetails(
                x.MaintenanceId,
                x.VendingMachineId,
                x.VendingMachine != null ? x.VendingMachine.Name : string.Empty,
                x.MaintenanceDate,
                x.WorkDescription,
                x.Problems,
                x.ExecutorUserAccountId,
                x.ExecutorUserAccount != null
                    ? BuildFullName(x.ExecutorUserAccount.LastName, x.ExecutorUserAccount.FirstName, x.ExecutorUserAccount.Patronymic)
                    : null,
                x.CreatedAt))
            .SingleAsync(cancellationToken);

        return Ok(dto);
    }

    [HttpDelete("{maintenanceId:int}")]
    public async Task<IActionResult> Delete(int maintenanceId, CancellationToken cancellationToken)
    {
        var entity = await db.Maintenances.SingleOrDefaultAsync(x => x.MaintenanceId == maintenanceId, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        db.Maintenances.Remove(entity);

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string BuildFullName(string lastName, string firstName, string? patronymic)
        => string.Join(" ", new[] { lastName, firstName, patronymic }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static bool IsForeignKeyViolation(DbUpdateException ex)
        => ex.InnerException is SqlException { Number: 547 };
}
