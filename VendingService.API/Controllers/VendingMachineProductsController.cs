using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VendingService.API.Contracts.VendingMachineProducts;
using VendingService.API.Data;
using VendingService.API.Models;

namespace VendingService.API.Controllers;

[ApiController]
[Authorize]
[Route("vending-machines/{vendingMachineId:int}/products")]
public sealed class VendingMachineProductsController(VendingServiceDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VendingMachineProductItem>>> GetList(
        int vendingMachineId,
        CancellationToken cancellationToken = default)
    {
        var items = await db.VendingMachineProducts
            .AsNoTracking()
            .Where(x => x.VendingMachineId == vendingMachineId)
            .OrderBy(x => x.Product != null ? x.Product.Name : string.Empty)
            .Select(x => new VendingMachineProductItem(
                x.VendingMachineId,
                x.ProductId,
                x.Product != null ? x.Product.Name : string.Empty,
                x.QuantityOnHand,
                x.MinimumStock,
                x.AverageDailySales,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpPut("{productId:int}")]
    public async Task<ActionResult<VendingMachineProductItem>> Upsert(
        int vendingMachineId,
        int productId,
        [FromBody] UpsertVendingMachineProductRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await db.VendingMachineProducts
            .SingleOrDefaultAsync(x => x.VendingMachineId == vendingMachineId && x.ProductId == productId, cancellationToken);

        if (entity is null)
        {
            entity = new VendingMachineProduct
            {
                VendingMachineId = vendingMachineId,
                ProductId = productId
            };
            db.VendingMachineProducts.Add(entity);
        }

        entity.QuantityOnHand = request.QuantityOnHand;
        entity.MinimumStock = request.MinimumStock;
        entity.AverageDailySales = request.AverageDailySales;
        entity.UpdatedAt = DateTime.Now;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
        {
            return BadRequest(new { Message = "Invalid vending machine or product id." });
        }

        var dto = await db.VendingMachineProducts
            .AsNoTracking()
            .Where(x => x.VendingMachineId == vendingMachineId && x.ProductId == productId)
            .Select(x => new VendingMachineProductItem(
                x.VendingMachineId,
                x.ProductId,
                x.Product != null ? x.Product.Name : string.Empty,
                x.QuantityOnHand,
                x.MinimumStock,
                x.AverageDailySales,
                x.UpdatedAt))
            .SingleAsync(cancellationToken);

        return Ok(dto);
    }

    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> Delete(
        int vendingMachineId,
        int productId,
        CancellationToken cancellationToken)
    {
        var entity = await db.VendingMachineProducts
            .SingleOrDefaultAsync(x => x.VendingMachineId == vendingMachineId && x.ProductId == productId, cancellationToken);

        if (entity is null)
        {
            return NotFound();
        }

        db.VendingMachineProducts.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static bool IsForeignKeyViolation(DbUpdateException ex)
        => ex.InnerException is SqlException { Number: 547 };
}
