using VendingService.WPF.Services.Auth;

namespace VendingService.WPF.ViewModels;

public sealed class ProfileViewModel : ObservableObject
{
    public string Title => "Мой профиль";

    public string? LastName { get; }
    public string? FirstName { get; }
    public string? Email { get; }
    public string? RoleName { get; }
    public string Initials { get; }

    public ProfileViewModel(AuthState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        LastName = state.LastName;
        FirstName = state.FirstName;
        Email = state.Email;
        RoleName = state.RoleName;

        Initials = BuildInitials(LastName, FirstName, Email);
    }

    private static string BuildInitials(string? lastName, string? firstName, string? email)
    {
        var last = string.IsNullOrWhiteSpace(lastName) ? null : lastName.Trim();
        var first = string.IsNullOrWhiteSpace(firstName) ? null : firstName.Trim();

        if (!string.IsNullOrWhiteSpace(last) || !string.IsNullOrWhiteSpace(first))
        {
            var a = !string.IsNullOrWhiteSpace(last) ? last![0].ToString() : string.Empty;
            var b = !string.IsNullOrWhiteSpace(first) ? first![0].ToString() : string.Empty;
            return (a + b).ToUpperInvariant();
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return "?";
        }

        var prefix = email.Trim().Split('@')[0];
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return "?";
        }

        return prefix[..1].ToUpperInvariant();
    }
}
