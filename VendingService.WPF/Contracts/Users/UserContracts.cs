namespace VendingService.WPF.Contracts.Users;

public sealed record UserListItem(
    int UserAccountId,
    string FullName,
    string Email,
    string? Phone,
    int UserRoleId,
    string RoleName);
