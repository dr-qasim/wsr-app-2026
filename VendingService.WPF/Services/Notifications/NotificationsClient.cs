using Microsoft.AspNetCore.SignalR.Client;
using VendingService.WPF.Contracts.Notifications;
using VendingService.WPF.Services.Auth;
using VendingService.WPF.Services.Toasts;
using VendingService.WPF.ViewModels;
using VendingService.WPF.Services;

namespace VendingService.WPF.Services.Notifications;

public sealed class NotificationsClient
{
    private readonly AppSettings _settings;
    private readonly AuthState _authState;
    private readonly ToastService _toastService;
    private HubConnection? _connection;

    public NotificationsClient(AppSettings settings, AuthState authState, ToastService toastService)
    {
        _settings = settings;
        _authState = authState;
        _toastService = toastService;
    }

    public async Task StartAsync()
    {
        if (_authState.Tokens is null || _connection is not null)
        {
            return;
        }

        var hubUrl = new Uri(new Uri(_settings.ApiBaseUrl, UriKind.Absolute), "notifications");

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(_authState.Tokens?.AccessToken);
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<NotificationEventItem>("notification", HandleNotification);

        try
        {
            await _connection.StartAsync();
        }
        catch
        {
            await StopAsync();
        }
    }

    public async Task StopAsync()
    {
        if (_connection is null)
        {
            return;
        }

        try
        {
            await _connection.StopAsync();
        }
        catch
        {
        }
        finally
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    private void HandleNotification(NotificationEventItem ev)
    {
        var level = MapSeverity(ev.SeverityName);
        var duration = level switch
        {
            ToastLevel.Critical => TimeSpan.FromSeconds(10),
            ToastLevel.Warning => TimeSpan.FromSeconds(7),
            _ => TimeSpan.FromSeconds(5)
        };

        var title = string.IsNullOrWhiteSpace(ev.VendingMachineName)
            ? ev.EventTypeName
            : ev.VendingMachineName;

        var message = string.IsNullOrWhiteSpace(ev.Message)
            ? ev.EventTypeName
            : ev.Message!;

        _toastService.Enqueue(
            level: level,
            title: title,
            message: message,
            duration: duration,
            requiresAcknowledgement: level == ToastLevel.Critical);
    }

    private static ToastLevel MapSeverity(string? severityName)
    {
        if (string.IsNullOrWhiteSpace(severityName))
        {
            return ToastLevel.Info;
        }

        return severityName.Trim().ToLowerInvariant() switch
        {
            "критическая" => ToastLevel.Critical,
            "предупреждение" => ToastLevel.Warning,
            _ => ToastLevel.Info
        };
    }
}
