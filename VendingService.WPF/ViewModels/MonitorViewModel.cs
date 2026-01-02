using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using VendingService.WPF.Contracts.Monitor;
using VendingService.WPF.Services.Api;
using VendingService.WPF.Services.Toasts;

namespace VendingService.WPF.ViewModels;

public sealed class MonitorViewModel : ObservableObject
{
    private sealed record MonitorFilterState(
        int? OverallStatusId,
        IReadOnlySet<int> ConnectionTypeIds,
        IReadOnlySet<string> AdditionalStatusCodes)
    {
        public static readonly MonitorFilterState None = new(
            OverallStatusId: null,
            ConnectionTypeIds: new HashSet<int>(),
            AdditionalStatusCodes: new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    private readonly ApiClient _apiClient;
    private readonly ToastService _toastService;

    private readonly ObservableCollection<MonitorVendingMachineItem> _allItems = new();
    private readonly DispatcherTimer _timer;

    private bool _isLoading;
    private int _afterEventId;
    private DateTime _generatedAt;
    private MonitorFilterState _appliedFilter = MonitorFilterState.None;

    private int? _selectedOverallStatusId;
    private int _workStatusId;
    private int _downStatusId;
    private int _serviceStatusId;

    public string Title => "Монитор ТА";

    public ObservableCollection<MonitorVendingMachineItem> Items { get; } = new();
    public ObservableCollection<CheckableLookupItem> ConnectionTypes { get; } = new();
    public ObservableCollection<CheckableCodeItem> AdditionalStatuses { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public DateTime GeneratedAt
    {
        get => _generatedAt;
        private set
        {
            if (SetProperty(ref _generatedAt, value))
            {
                RaisePropertyChanged(nameof(GeneratedAtText));
            }
        }
    }

    public string GeneratedAtText => $"данные актуальны на {GeneratedAt:HH:mm:ss} (UTC+3)";

    public int? SelectedOverallStatusId
    {
        get => _selectedOverallStatusId;
        set
        {
            if (SetProperty(ref _selectedOverallStatusId, value))
            {
                RaisePropertyChanged(nameof(IsWorkSelected));
                RaisePropertyChanged(nameof(IsDownSelected));
                RaisePropertyChanged(nameof(IsServiceSelected));
            }
        }
    }

    public int WorkStatusId
    {
        get => _workStatusId;
        private set
        {
            if (SetProperty(ref _workStatusId, value))
            {
                RaisePropertyChanged(nameof(IsWorkSelected));
            }
        }
    }

    public int DownStatusId
    {
        get => _downStatusId;
        private set
        {
            if (SetProperty(ref _downStatusId, value))
            {
                RaisePropertyChanged(nameof(IsDownSelected));
            }
        }
    }

    public int ServiceStatusId
    {
        get => _serviceStatusId;
        private set
        {
            if (SetProperty(ref _serviceStatusId, value))
            {
                RaisePropertyChanged(nameof(IsServiceSelected));
            }
        }
    }

    public bool IsWorkSelected => SelectedOverallStatusId.HasValue && SelectedOverallStatusId.Value == WorkStatusId;
    public bool IsDownSelected => SelectedOverallStatusId.HasValue && SelectedOverallStatusId.Value == DownStatusId;
    public bool IsServiceSelected => SelectedOverallStatusId.HasValue && SelectedOverallStatusId.Value == ServiceStatusId;

    public string TotalsText
    {
        get
        {
            var total = Items.Count;
            var work = WorkStatusId == 0 ? 0 : Items.Count(x => x.VendingMachineStatusId == WorkStatusId);
            var down = DownStatusId == 0 ? 0 : Items.Count(x => x.VendingMachineStatusId == DownStatusId);
            var service = ServiceStatusId == 0 ? 0 : Items.Count(x => x.VendingMachineStatusId == ServiceStatusId);

            return $"Итого автоматов: {total} ({work}/{down}/{service})";
        }
    }

    public string MoneyTotalsText
    {
        get
        {
            var cashTotal = Items.Sum(x => x.CashCoins + x.CashBills);
            var changeTotal = Items.Sum(x => x.CashChange);
            return $"Денег в автоматах: {cashTotal:N0} р. + {changeTotal:N0} р. (сдача)";
        }
    }

    public bool IsEmpty => Items.Count == 0;

    public RelayCommand<int> ToggleOverallStatusCommand { get; }
    public RelayCommand ApplyFiltersCommand { get; }
    public RelayCommand ClearFiltersCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }

    public MonitorViewModel()
    {
        var services = ((App)Application.Current).Services;
        _apiClient = services.ApiClient;
        _toastService = services.ToastService;

        ToggleOverallStatusCommand = new RelayCommand<int>(ToggleOverallStatus, id => id > 0);
        ApplyFiltersCommand = new RelayCommand(ApplyFilters);
        ClearFiltersCommand = new RelayCommand(ClearFilters);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsLoading);

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _timer.Tick += async (_, _) => await RefreshAsync();
    }

    public void Start()
    {
        if (!_timer.IsEnabled)
        {
            _timer.Start();
        }

        _ = RefreshAsync();
    }

    public void Stop()
    {
        if (_timer.IsEnabled)
        {
            _timer.Stop();
        }
    }

    private void ToggleOverallStatus(int statusId)
    {
        if (statusId <= 0)
        {
            return;
        }

        SelectedOverallStatusId = SelectedOverallStatusId == statusId
            ? null
            : statusId;
    }

    private void ApplyFilters()
    {
        var selectedConnectionTypeIds = ConnectionTypes
            .Where(x => x.IsSelected)
            .Select(x => x.Id)
            .ToHashSet();

        var selectedAdditionalCodes = AdditionalStatuses
            .Where(x => x.IsSelected)
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        _appliedFilter = new MonitorFilterState(
            OverallStatusId: SelectedOverallStatusId,
            ConnectionTypeIds: selectedConnectionTypeIds,
            AdditionalStatusCodes: selectedAdditionalCodes);

        ApplyFilterToItems();
    }

    private void ClearFilters()
    {
        SelectedOverallStatusId = null;

        foreach (var item in ConnectionTypes)
        {
            item.IsSelected = false;
        }

        foreach (var item in AdditionalStatuses)
        {
            item.IsSelected = false;
        }

        _appliedFilter = MonitorFilterState.None;
        ApplyFilterToItems();
    }

    private async Task EnsureLookupsAsync(CancellationToken cancellationToken)
    {
        if (ConnectionTypes.Count > 0 || AdditionalStatuses.Count > 0)
        {
            return;
        }

        var lookups = await _apiClient.GetMonitorLookupsAsync(cancellationToken);

        ConnectionTypes.Clear();
        foreach (var ct in lookups.ConnectionTypes)
        {
            ConnectionTypes.Add(new CheckableLookupItem(ct.Id, ct.Name));
        }

        AdditionalStatuses.Clear();
        foreach (var st in lookups.AdditionalStatuses)
        {
            AdditionalStatuses.Add(new CheckableCodeItem(st.Code, st.Name));
        }

        // Общие статусы: ожидаем 3 (работает / не работает / обслуживание).
        if (lookups.VendingMachineStatuses.Count >= 3)
        {
            WorkStatusId = lookups.VendingMachineStatuses[0].Id;
            DownStatusId = lookups.VendingMachineStatuses[1].Id;
            ServiceStatusId = lookups.VendingMachineStatuses[2].Id;
        }
    }

    private async Task RefreshAsync()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        RefreshCommand.RaiseCanExecuteChanged();

        try
        {
            await EnsureLookupsAsync(CancellationToken.None);

            var snapshot = await _apiClient.GetMonitorSnapshotAsync(_afterEventId, CancellationToken.None);
            _afterEventId = snapshot.LastEventId;
            GeneratedAt = snapshot.GeneratedAt;

            _allItems.Clear();
            foreach (var item in snapshot.Items)
            {
                _allItems.Add(item);
            }

            foreach (var ev in snapshot.NewEvents)
            {
                EnqueueToast(ev);
            }

            ApplyFilterToItems();
        }
        catch (ApiException ex)
        {
            Stop();
            MessageBox.Show(ex.Message, "Ошибка API", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
            RefreshCommand.RaiseCanExecuteChanged();
        }
    }

    private void ApplyFilterToItems()
    {
        IEnumerable<MonitorVendingMachineItem> query = _allItems;

        if (_appliedFilter.OverallStatusId.HasValue)
        {
            var statusId = _appliedFilter.OverallStatusId.Value;
            query = query.Where(x => x.VendingMachineStatusId == statusId);
        }

        if (_appliedFilter.ConnectionTypeIds.Count > 0)
        {
            query = query.Where(x => x.ConnectionTypeId.HasValue && _appliedFilter.ConnectionTypeIds.Contains(x.ConnectionTypeId.Value));
        }

        if (_appliedFilter.AdditionalStatusCodes.Count > 0)
        {
            query = query.Where(x => x.AdditionalStatusCodes.Any(code => _appliedFilter.AdditionalStatusCodes.Contains(code)));
        }

        Items.Clear();
        foreach (var item in query)
        {
            Items.Add(item);
        }

        RaisePropertyChanged(nameof(TotalsText));
        RaisePropertyChanged(nameof(MoneyTotalsText));
        RaisePropertyChanged(nameof(IsEmpty));
    }

    private void EnqueueToast(MonitorEventItem ev)
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
