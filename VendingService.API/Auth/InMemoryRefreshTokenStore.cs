using System.Collections.Concurrent;

namespace VendingService.API.Auth;

public sealed class InMemoryRefreshTokenStore : IRefreshTokenStore
{
    private readonly ConcurrentDictionary<string, RefreshTokenEntry> _tokens = new(StringComparer.Ordinal);

    public void Store(string tokenHash, RefreshTokenEntry entry)
    {
        _tokens[tokenHash] = entry;
    }

    public bool TryGet(string tokenHash, out RefreshTokenEntry entry)
    {
        if (_tokens.TryGetValue(tokenHash, out entry))
        {
            if (entry.ExpiresAtUtc <= DateTimeOffset.UtcNow)
            {
                _tokens.TryRemove(tokenHash, out _);
                entry = default;
                return false;
            }

            return true;
        }

        entry = default;
        return false;
    }

    public void Remove(string tokenHash)
    {
        _tokens.TryRemove(tokenHash, out _);
    }
}
