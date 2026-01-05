using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VendingService.API.Data;

namespace VendingService.API.Pages.Web;

public enum CalendarViewMode
{
    Week,
    Month,
    Year
}

public enum CalendarFilterMode
{
    All,
    Single
}

public sealed class CalendarModel : PageModel
{
    private readonly VendingServiceDbContext _db;

    public bool IsEnglish => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";

    [BindProperty(SupportsGet = true)]
    public string? ViewMode { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Mode { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? MachineId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? Date { get; set; }

    public CalendarViewMode View { get; private set; } = CalendarViewMode.Month;
    public CalendarFilterMode ModeValue { get; private set; } = CalendarFilterMode.All;
    public DateOnly ReferenceDate { get; private set; }
    public int? SelectedMachineId { get; private set; }

    public List<MachineOption> Machines { get; } = new();
    public List<CalendarDay> Days { get; } = new();
    public List<CalendarMonth> Months { get; } = new();

    public CalendarModel(VendingServiceDbContext db)
    {
        _db = db;
    }

    public async Task OnGetAsync()
    {
        View = ViewMode?.ToLowerInvariant() switch
        {
            "week" => CalendarViewMode.Week,
            "year" => CalendarViewMode.Year,
            _ => CalendarViewMode.Month
        };

        ModeValue = Mode?.ToLowerInvariant() == "single" ? CalendarFilterMode.Single : CalendarFilterMode.All;
        SelectedMachineId = MachineId;
        ReferenceDate = Date ?? DateOnly.FromDateTime(DateTime.Today);

        IQueryable<Models.VendingMachine> machineQuery = _db.VendingMachines
            .AsNoTracking()
            .Include(x => x.VendingMachineModel)
            .ThenInclude(x => x!.VendingMachineManufacturer)
            .Include(x => x.Company);

        if (ModeValue == CalendarFilterMode.Single && SelectedMachineId.HasValue)
        {
            machineQuery = machineQuery.Where(x => x.VendingMachineId == SelectedMachineId.Value);
        }

        var machines = await machineQuery.ToListAsync();

        var allMachines = await _db.VendingMachines
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new MachineOption(x.VendingMachineId, x.Name))
            .ToListAsync();
        Machines.Clear();
        Machines.AddRange(allMachines);

        var itemsByDate = BuildSchedule(machines);

        if (View == CalendarViewMode.Year)
        {
            for (var month = 1; month <= 12; month++)
            {
                var monthDate = new DateOnly(ReferenceDate.Year, month, 1);
                Months.Add(BuildMonth(monthDate, itemsByDate, IsEnglish));
            }

            return;
        }

        if (View == CalendarViewMode.Week)
        {
            var start = StartOfWeek(ReferenceDate);
            for (var i = 0; i < 7; i++)
            {
                var day = start.AddDays(i);
                Days.Add(BuildDay(day, itemsByDate, false));
            }

            return;
        }

        var monthBlock = BuildMonth(ReferenceDate, itemsByDate, IsEnglish);
        Days.AddRange(monthBlock.Days);
    }

    private static Dictionary<DateOnly, List<CalendarItem>> BuildSchedule(IReadOnlyList<Models.VendingMachine> machines)
    {
        var result = new Dictionary<DateOnly, List<CalendarItem>>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        foreach (var machine in machines)
        {
            var planned = machine.NextVerificationDate;
            if (!planned.HasValue && machine.LastVerificationDate.HasValue && machine.VerificationIntervalMonths.HasValue)
            {
                planned = machine.LastVerificationDate.Value.AddMonths(machine.VerificationIntervalMonths.Value);
            }

            if (machine.ResourceHours.HasValue)
            {
                var due = EstimateByResource(machine, today);
                if (!planned.HasValue || due < planned.Value)
                {
                    planned = due;
                }
            }

            if (!planned.HasValue)
            {
                continue;
            }

            var color = GetColorClass(planned.Value, today);
            var companyName = machine.Company?.Name ?? "-";
            var model = machine.VendingMachineModel?.Name ?? "-";
            var manufacturer = machine.VendingMachineModel?.VendingMachineManufacturer?.Name ?? "-";
            var tooltip = $"{manufacturer} {model} | {machine.Place} | {companyName}";

            if (!result.TryGetValue(planned.Value, out var list))
            {
                list = new List<CalendarItem>();
                result[planned.Value] = list;
            }

            list.Add(new CalendarItem(machine.Name, color, tooltip));
        }

        return result;
    }

    private static DateOnly EstimateByResource(Models.VendingMachine machine, DateOnly today)
    {
        var created = machine.CreatedAt.Date;
        var usedHours = (DateTime.Today - created).TotalDays * 8;
        var threshold = machine.ResourceHours!.Value * 0.85;
        var remaining = Math.Max(0, threshold - usedHours);
        var daysLeft = remaining / 8;
        return DateOnly.FromDateTime(DateTime.Today.AddDays(daysLeft));
    }

    private static CalendarDay BuildDay(DateOnly date, Dictionary<DateOnly, List<CalendarItem>> itemsByDate, bool isOutsideMonth)
    {
        itemsByDate.TryGetValue(date, out var items);
        items ??= new List<CalendarItem>();
        return new CalendarDay(date, isOutsideMonth, items);
    }

    private static CalendarMonth BuildMonth(DateOnly date, Dictionary<DateOnly, List<CalendarItem>> itemsByDate, bool isEnglish)
    {
        var days = new List<CalendarDay>();
        var first = new DateOnly(date.Year, date.Month, 1);
        var start = StartOfWeek(first);
        var monthName = isEnglish
            ? first.ToString("MMMM", CultureInfo.InvariantCulture)
            : first.ToString("MMMM", new CultureInfo("ru-RU"));

        for (var i = 0; i < 42; i++)
        {
            var current = start.AddDays(i);
            var outside = current.Month != date.Month;
            days.Add(BuildDay(current, itemsByDate, outside));
        }

        return new CalendarMonth($"{monthName} {date.Year}", days);
    }

    private static DateOnly StartOfWeek(DateOnly date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var diff = dayOfWeek == 0 ? -6 : 1 - dayOfWeek;
        return date.AddDays(diff);
    }

    private static string GetColorClass(DateOnly planned, DateOnly today)
    {
        if (planned < today)
        {
            return "red";
        }

        var days = planned.DayNumber - today.DayNumber;
        return days <= 5 ? "yellow" : "green";
    }
}

public sealed record MachineOption(int Id, string Name);

public sealed record CalendarItem(string ShortName, string ColorClass, string Tooltip);

public sealed class CalendarDay
{
    public DateOnly Date { get; }
    public bool IsOutsideMonth { get; }
    public List<CalendarItem> Items { get; }

    public CalendarDay(DateOnly date, bool isOutsideMonth, List<CalendarItem> items)
    {
        Date = date;
        IsOutsideMonth = isOutsideMonth;
        Items = items;
    }
}

public sealed class CalendarMonth
{
    public string Title { get; }
    public List<CalendarDay> Days { get; }

    public CalendarMonth(string title, List<CalendarDay> days)
    {
        Title = title;
        Days = days;
    }
}
