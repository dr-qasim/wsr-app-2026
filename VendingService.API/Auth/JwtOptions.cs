namespace VendingService.API.Auth;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "VendingService";
    public string Audience { get; init; } = "VendingService";
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 30;
}

