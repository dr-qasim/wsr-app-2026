using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VendingService.API.Auth;
using VendingService.API.Contracts.Common;
using VendingService.API.Contracts.Users;
using VendingService.API.Data;
using VendingService.API.Models;

namespace VendingService.API.Controllers;

[ApiController]
[Authorize]
[Route("users")]
public sealed class UsersController(VendingServiceDbContext db, PasswordHasher passwordHasher) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserListItem>>> GetList(
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

        IQueryable<UserAccount> query = db.UserAccounts.AsNoTracking().Include(x => x.UserRole);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(x =>
                x.LastName.Contains(s) ||
                x.FirstName.Contains(s) ||
                x.Email.Contains(s));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserListItem(
                x.UserAccountId,
                BuildFullName(x.LastName, x.FirstName, x.Patronymic),
                x.Email,
                x.Phone,
                x.UserRoleId,
                x.UserRole != null ? x.UserRole.Name : string.Empty))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<UserListItem>(items, totalCount, page, pageSize));
    }

    [HttpGet("{userAccountId:int}")]
    public async Task<ActionResult<UserDetails>> GetById(int userAccountId, CancellationToken cancellationToken)
    {
        var user = await db.UserAccounts
            .AsNoTracking()
            .Include(x => x.UserRole)
            .SingleOrDefaultAsync(x => x.UserAccountId == userAccountId, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(new UserDetails(
            user.UserAccountId,
            user.LastName,
            user.FirstName,
            user.Patronymic,
            user.Email,
            user.Phone,
            user.UserRoleId,
            user.UserRole?.Name ?? string.Empty));
    }

    [HttpPost]
    public async Task<ActionResult<UserDetails>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var (hash, salt) = passwordHasher.HashPassword(request.Password);

        var user = new UserAccount
        {
            Email = request.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            LastName = request.LastName.Trim(),
            FirstName = request.FirstName.Trim(),
            Patronymic = string.IsNullOrWhiteSpace(request.Patronymic) ? null : request.Patronymic.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            PhotoUrl = null,
            UserRoleId = request.UserRoleId,
            CompanyId = null,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        db.UserAccounts.Add(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Conflict(new { Message = "User with the same email already exists." });
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            return BadRequest(new { Message = "Invalid role id." });
        }

        await db.Entry(user).Reference(x => x.UserRole).LoadAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { userAccountId = user.UserAccountId }, new UserDetails(
            user.UserAccountId,
            user.LastName,
            user.FirstName,
            user.Patronymic,
            user.Email,
            user.Phone,
            user.UserRoleId,
            user.UserRole?.Name ?? string.Empty));
    }

    [HttpPut("{userAccountId:int}")]
    public async Task<ActionResult<UserDetails>> Update(int userAccountId, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await db.UserAccounts
            .Include(x => x.UserRole)
            .SingleOrDefaultAsync(x => x.UserAccountId == userAccountId, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        user.Email = request.Email.Trim();
        user.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        user.LastName = request.LastName.Trim();
        user.FirstName = request.FirstName.Trim();
        user.Patronymic = string.IsNullOrWhiteSpace(request.Patronymic) ? null : request.Patronymic.Trim();
        user.UserRoleId = request.UserRoleId;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Conflict(new { Message = "User with the same email already exists." });
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            return BadRequest(new { Message = "Invalid role id." });
        }

        await db.Entry(user).Reference(x => x.UserRole).LoadAsync(cancellationToken);

        return Ok(new UserDetails(
            user.UserAccountId,
            user.LastName,
            user.FirstName,
            user.Patronymic,
            user.Email,
            user.Phone,
            user.UserRoleId,
            user.UserRole?.Name ?? string.Empty));
    }

    [HttpDelete("{userAccountId:int}")]
    public async Task<IActionResult> Delete(int userAccountId, CancellationToken cancellationToken)
    {
        var user = await db.UserAccounts.SingleOrDefaultAsync(x => x.UserAccountId == userAccountId, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        db.UserAccounts.Remove(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { Message = "Cannot delete user because it is referenced by other entities." });
        }

        return NoContent();
    }

    [HttpGet("{userAccountId:int}/models")]
    public async Task<ActionResult<IReadOnlyList<UserModelItem>>> GetModels(int userAccountId, CancellationToken cancellationToken)
    {
        var exists = await db.UserAccounts.AnyAsync(x => x.UserAccountId == userAccountId, cancellationToken);
        if (!exists)
        {
            return NotFound();
        }

        var items = await db.UserAccountVendingMachineModels
            .AsNoTracking()
            .Where(x => x.UserAccountId == userAccountId)
            .Select(x => new UserModelItem(
                x.VendingMachineModelId,
                x.VendingMachineModel != null ? x.VendingMachineModel.Name : string.Empty,
                x.VendingMachineModel != null ? x.VendingMachineModel.VendingMachineManufacturerId : 0,
                x.VendingMachineModel != null && x.VendingMachineModel.VendingMachineManufacturer != null
                    ? x.VendingMachineModel.VendingMachineManufacturer.Name
                    : string.Empty))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPut("{userAccountId:int}/models")]
    public async Task<IActionResult> UpdateModels(
        int userAccountId,
        [FromBody] UpdateUserModelsRequest request,
        CancellationToken cancellationToken)
    {
        var exists = await db.UserAccounts.AnyAsync(x => x.UserAccountId == userAccountId, cancellationToken);
        if (!exists)
        {
            return NotFound();
        }

        var modelIds = request.VendingMachineModelIds
            .Distinct()
            .Where(x => x > 0)
            .ToList();

        var knownModelIds = await db.VendingMachineModels
            .Where(x => modelIds.Contains(x.VendingMachineModelId))
            .Select(x => x.VendingMachineModelId)
            .ToListAsync(cancellationToken);

        if (knownModelIds.Count != modelIds.Count)
        {
            return BadRequest(new { Message = "VendingMachineModelIds contains unknown ids." });
        }

        var existing = await db.UserAccountVendingMachineModels
            .Where(x => x.UserAccountId == userAccountId)
            .ToListAsync(cancellationToken);

        var toRemove = existing.Where(x => !modelIds.Contains(x.VendingMachineModelId)).ToList();
        var existingIds = existing.Select(x => x.VendingMachineModelId).ToHashSet();

        foreach (var id in modelIds)
        {
            if (!existingIds.Contains(id))
            {
                db.UserAccountVendingMachineModels.Add(new UserAccountVendingMachineModel
                {
                    UserAccountId = userAccountId,
                    VendingMachineModelId = id
                });
            }
        }

        if (toRemove.Count > 0)
        {
            db.UserAccountVendingMachineModels.RemoveRange(toRemove);
        }

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string BuildFullName(string lastName, string firstName, string? patronymic)
        => string.Join(" ", new[] { lastName, firstName, patronymic }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => ex.InnerException is SqlException { Number: 2601 or 2627 };

    private static bool IsForeignKeyViolation(DbUpdateException ex)
        => ex.InnerException is SqlException { Number: 547 };
}
