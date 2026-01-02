using System.Collections.ObjectModel;
using System.Globalization;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using VendingService.WPF.Contracts.Dashboard;

namespace VendingService.WPF.ViewModels;

public abstract class DashboardTileViewModel : ObservableObject
{
    private bool _isVisible = true;

    public string Title { get; }
    public double TileWidth { get; }
    public double TileHeight { get; }

    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    public RelayCommand HideCommand { get; }

    protected DashboardTileViewModel(string title, double tileWidth, double tileHeight)
    {
        Title = title;
        TileWidth = tileWidth;
        TileHeight = tileHeight;
        HideCommand = new RelayCommand(() => IsVisible = false);
    }
}

public sealed class NetworkEfficiencyTileViewModel : DashboardTileViewModel
{
    private PlotModel _plotModel = BuildGaugeModel(0);
    private string _caption = "Работающих автоматов - 0%";

    public PlotModel PlotModel
    {
        get => _plotModel;
        set => SetProperty(ref _plotModel, value);
    }

    public string Caption
    {
        get => _caption;
        set => SetProperty(ref _caption, value);
    }

    public NetworkEfficiencyTileViewModel() : base("Эффективность сети", 360, 200)
    {
    }

    public void Update(DashboardEfficiency efficiency)
    {
        var percent = Math.Clamp(efficiency.WorkingPercent, 0, 100);
        PlotModel = BuildGaugeModel(percent);
        Caption = efficiency.TotalMachines <= 0
            ? "Нет данных по автоматам"
            : $"Работающих автоматов - {percent}%";
    }

    private static PlotModel BuildGaugeModel(int percent)
    {
        var model = CreateBasePlotModel();

        var series = new PieSeries
        {
            StartAngle = 180,
            AngleSpan = 180,
            InnerDiameter = 0.65,
            StrokeThickness = 0,
            InsideLabelFormat = string.Empty,
            OutsideLabelFormat = string.Empty,
            TickHorizontalLength = 0,
            TickRadialLength = 0
        };

        series.Slices.Add(new PieSlice(string.Empty, percent) { Fill = ParseColor("#22C55E") });
        series.Slices.Add(new PieSlice(string.Empty, Math.Max(0, 100 - percent)) { Fill = ParseColor("#E5E7EB") });

        model.Series.Add(series);
        return model;
    }

    private static OxyColor ParseColor(string hex)
        => OxyColor.Parse(hex);

    private static PlotModel CreateBasePlotModel()
        => new()
        {
            Background = OxyColors.Transparent,
            PlotAreaBorderThickness = new OxyThickness(0),
            Padding = new OxyThickness(0),
            PlotMargins = new OxyThickness(0),
            IsLegendVisible = false
        };
}

public sealed class NetworkStateTileViewModel : DashboardTileViewModel
{
    private PlotModel _plotModel = BuildDonutModel(Array.Empty<DashboardStatusSlice>());
    private string _centerText = "Нет данных";

    public PlotModel PlotModel
    {
        get => _plotModel;
        set => SetProperty(ref _plotModel, value);
    }

    public string CenterText
    {
        get => _centerText;
        set => SetProperty(ref _centerText, value);
    }

    public NetworkStateTileViewModel() : base("Состояние сети", 360, 200)
    {
    }

    public void Update(IReadOnlyList<DashboardStatusSlice> statuses)
    {
        PlotModel = BuildDonutModel(statuses);

        var top = statuses
            .Where(x => x.Count > 0)
            .OrderByDescending(x => x.Count)
            .FirstOrDefault();

        CenterText = top is null ? "Нет данных" : $"{top.StatusName}\n{top.Count}";
    }

    private static PlotModel BuildDonutModel(IReadOnlyList<DashboardStatusSlice> statuses)
    {
        var slices = statuses.Where(x => x.Count > 0).ToList();
        if (slices.Count == 0)
        {
            slices.Add(new DashboardStatusSlice(0, "Нет данных", 1));
        }

        var model = new PlotModel
        {
            Background = OxyColors.Transparent,
            PlotAreaBorderThickness = new OxyThickness(0),
            Padding = new OxyThickness(0),
            PlotMargins = new OxyThickness(0),
            IsLegendVisible = false
        };

        var series = new PieSeries
        {
            InnerDiameter = 0.6,
            StrokeThickness = 0,
            InsideLabelFormat = string.Empty,
            OutsideLabelFormat = string.Empty,
            TickHorizontalLength = 0,
            TickRadialLength = 0
        };

        foreach (var s in slices)
        {
            series.Slices.Add(new PieSlice(s.StatusName, s.Count)
            {
                Fill = PickStatusColor(s.StatusName)
            });
        }

        model.Series.Add(series);
        return model;
    }

    private static OxyColor PickStatusColor(string statusName)
    {
        if (statusName.Contains("Работ", StringComparison.OrdinalIgnoreCase))
        {
            return OxyColor.Parse("#22C55E");
        }

        if (statusName.Contains("ремонт", StringComparison.OrdinalIgnoreCase)
            || statusName.Contains("обслуж", StringComparison.OrdinalIgnoreCase))
        {
            return OxyColor.Parse("#3B82F6");
        }

        if (statusName.Contains("не", StringComparison.OrdinalIgnoreCase)
            || statusName.Contains("стро", StringComparison.OrdinalIgnoreCase))
        {
            return OxyColor.Parse("#EF4444");
        }

        return OxyColor.Parse("#9CA3AF");
    }
}

public sealed record DashboardSummaryLine(string Name, string Value);

public sealed class SummaryTileViewModel : DashboardTileViewModel
{
    public ObservableCollection<DashboardSummaryLine> Lines { get; } = new();

    public SummaryTileViewModel() : base("Сводка", 360, 200)
    {
    }

