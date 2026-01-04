using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using VendingService.API.Contracts.Common;
using VendingService.API.Contracts.Products;
using VendingService.API.Data;
using VendingService.API.Models;

namespace VendingService.API.Controllers;

[ApiController]
[Authorize]
[Route("products")]
public sealed class ProductsController(VendingServiceDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductListItem>>> GetList(
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

        var query = db.Products.AsNoTracking();
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
            .Select(x => new ProductListItem(x.ProductId, x.Name, x.Description, x.Price))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<ProductListItem>(items, totalCount, page, pageSize));
    }

    [HttpGet("{productId:int}")]
    public async Task<ActionResult<ProductDetails>> GetById(int productId, CancellationToken cancellationToken)
    {
        var product = await db.Products
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Select(x => new ProductDetails(x.ProductId, x.Name, x.Description, x.Price, x.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDetails>> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Price = request.Price,
            CreatedAt = DateTime.Now
        };

        db.Products.Add(product);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Conflict(new { Message = "Product with the same name already exists." });
        }

        var dto = new ProductDetails(product.ProductId, product.Name, product.Description, product.Price, product.CreatedAt);
        return CreatedAtAction(nameof(GetById), new { productId = product.ProductId }, dto);
    }

    [HttpPut("{productId:int}")]
    public async Task<ActionResult<ProductDetails>> Update(int productId, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await db.Products.SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        product.Name = request.Name.Trim();
        product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        product.Price = request.Price;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return Conflict(new { Message = "Product with the same name already exists." });
        }

        return Ok(new ProductDetails(product.ProductId, product.Name, product.Description, product.Price, product.CreatedAt));
    }

    [HttpDelete("{productId:int}")]
    public async Task<IActionResult> Delete(int productId, CancellationToken cancellationToken)
    {
        var product = await db.Products.SingleOrDefaultAsync(x => x.ProductId == productId, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        db.Products.Remove(product);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { Message = "Cannot delete product because it is referenced by other entities." });
        }

        return NoContent();
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => ex.InnerException is SqlException { Number: 2601 or 2627 };
}
