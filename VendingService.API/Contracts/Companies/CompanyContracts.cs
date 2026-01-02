using System.ComponentModel.DataAnnotations;

namespace VendingService.API.Contracts.Companies;

public sealed record CompanyListItem(
    int CompanyId,
    string Name,
    string? Phone,
    string? Email,
    string? Address);

public sealed record CompanyDetails(
    int CompanyId,
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    DateTime CreatedAt);

public sealed record CreateCompanyRequest(
    [Required] string Name,
    string? Phone,
    [EmailAddress] string? Email,
    string? Address);

public sealed record UpdateCompanyRequest(
    [Required] string Name,
    string? Phone,
    [EmailAddress] string? Email,
    string? Address);

