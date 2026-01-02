namespace VendingService.WPF.Contracts.Dashboard;

public sealed record DashboardOverviewResponse(
    DateTime GeneratedAt,
    DashboardEfficiency Efficiency,
    IReadOnlyList<DashboardStatusSlice> NetworkStatuses,
    DashboardSummary Summary,
    IReadOnlyList<DashboardSalesPoint> SalesLast10Days,
    IReadOnlyList<DashboardNewsItem> News);

public sealed record DashboardEfficiency(
    int TotalMachines,
    int WorkingMachines,
    int WorkingPercent);

public sealed record DashboardStatusSlice(
    int StatusId,
    string StatusName,
    int Count);

public sealed record DashboardSummary(
    decimal MoneyInMachines,
    decimal ChangeInMachines,
    decimal IncomeToday,
    decimal IncomeYesterday,
    decimal CashInToday,
    decimal CashInYesterday,
    int ServicedMachinesToday,
    int ServicedMachinesYesterday);

public sealed record DashboardSalesPoint(
    DateTime Date,
    decimal TotalAmount,
    int TotalQuantity);

public sealed record DashboardNewsItem(
    DateTime Date,
    string Title);

