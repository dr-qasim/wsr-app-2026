namespace VendingService.WPF.Contracts.Companies;

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
    string Name,
    string? Phone,
    string? Email,
    string? Address);

public sealed record UpdateCompanyRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Address);

