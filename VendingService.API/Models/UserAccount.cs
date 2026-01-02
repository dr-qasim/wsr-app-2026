namespace VendingService.API.Models;

public sealed class UserAccount
{
    public int UserAccountId { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }

    public required string LastName { get; set; }
    public required string FirstName { get; set; }
    public string? Patronymic { get; set; }

    public required string PasswordHash { get; set; }
    public string? PasswordSalt { get; set; }

    public string? PhotoUrl { get; set; }

    public int UserRoleId { get; set; }
    public UserRole? UserRole { get; set; }

    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

