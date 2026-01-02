using System.Text;
using System.Text.Json;
using VendingService.WPF.Contracts.Auth;

namespace VendingService.WPF.Services.Auth;

public sealed class AuthState
{
    public LoginResponse? Tokens { get; private set; }

    public bool IsAuthenticated => Tokens is not null && Tokens.AccessTokenExpiresAtUtc > DateTimeOffset.UtcNow;

    public string? LastName { get; private set; }
    public string? FirstName { get; private set; }
    public string? UserDisplayName { get; private set; }
    public string? RoleName { get; private set; }
    public string? Email { get; private set; }

    public void SetTokens(LoginResponse tokens)
    {
        Tokens = tokens;
        ParseJwt(tokens.AccessToken);
    }

    public void Clear()
    {
        Tokens = null;
        LastName = null;
        FirstName = null;
        UserDisplayName = null;
        RoleName = null;
        Email = null;
    }

    private void ParseJwt(string jwt)
    {
        Email = null;
        LastName = null;
        FirstName = null;
        UserDisplayName = null;
        RoleName = null;

        var parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            return;
        }

        try
        {
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            Email = GetString(doc, "email");
            FirstName = GetString(doc, "given_name");
            LastName = GetString(doc, "family_name");
            RoleName = GetString(doc, "role");

            var display = string.Join(" ", new[] { LastName, FirstName }.Where(x => !string.IsNullOrWhiteSpace(x)));
            UserDisplayName = string.IsNullOrWhiteSpace(display) ? Email : display;
        }
        catch
        {
            // Ignore invalid JWT payload parsing errors (display name will be empty).
        }
    }

    private static string? GetString(JsonDocument doc, string propertyName)
    {
        if (doc.RootElement.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }

        return null;
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        var padding = 4 - (base64.Length % 4);
        if (padding is > 0 and < 4)
        {
            base64 += new string('=', padding);
        }

        return Convert.FromBase64String(base64);
    }
}
