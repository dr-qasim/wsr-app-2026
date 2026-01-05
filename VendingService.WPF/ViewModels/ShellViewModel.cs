using System.Linq;
using System.Windows;
using VendingService.WPF.Services.Toasts;

namespace VendingService.WPF.ViewModels;

public sealed class ShellViewModel : ObservableObject
{
    private object? _currentPage;
    private bool _isSidebarCollapsed;

    public string UserDisplayName { get; }
    public string RoleName { get; }
    public string UserInitials { get; }
    public ToastService ToastService { get; }

    public object? CurrentPage
    {
        get => _currentPage;
        set => SetProperty(ref _currentPage, value);
    }

    public bool IsSidebarCollapsed
    {
        get => _isSidebarCollapsed;
        set => SetProperty(ref _isSidebarCollapsed, value);
    }

    public RelayCommand ToggleSidebarCommand { get; }
    public RelayCommand NavigateDashboardCommand { get; }
    public RelayCommand NavigateVendingMachinesCommand { get; }
    public RelayCommand NavigateCompaniesCommand { get; }
    public RelayCommand NavigateMonitorCommand { get; }
    public RelayCommand NavigateReportsCommand { get; }
    public RelayCommand NavigateInventoryCommand { get; }
    public RelayCommand NavigateUsersCommand { get; }
    public RelayCommand NavigateModemsCommand { get; }
    public RelayCommand NavigateAdditionalCommand { get; }
    public RelayCommand NavigateProfileCommand { get; }
    public AsyncRelayCommand LogoutCommand { get; }

    public ShellViewModel()
    {
        var services = ((App)Application.Current).Services;
        var state = services.AuthState;

        UserDisplayName = state.UserDisplayName ?? state.Email ?? "Пользователь";
        RoleName = state.RoleName ?? "Роль";
        UserInitials = BuildInitials(state.LastName, state.FirstName, state.Email);
        ToastService = services.ToastService;
        _ = services.NotificationsClient.StartAsync();

        ToggleSidebarCommand = new RelayCommand(() => IsSidebarCollapsed = !IsSidebarCollapsed);

        NavigateDashboardCommand = new RelayCommand(() => CurrentPage = new DashboardViewModel());
        NavigateVendingMachinesCommand = new RelayCommand(() => CurrentPage = new VendingMachinesViewModel());
        NavigateCompaniesCommand = new RelayCommand(() => CurrentPage = new CompaniesViewModel());
        NavigateMonitorCommand = new RelayCommand(() => CurrentPage = new MonitorViewModel());
        NavigateReportsCommand = new RelayCommand(() => CurrentPage = new ReportsViewModel());
        NavigateInventoryCommand = new RelayCommand(() => CurrentPage = new InventoryViewModel());
        NavigateUsersCommand = new RelayCommand(() => CurrentPage = new UsersViewModel());
        NavigateModemsCommand = new RelayCommand(() => CurrentPage = new ModemsViewModel());
        NavigateAdditionalCommand = new RelayCommand(() => CurrentPage = new AdditionalViewModel());
        NavigateProfileCommand = new RelayCommand(() => CurrentPage = new ProfileViewModel(state));

        LogoutCommand = new AsyncRelayCommand(async () =>
        {
            await services.NotificationsClient.StopAsync();
            await services.AuthService.LogoutAsync();
            var loginWindow = new Views.LoginWindow();
            loginWindow.Show();

            foreach (var window in Application.Current.Windows.Cast<Window>().ToList())
            {
                if (window is Views.LoginWindow)
                {
                    continue;
                }

                window.Close();
            }
        });

        CurrentPage = new DashboardViewModel();
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
