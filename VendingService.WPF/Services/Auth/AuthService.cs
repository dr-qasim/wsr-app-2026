using VendingService.WPF.Contracts.Auth;
using VendingService.WPF.Services.Api;

namespace VendingService.WPF.Services.Auth;

public sealed class AuthService
{
    private readonly ApiClient _apiClient;
    private readonly AuthState _authState;

    public AuthState State => _authState;

    public AuthService(ApiClient apiClient, AuthState authState)
    {
        _apiClient = apiClient;
        _authState = authState;
    }

    public async Task LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var tokens = await _apiClient.LoginAsync(new LoginRequest(email, password), cancellationToken);
        _authState.SetTokens(tokens);
        _apiClient.SetBearerToken(tokens.AccessToken);
    }

    public async Task<bool> TryRefreshAsync(CancellationToken cancellationToken = default)
    {
        var refreshToken = _authState.Tokens?.RefreshToken;
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        var tokens = await _apiClient.RefreshTokenAsync(new RefreshTokenRequest(refreshToken), cancellationToken);
        _authState.SetTokens(tokens);
        _apiClient.SetBearerToken(tokens.AccessToken);
        return true;
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var refreshToken = _authState.Tokens?.RefreshToken;

        try
        {
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await _apiClient.LogoutAsync(new LogoutRequest(refreshToken), cancellationToken);
            }
        }
        finally
        {
            _authState.Clear();
            _apiClient.SetBearerToken(null);
        }
    }
}

