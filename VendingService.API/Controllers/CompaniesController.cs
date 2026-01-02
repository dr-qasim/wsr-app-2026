using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VendingService.API.Contracts.Common;
using VendingService.API.Contracts.Companies;
using VendingService.API.Data;
using VendingService.API.Models;

namespace VendingService.API.Controllers;

[ApiController]
[Authorize]
[Route("companies")]
public sealed class CompaniesController(VendingServiceDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CompanyListItem>>> GetList(
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

        var query = db.Companies.AsNoTracking();
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
            .Select(x => new CompanyListItem(x.CompanyId, x.Name, x.Phone, x.Email, x.Address))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<CompanyListItem>(items, totalCount, page, pageSize));
    }

    [HttpGet("{companyId:int}")]
    public async Task<ActionResult<CompanyDetails>> GetById(int companyId, CancellationToken cancellationToken)
    {
        var company = await db.Companies
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId)
            .Select(x => new CompanyDetails(x.CompanyId, x.Name, x.Phone, x.Email, x.Address, x.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return company is null ? NotFound() : Ok(company);
    }

    [HttpPost]
    public async Task<ActionResult<CompanyDetails>> Create([FromBody] CreateCompanyRequest request, CancellationToken cancellationToken)
    {
        var company = new Company
        {
            Name = request.Name.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            CreatedAt = DateTime.Now
        };

        db.Companies.Add(company);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Conflict(new { Message = "Company with the same name already exists." });
        }

        var dto = new CompanyDetails(company.CompanyId, company.Name, company.Phone, company.Email, company.Address, company.CreatedAt);
        return CreatedAtAction(nameof(GetById), new { companyId = company.CompanyId }, dto);
    }

    [HttpPut("{companyId:int}")]
    public async Task<ActionResult<CompanyDetails>> Update(int companyId, [FromBody] UpdateCompanyRequest request, CancellationToken cancellationToken)
    {
        var company = await db.Companies.SingleOrDefaultAsync(x => x.CompanyId == companyId, cancellationToken);
        if (company is null)
        {
            return NotFound();
        }

        company.Name = request.Name.Trim();
        company.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        company.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        company.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Conflict(new { Message = "Company with the same name already exists." });
        }

        return Ok(new CompanyDetails(company.CompanyId, company.Name, company.Phone, company.Email, company.Address, company.CreatedAt));
    }

    [HttpDelete("{companyId:int}")]
    public async Task<IActionResult> Delete(int companyId, CancellationToken cancellationToken)
    {
        var company = await db.Companies.SingleOrDefaultAsync(x => x.CompanyId == companyId, cancellationToken);
        if (company is null)
        {
            return NotFound();
        }

        db.Companies.Remove(company);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { Message = "Cannot delete company because it is referenced by other entities." });
        }

        return NoContent();
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => ex.InnerException is SqlException { Number: 2601 or 2627 };
}
