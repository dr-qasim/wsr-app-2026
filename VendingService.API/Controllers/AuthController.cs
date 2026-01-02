using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingService.API.Auth;
using VendingService.API.Contracts.Auth;
using VendingService.API.Data;
using VendingService.API.Models;

namespace VendingService.API.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController(
    VendingServiceDbContext db,
    PasswordHasher passwordHasher,
    JwtTokenService jwtTokenService) : ControllerBase
{
    [HttpPost("bootstrap")]
    public async Task<ActionResult<LoginResponse>> Bootstrap([FromBody] BootstrapRequest request, CancellationToken cancellationToken)
    {
        var hasUsers = await db.UserAccounts.AnyAsync(cancellationToken);
        if (hasUsers)
        {
            return Conflict(new { Message = "Bootstrap is disabled because at least one user already exists." });
        }

        var adminRoleId = await db.UserRoles
            .Where(x => x.Name == "Администратор")
            .Select(x => (int?)x.UserRoleId)
            .FirstOrDefaultAsync(cancellationToken);

        if (adminRoleId is null)
        {
            return Problem("Missing role 'Администратор' in dbo.UserRole.");
        }

        var (passwordHash, passwordSalt) = passwordHasher.HashPassword(request.Password);

        var user = new UserAccount
        {
            Email = request.Email.Trim(),
            Phone = null,
            LastName = request.LastName.Trim(),
            FirstName = request.FirstName.Trim(),
            Patronymic = string.IsNullOrWhiteSpace(request.Patronymic) ? null : request.Patronymic.Trim(),
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            PhotoUrl = null,
            UserRoleId = adminRoleId.Value,
            CompanyId = null,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        db.UserAccounts.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        await db.Entry(user).Reference(x => x.UserRole).LoadAsync(cancellationToken);
        var tokens = jwtTokenService.IssueTokens(user);

        return Ok(new LoginResponse(tokens.AccessToken, tokens.AccessTokenExpiresAtUtc, tokens.RefreshToken, tokens.RefreshTokenExpiresAtUtc));
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();

        var user = await db.UserAccounts
            .Include(x => x.UserRole)
            .SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { Message = "Invalid credentials." });
        }

        var isValid = passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt);
        if (!isValid)
        {
            return Unauthorized(new { Message = "Invalid credentials." });
        }

        var tokens = jwtTokenService.IssueTokens(user);
        return Ok(new LoginResponse(tokens.AccessToken, tokens.AccessTokenExpiresAtUtc, tokens.RefreshToken, tokens.RefreshTokenExpiresAtUtc));
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (!jwtTokenService.TryRotateRefreshToken(request.RefreshToken, out var userAccountId, out _))
        {
            return Unauthorized(new { Message = "Invalid refresh token." });
        }

        var user = await db.UserAccounts
            .Include(x => x.UserRole)
            .SingleOrDefaultAsync(x => x.UserAccountId == userAccountId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { Message = "Invalid refresh token." });
        }

        var tokens = jwtTokenService.IssueTokens(user);
        return Ok(new LoginResponse(tokens.AccessToken, tokens.AccessTokenExpiresAtUtc, tokens.RefreshToken, tokens.RefreshTokenExpiresAtUtc));
    }

    [HttpPost("logout")]
    public ActionResult Logout([FromBody] LogoutRequest request)
    {
        jwtTokenService.RevokeRefreshToken(request.RefreshToken);
        return Ok();
    }
}
