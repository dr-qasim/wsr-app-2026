using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using VendingService.WPF.Contracts.Lookups;
using VendingService.WPF.Services.Api;

namespace VendingService.WPF.ViewModels;

public sealed record LookupGroup(string Title, ObservableCollection<string> Items);

public sealed class AdditionalViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;
    private bool _isLoading;

    public string Title => "Дополнительные";

    public ObservableCollection<LookupGroup> Groups { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                LoadCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public AsyncRelayCommand LoadCommand { get; }

    public AdditionalViewModel()
    {
        _apiClient = ((App)Application.Current).Services.ApiClient;
        LoadCommand = new AsyncRelayCommand(LoadAsync, () => !IsLoading);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;

        try
        {
            var lookups = await _apiClient.GetVendingMachineCreateLookupsAsync();
            Groups.Clear();

            AddGroup("Страны", lookups.Countries.Select(x => x.Name));
            AddGroup("Часовые пояса", lookups.TimeZones.Select(x => x.Name));
            AddGroup("Режимы работы", lookups.WorkModes.Select(x => x.Name));
            AddGroup("Приоритеты обслуживания", lookups.ServicePriorities.Select(x => x.Name));
            AddGroup("Статусы ТА", lookups.VendingMachineStatuses.Select(x => x.Name));
            AddGroup("Матрицы товаров", lookups.ProductMatrices.Select(x => x.Name));
            AddGroup("Шаблоны критических значений", lookups.CriticalValuesTemplates.Select(x => x.Name));
            AddGroup("Шаблоны уведомлений", lookups.NotificationTemplates.Select(x => x.Name));
            AddGroup("Платежные системы", lookups.PaymentSystems.Select(x => x.Name));
            AddGroup("Производители", lookups.Manufacturers.Select(x => x.Name));
            AddGroup("Модели", lookups.Models.Select(x => $"{x.ManufacturerName} {x.Name}".Trim()));
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

    private void AddGroup(string title, IEnumerable<string> items)
    {
        var list = items
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (list.Count == 0)
        {
            list.Add("Нет данных");
        }

        Groups.Add(new LookupGroup(title, new ObservableCollection<string>(list)));
    }
}
