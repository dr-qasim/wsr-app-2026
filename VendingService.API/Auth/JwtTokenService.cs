using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VendingService.API.Models;

namespace VendingService.API.Auth;

public sealed class JwtTokenService(IOptions<JwtOptions> options, IRefreshTokenStore refreshTokenStore)
{
    private readonly JwtOptions _options = options.Value;
    private readonly IRefreshTokenStore _refreshTokenStore = refreshTokenStore;

    public TokenPair IssueTokens(UserAccount user)
    {
        var now = DateTimeOffset.UtcNow;
        var accessExpiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        var refreshExpiresAt = now.AddDays(_options.RefreshTokenDays);

        var accessToken = CreateAccessToken(user, accessExpiresAt);
        var refreshToken = CreateRefreshToken();
        var refreshTokenHash = HashToken(refreshToken);

        _refreshTokenStore.Store(refreshTokenHash, new RefreshTokenEntry(user.UserAccountId, refreshExpiresAt));

        return new TokenPair(accessToken, accessExpiresAt, refreshToken, refreshExpiresAt);
    }

    public bool TryRotateRefreshToken(string refreshToken, out int userAccountId, out TokenPair newTokens)
    {
        var refreshTokenHash = HashToken(refreshToken);
        if (!_refreshTokenStore.TryGet(refreshTokenHash, out var entry))
        {
            userAccountId = default;
            newTokens = default!;
            return false;
        }

        _refreshTokenStore.Remove(refreshTokenHash);
        userAccountId = entry.UserAccountId;
        newTokens = default!;
        return true;
    }

    public void RevokeRefreshToken(string refreshToken)
    {
        var refreshTokenHash = HashToken(refreshToken);
        _refreshTokenStore.Remove(refreshTokenHash);
    }

    private string CreateAccessToken(UserAccount user, DateTimeOffset expiresAtUtc)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserAccountId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(ClaimTypes.Role, user.UserRole?.Name ?? string.Empty),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims.Where(c => !string.IsNullOrWhiteSpace(c.Value)),
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}

public sealed record TokenPair(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc);