    public void Update(DashboardSummary summary)
    {
        var culture = CultureInfo.GetCultureInfo("ru-RU");

        Lines.Clear();
        Lines.Add(new DashboardSummaryLine("Денег в ТА", $"{summary.MoneyInMachines.ToString("N0", culture)} р."));
        Lines.Add(new DashboardSummaryLine("Сдача в ТА", $"{summary.ChangeInMachines.ToString("N0", culture)} р."));
        Lines.Add(new DashboardSummaryLine("Выручка, сегодня", $"{summary.IncomeToday.ToString("N0", culture)} р."));
        Lines.Add(new DashboardSummaryLine("Выручка, вчера", $"{summary.IncomeYesterday.ToString("N0", culture)} р."));
        Lines.Add(new DashboardSummaryLine("Инкассировано, сегодня", $"{summary.CashInToday.ToString("N0", culture)} р."));
        Lines.Add(new DashboardSummaryLine("Инкассировано, вчера", $"{summary.CashInYesterday.ToString("N0", culture)} р."));
        Lines.Add(new DashboardSummaryLine("Обслужено ТА, сег./вчера", $"{summary.ServicedMachinesToday} / {summary.ServicedMachinesYesterday}"));
    }
}

public enum SalesDynamicsMode
{
    Amount,
    Quantity
}

public sealed class SalesDynamicsTileViewModel : DashboardTileViewModel
{
    private PlotModel _plotModel = BuildSalesModel(Array.Empty<DashboardSalesPoint>(), SalesDynamicsMode.Amount);
    private SalesDynamicsMode _mode = SalesDynamicsMode.Amount;
    private string _rangeText = string.Empty;

    private IReadOnlyList<DashboardSalesPoint> _data = Array.Empty<DashboardSalesPoint>();

    public PlotModel PlotModel
    {
        get => _plotModel;
        set => SetProperty(ref _plotModel, value);
    }

    public SalesDynamicsMode Mode
    {
        get => _mode;
        set
        {
            if (SetProperty(ref _mode, value))
            {
                RaisePropertyChanged(nameof(IsAmountMode));
                RaisePropertyChanged(nameof(IsQuantityMode));
                PlotModel = BuildSalesModel(_data, _mode);
            }
        }
    }

    public bool IsAmountMode => Mode == SalesDynamicsMode.Amount;
    public bool IsQuantityMode => Mode == SalesDynamicsMode.Quantity;

    public string RangeText
    {
        get => _rangeText;
        set => SetProperty(ref _rangeText, value);
    }

    public RelayCommand ShowAmountCommand { get; }
    public RelayCommand ShowQuantityCommand { get; }

    public SalesDynamicsTileViewModel() : base("Динамика продаж за последние 10 дней", 740, 320)
    {
        ShowAmountCommand = new RelayCommand(() => Mode = SalesDynamicsMode.Amount);
        ShowQuantityCommand = new RelayCommand(() => Mode = SalesDynamicsMode.Quantity);
    }

    public void Update(IReadOnlyList<DashboardSalesPoint> salesLast10Days)
    {
        _data = salesLast10Days ?? Array.Empty<DashboardSalesPoint>();
        PlotModel = BuildSalesModel(_data, Mode);

        if (_data.Count > 0)
        {
            var from = _data.Min(x => x.Date).ToString("dd.MM.yyyy");
            var to = _data.Max(x => x.Date).ToString("dd.MM.yyyy");
            RangeText = $"Данные по продажам с {from} по {to}";
        }
        else
        {
            RangeText = string.Empty;
        }
    }

    private static PlotModel BuildSalesModel(IReadOnlyList<DashboardSalesPoint> sales, SalesDynamicsMode mode)
    {
        var model = new PlotModel
        {
            Background = OxyColors.Transparent,
            PlotAreaBorderThickness = new OxyThickness(0),
            IsLegendVisible = false
        };

        var labels = sales.Select(x => x.Date.ToString("dd.MM")).ToList();

        var xAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Minimum = -0.5,
            Maximum = Math.Max(-0.5, labels.Count - 0.5),
            MajorStep = 1,
            MinorStep = 1,
            FontSize = 10,
            IsPanEnabled = false,
            IsZoomEnabled = false,
            LabelFormatter = value =>
            {
                var index = (int)Math.Round(value);
                return index >= 0 && index < labels.Count ? labels[index] : string.Empty;
            }
        };

        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Minimum = 0,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.None,
            MajorGridlineColor = OxyColor.Parse("#E5E7EB")
        };

        model.Axes.Add(xAxis);
        model.Axes.Add(yAxis);

        var series = new RectangleBarSeries
        {
            FillColor = OxyColor.Parse("#93C5FD"),
            StrokeThickness = 0
        };

        for (var i = 0; i < sales.Count; i++)
        {
            var s = sales[i];
            var value = mode == SalesDynamicsMode.Amount ? (double)s.TotalAmount : s.TotalQuantity;
            series.Items.Add(new RectangleBarItem(i - 0.4, 0, i + 0.4, value));
        }

        model.Series.Add(series);
        return model;
    }
}

public sealed record DashboardNewsLine(string DateText, string Title);

public sealed class NewsTileViewModel : DashboardTileViewModel
{
    public ObservableCollection<DashboardNewsLine> Items { get; } = new();

    public NewsTileViewModel() : base("Новости", 360, 320)
    {
    }

    public void Update(IReadOnlyList<DashboardNewsItem> news)
    {
        Items.Clear();

        foreach (var n in news.OrderByDescending(x => x.Date).Take(10))
        {
            Items.Add(new DashboardNewsLine(n.Date.ToString("dd.MM.yy"), n.Title));
        }
    }
}
