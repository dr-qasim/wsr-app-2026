using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VendingService.API.Contracts.Common;
using VendingService.API.Contracts.Sales;
using VendingService.API.Data;
using VendingService.API.Models;

namespace VendingService.API.Controllers;

[ApiController]
[Authorize]
[Route("sales")]
public sealed class SalesController(VendingServiceDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<SaleListItem>>> GetList(
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

        var query = db.Sales.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.SoldAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SaleListItem(
                x.SaleId,
                x.VendingMachineId,
                x.VendingMachine != null ? x.VendingMachine.Name : string.Empty,
                x.ProductId,
                x.Product != null ? x.Product.Name : string.Empty,
                x.Quantity,
                x.TotalAmount,
                x.SoldAt,
                x.SalePaymentMethodId,
                x.SalePaymentMethod != null ? x.SalePaymentMethod.Name : string.Empty))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<SaleListItem>(items, totalCount, page, pageSize));
    }

    [HttpGet("{saleId:int}")]
    public async Task<ActionResult<SaleDetails>> GetById(int saleId, CancellationToken cancellationToken)
    {
        var sale = await db.Sales
            .AsNoTracking()
            .Where(x => x.SaleId == saleId)
            .Select(x => new SaleDetails(
                x.SaleId,
                x.VendingMachineId,
                x.VendingMachine != null ? x.VendingMachine.Name : string.Empty,
                x.ProductId,
                x.Product != null ? x.Product.Name : string.Empty,
                x.Quantity,
                x.TotalAmount,
                x.SoldAt,
                x.SalePaymentMethodId,
                x.SalePaymentMethod != null ? x.SalePaymentMethod.Name : string.Empty))
            .SingleOrDefaultAsync(cancellationToken);

        return sale is null ? NotFound() : Ok(sale);
    }

    [HttpPost]
    public async Task<ActionResult<SaleDetails>> Create([FromBody] CreateSaleRequest request, CancellationToken cancellationToken)
    {
        var sale = new Sale
        {
            VendingMachineId = request.VendingMachineId,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            TotalAmount = request.TotalAmount,
            SoldAt = request.SoldAt,
            SalePaymentMethodId = request.SalePaymentMethodId
        };

        db.Sales.Add(sale);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            return BadRequest(new { Message = "Invalid vending machine, product, or payment method id." });
        }

        var dto = await db.Sales
            .AsNoTracking()
            .Where(x => x.SaleId == sale.SaleId)
            .Select(x => new SaleDetails(
                x.SaleId,
                x.VendingMachineId,
                x.VendingMachine != null ? x.VendingMachine.Name : string.Empty,
                x.ProductId,
                x.Product != null ? x.Product.Name : string.Empty,
                x.Quantity,
                x.TotalAmount,
                x.SoldAt,
                x.SalePaymentMethodId,
                x.SalePaymentMethod != null ? x.SalePaymentMethod.Name : string.Empty))
            .SingleAsync(cancellationToken);

        return CreatedAtAction(nameof(GetById), new { saleId = sale.SaleId }, dto);
    }

    private static bool IsForeignKeyViolation(DbUpdateException ex)
        => ex.InnerException is SqlException { Number: 547 };
}
