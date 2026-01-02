using System.Windows;

namespace VendingService.WPF.ViewModels;

public sealed class SessionsViewModel : ObservableObject
{
    public string Title => "Мои сессии";

    public DateTimeOffset? AccessTokenExpiresAtUtc { get; }
    public DateTimeOffset? RefreshTokenExpiresAtUtc { get; }

    public string AccessTokenExpiresAtText => AccessTokenExpiresAtUtc is null
        ? "—"
        : $"{AccessTokenExpiresAtUtc.Value.ToLocalTime():dd.MM.yyyy HH:mm:ss} (local)";

    public string RefreshTokenExpiresAtText => RefreshTokenExpiresAtUtc is null
        ? "—"
        : $"{RefreshTokenExpiresAtUtc.Value.ToLocalTime():dd.MM.yyyy HH:mm:ss} (local)";

    public SessionsViewModel()
    {
        var state = ((App)Application.Current).Services.AuthState;
        AccessTokenExpiresAtUtc = state.Tokens?.AccessTokenExpiresAtUtc;
        RefreshTokenExpiresAtUtc = state.Tokens?.RefreshTokenExpiresAtUtc;
    }
}

