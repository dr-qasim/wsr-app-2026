using System.Collections.ObjectModel;
using System.Windows;
using VendingService.WPF.Contracts.Dashboard;
using VendingService.WPF.Services.Api;

namespace VendingService.WPF.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;

    private bool _isLoading;
    private string _updatedAtText = string.Empty;

    private readonly NetworkEfficiencyTileViewModel _efficiencyTile = new();
    private readonly NetworkStateTileViewModel _networkStateTile = new();
    private readonly SummaryTileViewModel _summaryTile = new();
    private readonly SalesDynamicsTileViewModel _salesTile = new();
    private readonly NewsTileViewModel _newsTile = new();

    public string Title => "Личный кабинет. Главная";

    public ObservableCollection<DashboardTileViewModel> Tiles { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                RefreshCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string UpdatedAtText
    {
        get => _updatedAtText;
        set => SetProperty(ref _updatedAtText, value);
    }

    public AsyncRelayCommand RefreshCommand { get; }
    public RelayCommand ShowAllTilesCommand { get; }

    public DashboardViewModel()
    {
        _apiClient = ((App)Application.Current).Services.ApiClient;

        Tiles.Add(_efficiencyTile);
        Tiles.Add(_networkStateTile);
        Tiles.Add(_summaryTile);
        Tiles.Add(_salesTile);
        Tiles.Add(_newsTile);

        RefreshCommand = new AsyncRelayCommand(LoadAsync, () => !IsLoading);
        ShowAllTilesCommand = new RelayCommand(() =>
        {
            foreach (var tile in Tiles)
            {
                tile.IsVisible = true;
            }
        });

        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        IsLoading = true;

        try
        {
            var overview = await _apiClient.GetDashboardOverviewAsync();
            ApplyOverview(overview);
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

    private void ApplyOverview(DashboardOverviewResponse overview)
    {
        UpdatedAtText = $"данные актуальны на {overview.GeneratedAt:HH:mm:ss} (UTC+3)";

        _efficiencyTile.Update(overview.Efficiency);
        _networkStateTile.Update(overview.NetworkStatuses);
        _summaryTile.Update(overview.Summary);
        _salesTile.Update(overview.SalesLast10Days);
        _newsTile.Update(overview.News);
    }
}
