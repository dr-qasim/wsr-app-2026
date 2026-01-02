namespace VendingService.API.Auth;

public interface IRefreshTokenStore
{
    void Store(string tokenHash, RefreshTokenEntry entry);
    bool TryGet(string tokenHash, out RefreshTokenEntry entry);
    void Remove(string tokenHash);
}

public readonly record struct RefreshTokenEntry(int UserAccountId, DateTimeOffset ExpiresAtUtc);
