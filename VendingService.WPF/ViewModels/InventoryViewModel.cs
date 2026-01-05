using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using VendingService.WPF.Contracts.VendingMachineProducts;
using VendingService.WPF.Contracts.VendingMachines;
using VendingService.WPF.Services.Api;

namespace VendingService.WPF.ViewModels;

public sealed class InventoryViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;

    private bool _isLoading;
    private int _machinesTotal;
    private VendingMachineListItem? _selectedMachine;
    private bool _suppressSelectionLoad;

    public string Title => "Учет ТМЦ";

    public ObservableCollection<VendingMachineListItem> Machines { get; } = new();
    public ObservableCollection<VendingMachineProductItem> Items { get; } = new();

    public int MachinesTotal
    {
        get => _machinesTotal;
        private set
        {
            if (SetProperty(ref _machinesTotal, value))
            {
                RaisePropertyChanged(nameof(MachinesSummary));
            }
        }
    }

    public string MachinesSummary => $"Автоматов: {MachinesTotal}";

    public VendingMachineListItem? SelectedMachine
    {
        get => _selectedMachine;
        set
        {
            if (SetProperty(ref _selectedMachine, value))
            {
                RaisePropertyChanged(nameof(HasSelection));
                RaisePropertyChanged(nameof(EmptyHintText));
                RefreshCommand.RaiseCanExecuteChanged();

                if (!_suppressSelectionLoad)
                {
                    _ = LoadProductsAsync();
                }
            }
        }
    }

    public bool HasSelection => SelectedMachine is not null;
    public bool IsEmpty => Items.Count == 0;

    public string EmptyHintText => SelectedMachine is null
        ? "Выберите торговый автомат."
        : "Нет данных по товарам.";

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                LoadCommand.RaiseCanExecuteChanged();
                RefreshCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public AsyncRelayCommand LoadCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }

    public InventoryViewModel()
    {
        _apiClient = ((App)Application.Current).Services.ApiClient;

        LoadCommand = new AsyncRelayCommand(LoadMachinesAsync, () => !IsLoading);
        RefreshCommand = new AsyncRelayCommand(LoadProductsAsync, () => !IsLoading && SelectedMachine is not null);
    }

    public async Task LoadMachinesAsync()
    {
        IsLoading = true;
        var machinesLoaded = false;

        try
        {
            var result = await _apiClient.GetVendingMachinesAsync(search: null, page: 1, pageSize: 200);

            Machines.Clear();
            foreach (var item in result.Items)
            {
                Machines.Add(item);
            }

            MachinesTotal = result.TotalCount;
            machinesLoaded = true;
        }
        catch (ApiException ex)
        {
            MessageBox.Show(ex.Message, "Ошибка API", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }

        if (!machinesLoaded)
        {
            return;
        }

        _suppressSelectionLoad = true;
        if (SelectedMachine is null && Machines.Count > 0)
        {
            SelectedMachine = Machines.First();
        }
        _suppressSelectionLoad = false;

        await LoadProductsAsync();
    }

    public async Task LoadProductsAsync()
    {
        if (SelectedMachine is null)
        {
            Items.Clear();
            RaisePropertyChanged(nameof(IsEmpty));
            RaisePropertyChanged(nameof(EmptyHintText));
            return;
        }

        IsLoading = true;

        try
        {
            var items = await _apiClient.GetVendingMachineProductsAsync(SelectedMachine.VendingMachineId);

            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }

            RaisePropertyChanged(nameof(IsEmpty));
            RaisePropertyChanged(nameof(EmptyHintText));
        }
        catch (ApiException ex)
        {
            MessageBox.Show(ex.Message, "Ошибка API", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
