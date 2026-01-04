using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VendingService.API.Contracts.Common;
using VendingService.API.Contracts.Modems;
using VendingService.API.Data;
using VendingService.API.Models;

namespace VendingService.API.Controllers;

[ApiController]
[Authorize]
[Route("modems")]
public sealed class ModemsController(VendingServiceDbContext db) : ControllerBase
{
    private static int? NormalizeOptionalId(int? value)
        => value is > 0 ? value : null;

    [HttpGet]
    public async Task<ActionResult<PagedResult<ModemListItem>>> GetList(
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

        var query = db.Modems.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(x =>
                x.ModemNumber.Contains(s) ||
                (x.Imei != null && x.Imei.Contains(s)) ||
                (x.SimPhoneNumber != null && x.SimPhoneNumber.Contains(s)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.ModemNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ModemListItem(
                x.ModemId,
                x.ModemNumber,
                x.Imei,
                x.SimPhoneNumber,
                x.ProviderId,
                x.Provider != null ? x.Provider.Name : null,
                x.ConnectionTypeId,
                x.ConnectionType != null ? x.ConnectionType.Name : null))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<ModemListItem>(items, totalCount, page, pageSize));
    }

    [HttpGet("{modemId:int}")]
    public async Task<ActionResult<ModemDetails>> GetById(int modemId, CancellationToken cancellationToken)
    {
        var modem = await db.Modems
            .AsNoTracking()
            .Where(x => x.ModemId == modemId)
            .Select(x => new ModemDetails(
                x.ModemId,
                x.ModemNumber,
                x.Imei,
                x.SimPhoneNumber,
                x.ProviderId,
                x.Provider != null ? x.Provider.Name : null,
                x.ConnectionTypeId,
                x.ConnectionType != null ? x.ConnectionType.Name : null,
                x.Notes,
                x.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return modem is null ? NotFound() : Ok(modem);
    }

    [HttpPost]
    public async Task<ActionResult<ModemDetails>> Create([FromBody] CreateModemRequest request, CancellationToken cancellationToken)
    {
        var modem = new Modem
        {
            ModemNumber = request.ModemNumber.Trim(),
            Imei = string.IsNullOrWhiteSpace(request.Imei) ? null : request.Imei.Trim(),
            SimPhoneNumber = string.IsNullOrWhiteSpace(request.SimPhoneNumber) ? null : request.SimPhoneNumber.Trim(),
            ProviderId = NormalizeOptionalId(request.ProviderId),
            ConnectionTypeId = NormalizeOptionalId(request.ConnectionTypeId),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = DateTime.Now
        };

        db.Modems.Add(modem);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Conflict(new { Message = "Modem number or IMEI already exists." });
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            return BadRequest(new { Message = "Invalid provider or connection type id." });
        }

        var dto = await db.Modems
            .AsNoTracking()
            .Where(x => x.ModemId == modem.ModemId)
            .Select(x => new ModemDetails(
                x.ModemId,
                x.ModemNumber,
                x.Imei,
                x.SimPhoneNumber,
                x.ProviderId,
                x.Provider != null ? x.Provider.Name : null,
                x.ConnectionTypeId,
                x.ConnectionType != null ? x.ConnectionType.Name : null,
                x.Notes,
                x.CreatedAt))
            .SingleAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { modemId = modem.ModemId }, dto);
    }

    [HttpPut("{modemId:int}")]
    public async Task<ActionResult<ModemDetails>> Update(int modemId, [FromBody] UpdateModemRequest request, CancellationToken cancellationToken)
    {
        var modem = await db.Modems.SingleOrDefaultAsync(x => x.ModemId == modemId, cancellationToken);
        if (modem is null)
        {
            return NotFound();
        }

        modem.ModemNumber = request.ModemNumber.Trim();
        modem.Imei = string.IsNullOrWhiteSpace(request.Imei) ? null : request.Imei.Trim();
        modem.SimPhoneNumber = string.IsNullOrWhiteSpace(request.SimPhoneNumber) ? null : request.SimPhoneNumber.Trim();
        modem.ProviderId = NormalizeOptionalId(request.ProviderId);
        modem.ConnectionTypeId = NormalizeOptionalId(request.ConnectionTypeId);
        modem.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Conflict(new { Message = "Modem number or IMEI already exists." });
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            return BadRequest(new { Message = "Invalid provider or connection type id." });
        }

        var dto = await db.Modems
            .AsNoTracking()
            .Where(x => x.ModemId == modem.ModemId)
            .Select(x => new ModemDetails(
                x.ModemId,
                x.ModemNumber,
                x.Imei,
                x.SimPhoneNumber,
                x.ProviderId,
                x.Provider != null ? x.Provider.Name : null,
                x.ConnectionTypeId,
                x.ConnectionType != null ? x.ConnectionType.Name : null,
                x.Notes,
                x.CreatedAt))
            .SingleAsync(cancellationToken);

        return Ok(dto);
    }

    [HttpDelete("{modemId:int}")]
    public async Task<IActionResult> Delete(int modemId, CancellationToken cancellationToken)
    {
        var modem = await db.Modems.SingleOrDefaultAsync(x => x.ModemId == modemId, cancellationToken);
        if (modem is null)
        {
            return NotFound();
        }

        db.Modems.Remove(modem);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { Message = "Cannot delete modem because it is referenced by other entities." });
        }

        return NoContent();
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => ex.InnerException is SqlException { Number: 2601 or 2627 };

    private static bool IsForeignKeyViolation(DbUpdateException ex)
        => ex.InnerException is SqlException { Number: 547 };
}
