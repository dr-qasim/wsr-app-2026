using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VendingService.API.Contracts.Common;
using VendingService.API.Contracts.ServiceRequests;
using VendingService.API.Data;
using VendingService.API.Models;

namespace VendingService.API.Controllers;

[ApiController]
[Authorize]
[Route("service-requests")]
public sealed class ServiceRequestsController(VendingServiceDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ServiceRequestListItem>>> GetList(
        [FromQuery] DateOnly? date,
        [FromQuery] int? assignedUserAccountId,
        [FromQuery] int? statusId,
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

        var query = db.ServiceRequests.AsNoTracking();

        if (date.HasValue)
        {
            query = query.Where(x => x.PlannedDate == date.Value);
        }

        if (assignedUserAccountId.HasValue)
        {
            query = query.Where(x => x.AssignedUserAccountId == assignedUserAccountId.Value);
        }

        if (statusId.HasValue)
        {
            query = query.Where(x => x.ServiceRequestStatusId == statusId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.PlannedDate)
            .ThenBy(x => x.SortOrder)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ServiceRequestListItem(
                x.ServiceRequestId,
                x.VendingMachineId,
                x.VendingMachine != null ? x.VendingMachine.Name : string.Empty,
                x.ServiceRequestTypeId,
                x.ServiceRequestType != null ? x.ServiceRequestType.Name : string.Empty,
                x.ServiceRequestStatusId,
                x.ServiceRequestStatus != null ? x.ServiceRequestStatus.Name : string.Empty,
                x.PlannedDate,
                x.AssignedUserAccountId,
                x.AssignedUserAccount != null
                    ? BuildFullName(x.AssignedUserAccount.LastName, x.AssignedUserAccount.FirstName, x.AssignedUserAccount.Patronymic)
                    : null,
                x.SortOrder))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<ServiceRequestListItem>(items, totalCount, page, pageSize));
    }

    [HttpGet("{serviceRequestId:int}")]
    public async Task<ActionResult<ServiceRequestDetails>> GetById(int serviceRequestId, CancellationToken cancellationToken)
    {
        var item = await db.ServiceRequests
            .AsNoTracking()
            .Where(x => x.ServiceRequestId == serviceRequestId)
            .Select(x => new ServiceRequestDetails(
                x.ServiceRequestId,
                x.VendingMachineId,
                x.VendingMachine != null ? x.VendingMachine.Name : string.Empty,
                x.ServiceRequestTypeId,
                x.ServiceRequestType != null ? x.ServiceRequestType.Name : string.Empty,
                x.ServiceRequestStatusId,
                x.ServiceRequestStatus != null ? x.ServiceRequestStatus.Name : string.Empty,
                x.PlannedDate,
                x.AssignedUserAccountId,
                x.AssignedUserAccount != null
                    ? BuildFullName(x.AssignedUserAccount.LastName, x.AssignedUserAccount.FirstName, x.AssignedUserAccount.Patronymic)
                    : null,
                x.SortOrder,
                x.Notes,
                x.DeclineReason,
                x.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<ServiceRequestDetails>> Create([FromBody] CreateServiceRequestRequest request, CancellationToken cancellationToken)
    {
        var newStatusId = await GetStatusIdAsync("Новая", cancellationToken);
        if (newStatusId == 0)
        {
            return Problem("Missing ServiceRequestStatus 'Новая'.");
        }

        var entity = new ServiceRequest
        {
            VendingMachineId = request.VendingMachineId,
            ServiceRequestTypeId = request.ServiceRequestTypeId,
            ServiceRequestStatusId = newStatusId,
            PlannedDate = request.PlannedDate,
            AssignedUserAccountId = request.AssignedUserAccountId,
            SortOrder = request.SortOrder,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            DeclineReason = null,
            CreatedAt = DateTime.Now
        };

        db.ServiceRequests.Add(entity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            return BadRequest(new { Message = "Invalid vending machine, type or assigned user id." });
        }

        db.ServiceRequestStatusHistories.Add(new ServiceRequestStatusHistory
        {
            ServiceRequestId = entity.ServiceRequestId,
            ServiceRequestStatusId = newStatusId,
            ChangedAt = DateTime.Now,
            ChangedByUserAccountId = null
        });
        await db.SaveChangesAsync(cancellationToken);

        var dto = await GetDetailsAsync(entity.ServiceRequestId, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { serviceRequestId = entity.ServiceRequestId }, dto);
    }

    [HttpPut("{serviceRequestId:int}")]
    public async Task<ActionResult<ServiceRequestDetails>> Update(
        int serviceRequestId,
        [FromBody] UpdateServiceRequestRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await db.ServiceRequests.SingleOrDefaultAsync(x => x.ServiceRequestId == serviceRequestId, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.VendingMachineId = request.VendingMachineId;
        entity.ServiceRequestTypeId = request.ServiceRequestTypeId;
        entity.PlannedDate = request.PlannedDate;
        entity.AssignedUserAccountId = request.AssignedUserAccountId;
        entity.SortOrder = request.SortOrder;
        entity.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            return BadRequest(new { Message = "Invalid vending machine, type or assigned user id." });
        }

        var dto = await GetDetailsAsync(entity.ServiceRequestId, cancellationToken);
        return Ok(dto);
    }

    [HttpPut("{serviceRequestId:int}/schedule")]
    public async Task<ActionResult<ServiceRequestDetails>> UpdateSchedule(
        int serviceRequestId,
        [FromBody] UpdateServiceRequestScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await db.ServiceRequests.SingleOrDefaultAsync(x => x.ServiceRequestId == serviceRequestId, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.PlannedDate = request.PlannedDate;
        entity.AssignedUserAccountId = request.AssignedUserAccountId;
        entity.SortOrder = request.SortOrder;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            return BadRequest(new { Message = "Invalid assigned user id." });
        }

        var dto = await GetDetailsAsync(entity.ServiceRequestId, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{serviceRequestId:int}/status")]
    public async Task<ActionResult<ServiceRequestDetails>> ChangeStatus(
        int serviceRequestId,
        [FromBody] ChangeServiceRequestStatusRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await db.ServiceRequests.SingleOrDefaultAsync(x => x.ServiceRequestId == serviceRequestId, cancellationToken);
        if (entity is null)
        {
            return NotFound();
        }

        entity.ServiceRequestStatusId = request.ServiceRequestStatusId;
        entity.DeclineReason = string.IsNullOrWhiteSpace(request.DeclineReason)
            ? null
            : request.DeclineReason.Trim();

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            return BadRequest(new { Message = "Invalid status id." });
        }

        db.ServiceRequestStatusHistories.Add(new ServiceRequestStatusHistory
        {
            ServiceRequestId = entity.ServiceRequestId,
            ServiceRequestStatusId = request.ServiceRequestStatusId,
            ChangedAt = DateTime.Now,
            ChangedByUserAccountId = request.ChangedByUserAccountId
        });
        await db.SaveChangesAsync(cancellationToken);

        var dto = await GetDetailsAsync(entity.ServiceRequestId, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("generate")]
    public async Task<ActionResult> GenerateRequests(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);

        var planTypeId = await GetTypeIdAsync("Плановое техническое обслуживание", cancellationToken);
        var newStatusId = await GetStatusIdAsync("Новая", cancellationToken);
        var inProgressStatusId = await GetStatusIdAsync("В работе", cancellationToken);

        if (planTypeId == 0 || newStatusId == 0 || inProgressStatusId == 0)
        {
            return Problem("Missing ServiceRequestType or ServiceRequestStatus lookups.");
        }

        var openMachineIds = await db.ServiceRequests
            .AsNoTracking()
            .Where(x => x.ServiceRequestStatusId == newStatusId || x.ServiceRequestStatusId == inProgressStatusId)
            .Select(x => x.VendingMachineId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var dueMachines = await db.VendingMachines
            .AsNoTracking()
            .Where(x => x.NextVerificationDate.HasValue && x.NextVerificationDate.Value <= targetDate)
            .Select(x => new { x.VendingMachineId, x.NextVerificationDate })
            .ToListAsync(cancellationToken);

        var toCreate = dueMachines
            .Where(x => !openMachineIds.Contains(x.VendingMachineId))
            .ToList();

        if (toCreate.Count == 0)
        {
            return Ok(new { Created = 0 });
        }

        var now = DateTime.Now;
        var newRequests = new List<ServiceRequest>();

        foreach (var vm in toCreate)
        {
            var planned = vm.NextVerificationDate ?? targetDate;
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
                ChangedAt = DateTime.Now,
                ChangedByUserAccountId = null
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        return Ok(new { Created = toCreate.Count });
    }

    [HttpPost("apply-schedule")]
    public async Task<ActionResult> ApplySchedule(
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var newStatusId = await GetStatusIdAsync("Новая", cancellationToken);
        var inProgressStatusId = await GetStatusIdAsync("В работе", cancellationToken);
        var serviceStatusId = await GetMachineStatusIdAsync("В ремонте/на обслуживании", cancellationToken);

        if (newStatusId == 0 || inProgressStatusId == 0 || serviceStatusId == 0)
        {
            return Problem("Missing ServiceRequestStatus or VendingMachineStatus lookups.");
        }

        var requests = await db.ServiceRequests
            .Where(x => x.PlannedDate == targetDate && x.ServiceRequestStatusId == newStatusId)
            .ToListAsync(cancellationToken);

        if (requests.Count == 0)
        {
            return Ok(new { Updated = 0 });
        }

        var now = DateTime.Now;

        foreach (var req in requests)
        {
            req.ServiceRequestStatusId = inProgressStatusId;
            db.ServiceRequestStatusHistories.Add(new ServiceRequestStatusHistory
            {
                ServiceRequestId = req.ServiceRequestId,
                ServiceRequestStatusId = inProgressStatusId,
                ChangedAt = now,
                ChangedByUserAccountId = null
            });
        }

        var machineIds = requests.Select(x => x.VendingMachineId).Distinct().ToList();
        var machines = await db.VendingMachines
            .Where(x => machineIds.Contains(x.VendingMachineId))
            .ToListAsync(cancellationToken);

        foreach (var vm in machines)
        {
            if (vm.VendingMachineStatusId == serviceStatusId)
            {
                continue;
            }

            vm.VendingMachineStatusId = serviceStatusId;
            db.VendingMachineStatusHistories.Add(new VendingMachineStatusHistory
            {
                VendingMachineId = vm.VendingMachineId,
                VendingMachineStatusId = serviceStatusId,
                ChangedAt = now,
                ChangedByUserAccountId = null
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { Updated = requests.Count });
    }

    private async Task<int> GetStatusIdAsync(string name, CancellationToken cancellationToken)
        => await db.ServiceRequestStatuses
            .AsNoTracking()
            .Where(x => x.Name == name)
            .Select(x => x.ServiceRequestStatusId)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<int> GetTypeIdAsync(string name, CancellationToken cancellationToken)
        => await db.ServiceRequestTypes
            .AsNoTracking()
            .Where(x => x.Name == name)
            .Select(x => x.ServiceRequestTypeId)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<int> GetMachineStatusIdAsync(string name, CancellationToken cancellationToken)
        => await db.VendingMachineStatuses
            .AsNoTracking()
            .Where(x => x.Name == name)
            .Select(x => x.VendingMachineStatusId)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<ServiceRequestDetails> GetDetailsAsync(int serviceRequestId, CancellationToken cancellationToken)
    {
        var item = await db.ServiceRequests
            .AsNoTracking()
            .Where(x => x.ServiceRequestId == serviceRequestId)
            .Select(x => new ServiceRequestDetails(
                x.ServiceRequestId,
                x.VendingMachineId,
                x.VendingMachine != null ? x.VendingMachine.Name : string.Empty,
                x.ServiceRequestTypeId,
                x.ServiceRequestType != null ? x.ServiceRequestType.Name : string.Empty,
                x.ServiceRequestStatusId,
                x.ServiceRequestStatus != null ? x.ServiceRequestStatus.Name : string.Empty,
                x.PlannedDate,
                x.AssignedUserAccountId,
                x.AssignedUserAccount != null
                    ? BuildFullName(x.AssignedUserAccount.LastName, x.AssignedUserAccount.FirstName, x.AssignedUserAccount.Patronymic)
                    : null,
                x.SortOrder,
                x.Notes,
                x.DeclineReason,
                x.CreatedAt))
            .SingleAsync(cancellationToken);

        return item;
    }

    private static string BuildFullName(string lastName, string firstName, string? patronymic)
        => string.Join(" ", new[] { lastName, firstName, patronymic }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static bool IsForeignKeyViolation(DbUpdateException ex)
        => ex.InnerException is SqlException { Number: 547 };
}
