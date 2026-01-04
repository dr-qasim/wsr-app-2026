using System.ComponentModel.DataAnnotations;

namespace VendingService.API.Contracts.Users;

public sealed record UserListItem(
    int UserAccountId,
    string FullName,
    string Email,
    string? Phone,
    int UserRoleId,
    string RoleName);

public sealed record UserDetails(
    int UserAccountId,
    string LastName,
    string FirstName,
    string? Patronymic,
    string Email,
    string? Phone,
    int UserRoleId,
    string RoleName);

public sealed record CreateUserRequest(
    [Required] string LastName,
    [Required] string FirstName,
    string? Patronymic,
    [Required][EmailAddress] string Email,
    string? Phone,
    [Required] int UserRoleId,
    [Required] string Password);

public sealed record UpdateUserRequest(
    [Required] string LastName,
    [Required] string FirstName,
    string? Patronymic,
    [Required][EmailAddress] string Email,
    string? Phone,
    [Required] int UserRoleId);
