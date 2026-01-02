using System.IO;
using System.Net.Http;
using VendingService.WPF.Services.Api;
using VendingService.WPF.Services.Auth;
using VendingService.WPF.Services.Toasts;

namespace VendingService.WPF.Services;

public sealed class AppServices
{
    public AppSettings Settings { get; }
    public HttpClient HttpClient { get; }
    public ApiClient ApiClient { get; }
    public AuthState AuthState { get; }
    public AuthService AuthService { get; }
    public ToastService ToastService { get; }

    public AppServices()
    {
        var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        Settings = AppSettings.Load(settingsPath);

        HttpClient = new HttpClient
        {
            BaseAddress = new Uri(Settings.ApiBaseUrl, UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(30)
        };

        ApiClient = new ApiClient(HttpClient);
        AuthState = new AuthState();
        AuthService = new AuthService(ApiClient, AuthState);
        ToastService = new ToastService();
    }
}
