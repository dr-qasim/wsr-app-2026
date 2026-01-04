using System.ComponentModel.DataAnnotations;

namespace VendingService.API.Contracts.Products;

public sealed record ProductListItem(
    int ProductId,
    string Name,
    string? Description,
    decimal Price);

public sealed record ProductDetails(
    int ProductId,
    string Name,
    string? Description,
    decimal Price,
    DateTime CreatedAt);

public sealed record CreateProductRequest(
    [Required] string Name,
    string? Description,
    [Range(0, double.MaxValue)] decimal Price);

public sealed record UpdateProductRequest(
    [Required] string Name,
    string? Description,
    [Range(0, double.MaxValue)] decimal Price);
