using System.Collections.ObjectModel;
using System.Windows;
using VendingService.WPF.Contracts.Common;
using VendingService.WPF.Contracts.VendingMachines;
using VendingService.WPF.Services.Api;

namespace VendingService.WPF.ViewModels;

public sealed class VendingMachinesViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;

    private string _search = string.Empty;
    private bool _isLoading;
    private int _page = 1;
    private int _pageSize = 50;
    private int _totalCount;
    private bool _isTileView;

    public ObservableCollection<VendingMachineListItem> Items { get; } = new();

    public IReadOnlyList<int> PageSizes { get; } = [10, 20, 50, 100];

    public string Search
    {
        get => _search;
        set => SetProperty(ref _search, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                LoadCommand.RaiseCanExecuteChanged();
                SearchCommand.RaiseCanExecuteChanged();
                NextPageCommand.RaiseCanExecuteChanged();
                PrevPageCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int Page
    {
        get => _page;
        set
        {
            if (SetProperty(ref _page, Math.Max(1, value)))
            {
                RaisePropertyChanged(nameof(PageInfo));
                RaisePropertyChanged(nameof(CanGoPrev));
                RaisePropertyChanged(nameof(CanGoNext));
                NextPageCommand.RaiseCanExecuteChanged();
                PrevPageCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (SetProperty(ref _pageSize, value))
            {
                Page = 1;
                _ = LoadAsync();
                RaisePropertyChanged(nameof(PageInfo));
            }
        }
    }

    public int TotalCount
    {
        get => _totalCount;
        set
        {
            if (SetProperty(ref _totalCount, value))
            {
                RaisePropertyChanged(nameof(PageInfo));
                RaisePropertyChanged(nameof(CanGoNext));
                NextPageCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsTileView
    {
        get => _isTileView;
        set => SetProperty(ref _isTileView, value);
    }

    public bool CanGoPrev => Page > 1;
    public bool CanGoNext => Page * PageSize < TotalCount;

    public string PageInfo
    {
        get
        {
            if (TotalCount <= 0)
            {
                return "Записей: 0";
            }

            var from = (Page - 1) * PageSize + 1;
            var to = Math.Min(Page * PageSize, TotalCount);
            return $"Записи {from}-{to} из {TotalCount}";
        }
    }

    public AsyncRelayCommand LoadCommand { get; }
    public AsyncRelayCommand SearchCommand { get; }
    public AsyncRelayCommand NextPageCommand { get; }
    public AsyncRelayCommand PrevPageCommand { get; }
    public AsyncRelayCommand<int> DeleteCommand { get; }
    public AsyncRelayCommand<int> DetachModemCommand { get; }

    public VendingMachinesViewModel()
    {
        _apiClient = ((App)Application.Current).Services.ApiClient;

        LoadCommand = new AsyncRelayCommand(LoadAsync, () => !IsLoading);
        SearchCommand = new AsyncRelayCommand(() =>
        {
            Page = 1;
            return LoadAsync();
        }, () => !IsLoading);

        NextPageCommand = new AsyncRelayCommand(async () =>
        {
            Page += 1;
            await LoadAsync();
        }, () => !IsLoading && CanGoNext);

        PrevPageCommand = new AsyncRelayCommand(async () =>
        {
            Page -= 1;
            await LoadAsync();
        }, () => !IsLoading && CanGoPrev);

        DeleteCommand = new AsyncRelayCommand<int>(DeleteAsync, _ => !IsLoading);
        DetachModemCommand = new AsyncRelayCommand<int>(DetachModemAsync, _ => !IsLoading);
    }

    public async Task LoadAsync()
    {
        IsLoading = true;

        try
        {
            var result = await _apiClient.GetVendingMachinesAsync(Search, Page, PageSize);
            ApplyResult(result);
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

    private void ApplyResult(PagedResult<VendingMachineListItem> result)
    {
        Items.Clear();
        foreach (var item in result.Items)
        {
            Items.Add(item);
        }

        _totalCount = result.TotalCount;
        _page = result.Page;
        _pageSize = result.PageSize;
        RaisePropertyChanged(nameof(TotalCount));
        RaisePropertyChanged(nameof(Page));
        RaisePropertyChanged(nameof(PageSize));
        RaisePropertyChanged(nameof(PageInfo));
        RaisePropertyChanged(nameof(CanGoPrev));
        RaisePropertyChanged(nameof(CanGoNext));
        NextPageCommand.RaiseCanExecuteChanged();
        PrevPageCommand.RaiseCanExecuteChanged();
    }

    private async Task DeleteAsync(int vendingMachineId)
    {
        var result = MessageBox.Show(
            $"Удалить торговый автомат #{vendingMachineId}?",
            "Подтверждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _apiClient.DeleteVendingMachineAsync(vendingMachineId);
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            MessageBox.Show(ex.Message, "Ошибка API", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task DetachModemAsync(int vendingMachineId)
    {
        var result = MessageBox.Show(
            $"Отвязать модем от торгового автомата #{vendingMachineId}?\n\nПосле отвязки модем будет отображаться как -1.",
            "Подтверждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _apiClient.DetachVendingMachineModemAsync(vendingMachineId);
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            MessageBox.Show(ex.Message, "Ошибка API", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
