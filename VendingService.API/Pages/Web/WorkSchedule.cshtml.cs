using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VendingService.API.Data;
using VendingService.API.Models;

namespace VendingService.API.Pages.Web;

public enum WorkScheduleViewMode
{
    Day,
    Week
}

public sealed class WorkScheduleModel : PageModel
{
    private readonly VendingServiceDbContext _db;

    public bool IsEnglish => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";

    [BindProperty(SupportsGet = true)]
    public string? ViewMode { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateOnly? Date { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? UserId { get; set; }

    public WorkScheduleViewMode View { get; private set; } = WorkScheduleViewMode.Day;
    public DateOnly ReferenceDate { get; private set; }
    public int? SelectedUserId { get; private set; }

    public string ViewString => View == WorkScheduleViewMode.Week ? "week" : "day";

    public List<EmployeeOption> FilterEmployees { get; } = new();
    public List<EmployeeSchedule> EmployeeSchedules { get; } = new();
    public List<WeekDay> WeekDays { get; } = new();
    public List<WeekEmployeeSchedule> WeekSchedules { get; } = new();
    public List<string> Messages { get; } = new();
    public List<string> Warnings { get; } = new();

    public WorkScheduleModel(VendingServiceDbContext db)
    {
        _db = db;
    }

    public async Task OnGetAsync()
    {
        InitializeContext();
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostGenerateAsync()
    {
        InitializeContext();
        await GenerateRequestsAsync(ReferenceDate);
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAutoAssignAsync()
    {
        InitializeContext();
        await AutoAssignAsync();
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostApplyAsync()
    {
        InitializeContext();
        await ApplyScheduleAsync(ReferenceDate);
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostMoveAsync([FromBody] MoveRequest request)
    {
        if (request is null || request.ServiceRequestId <= 0)
        {
            return BadRequest();
        }

        var entity = await _db.ServiceRequests.SingleOrDefaultAsync(x => x.ServiceRequestId == request.ServiceRequestId);
        if (entity is null)
        {
            return NotFound();
        }

        int? userId = request.UserId <= 0 ? null : request.UserId;
        if (userId.HasValue)
        {
            var exists = await _db.UserAccounts.AnyAsync(x => x.UserAccountId == userId.Value);
            if (!exists)
            {
                return BadRequest();
            }
        }

        entity.AssignedUserAccountId = userId;
        entity.PlannedDate = request.Date;
        entity.SortOrder = userId.HasValue
            ? await GetNextSortOrderAsync(request.Date, userId)
            : null;
        await _db.SaveChangesAsync();

        return new JsonResult(new { ok = true });
    }

    private void InitializeContext()
    {
        View = ViewMode?.ToLowerInvariant() == "week" ? WorkScheduleViewMode.Week : WorkScheduleViewMode.Day;
        ReferenceDate = Date ?? DateOnly.FromDateTime(DateTime.Today);
        SelectedUserId = UserId;
    }

    private async Task LoadAsync()
    {
        var employees = await LoadEmployeesAsync(includeUnassigned: true);
        FilterEmployees.Clear();
        FilterEmployees.AddRange(employees.Where(x => !x.IsVirtual));

        var start = View == WorkScheduleViewMode.Week ? StartOfWeek(ReferenceDate) : ReferenceDate;
        var end = View == WorkScheduleViewMode.Week ? start.AddDays(6) : ReferenceDate;

        var statusIds = await GetActiveStatusIdsAsync();
        var requests = await _db.ServiceRequests
            .AsNoTracking()
            .Include(x => x.VendingMachine)
            .ThenInclude(x => x!.VendingMachineModel)
            .Include(x => x.ServiceRequestType)
            .Include(x => x.ServiceRequestStatus)
            .Where(x => x.PlannedDate >= start && x.PlannedDate <= end)
            .Where(x => statusIds.Contains(x.ServiceRequestStatusId))
            .ToListAsync();

        if (View == WorkScheduleViewMode.Day)
        {
            EmployeeSchedules.Clear();
            foreach (var employee in employees)
            {
                if (SelectedUserId.HasValue && employee.Id != SelectedUserId.Value && !employee.IsVirtual)
                {
                    continue;
                }

                var tasks = requests
                    .Where(x => (employee.IsVirtual && x.AssignedUserAccountId == null)
                                || (!employee.IsVirtual && x.AssignedUserAccountId == employee.Id))
                    .Where(x => x.PlannedDate == ReferenceDate)
                    .OrderBy(x => x.SortOrder ?? int.MaxValue)
                    .ThenBy(x => x.ServiceRequestId)
                    .Select(CreateTask)
                    .ToList();

                ApplyTimeRanges(tasks, new TimeOnly(9, 0));
                EmployeeSchedules.Add(new EmployeeSchedule(employee, tasks));
            }

            return;
        }

        WeekDays.Clear();
        for (var i = 0; i < 7; i++)
        {
            var day = start.AddDays(i);
            WeekDays.Add(new WeekDay(day, day.ToString("dd.MM")));
        }

        WeekSchedules.Clear();
        foreach (var employee in employees)
        {
            if (SelectedUserId.HasValue && employee.Id != SelectedUserId.Value && !employee.IsVirtual)
            {
                continue;
            }

            var dayTasks = new List<WeekDayTasks>();
            foreach (var day in WeekDays)
            {
                var tasks = requests
                    .Where(x => (employee.IsVirtual && x.AssignedUserAccountId == null)
                                || (!employee.IsVirtual && x.AssignedUserAccountId == employee.Id))
                    .Where(x => x.PlannedDate == day.Date)
                    .OrderBy(x => x.SortOrder ?? int.MaxValue)
                    .ThenBy(x => x.ServiceRequestId)
                    .Select(CreateTask)
                    .ToList();

                dayTasks.Add(new WeekDayTasks(day.Date, tasks));
            }

            WeekSchedules.Add(new WeekEmployeeSchedule(employee, dayTasks));
        }
    }

    private ScheduleTask CreateTask(ServiceRequest request)
    {
        var hours = CalculateTaskHours(request);
        var typeName = request.ServiceRequestType?.Name ?? "-";
        var statusName = request.ServiceRequestStatus?.Name ?? string.Empty;
        var isEmergency = statusName.Contains("Авар", StringComparison.OrdinalIgnoreCase)
                          || typeName.Contains("Авар", StringComparison.OrdinalIgnoreCase);

        var modelName = request.VendingMachine?.VendingMachineModel?.Name ?? "-";
        var tooltip = $"{request.VendingMachine?.Name} | {modelName} | {typeName}";

        return new ScheduleTask
        {
            ServiceRequestId = request.ServiceRequestId,
            VendingMachineName = request.VendingMachine?.Name ?? "-",
            ServiceTypeName = typeName,
            PlannedDate = request.PlannedDate,
            AssignedUserId = request.AssignedUserAccountId,
            SortOrder = request.SortOrder,
            DurationHours = hours,
            IsEmergency = isEmergency,
            Tooltip = tooltip,
            ShortInfo = $"{typeName}, {hours:0.#} ч"
        };
    }

    private static void ApplyTimeRanges(List<ScheduleTask> tasks, TimeOnly start)
    {
        var current = start;
        foreach (var task in tasks)
        {
            var end = current.AddHours(task.DurationHours);
            task.TimeRange = $"{current:HH\\:mm} - {end:HH\\:mm}";
            current = end;
        }
    }

    private async Task<List<EmployeeOption>> LoadEmployeesAsync(bool includeUnassigned)
    {
        var employees = await _db.UserAccounts
            .AsNoTracking()
            .Include(x => x.UserRole)
            .Where(x => x.UserRole != null && (x.UserRole.Name == "Инженер" || x.UserRole.Name == "Техник-оператор"))
            .OrderBy(x => x.LastName)
            .Select(x => new EmployeeOption(x.UserAccountId,
                string.Join(" ", new[] { x.LastName, x.FirstName }.Where(s => !string.IsNullOrWhiteSpace(s))),
                x.UserRole!.Name,
                false))
            .ToListAsync();

        if (includeUnassigned)
        {
            employees.Insert(0, new EmployeeOption(0, IsEnglish ? "Unassigned" : "Не назначено", string.Empty, true));
        }

        return employees;
    }

    private async Task AutoAssignAsync()
    {
        var start = View == WorkScheduleViewMode.Week ? StartOfWeek(ReferenceDate) : ReferenceDate;
        var end = View == WorkScheduleViewMode.Week ? start.AddDays(6) : ReferenceDate;
        var employees = await LoadEmployeesAsync(includeUnassigned: false);
        if (employees.Count == 0)
        {
            Warnings.Add(IsEnglish ? "No available employees." : "Нет доступных сотрудников.");
            return;
        }

        var skills = await _db.UserAccountVendingMachineModels
            .AsNoTracking()
            .GroupBy(x => x.UserAccountId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(x => x.VendingMachineModelId).ToHashSet());

        var statusIds = await GetActiveStatusIdsAsync();
        var newStatusId = await GetStatusIdAsync("Новая");
        var emergencyStatusId = await GetStatusIdAsync("Авария");

        var assignedRequests = await _db.ServiceRequests
            .Include(x => x.VendingMachine)
            .Where(x => x.PlannedDate >= start && x.PlannedDate <= end)
            .Where(x => x.AssignedUserAccountId != null)
            .Where(x => statusIds.Contains(x.ServiceRequestStatusId))
            .ToListAsync();

        var dayLoad = new Dictionary<(int userId, DateOnly date), double>();
        var weekLoad = new Dictionary<int, double>();
        var nextOrder = new Dictionary<(int userId, DateOnly date), int>();

        foreach (var req in assignedRequests)
        {
            var userId = req.AssignedUserAccountId!.Value;
            var hours = CalculateTaskHours(req);
            var key = (userId, req.PlannedDate);
            dayLoad[key] = dayLoad.GetValueOrDefault(key) + hours;
            weekLoad[userId] = weekLoad.GetValueOrDefault(userId) + hours;

            var order = req.SortOrder ?? 0;
            if (!nextOrder.ContainsKey(key) || order > nextOrder[key])
            {
                nextOrder[key] = order;
            }
        }

        var toAssign = await _db.ServiceRequests
            .Include(x => x.VendingMachine)
            .Include(x => x.ServiceRequestType)
            .Where(x => x.PlannedDate >= start && x.PlannedDate <= end)
            .Where(x => x.AssignedUserAccountId == null)
            .Where(x => x.ServiceRequestStatusId == newStatusId || (emergencyStatusId != 0 && x.ServiceRequestStatusId == emergencyStatusId))
            .ToListAsync();

        var ordered = toAssign
            .OrderByDescending(x => IsEmergency(x, emergencyStatusId))
            .ThenBy(x => x.PlannedDate)
            .ThenBy(x => x.ServiceRequestId)
            .ToList();

        foreach (var req in ordered)
        {
            var planned = req.PlannedDate;
            var hours = CalculateTaskHours(req);
            var modelId = req.VendingMachine?.VendingMachineModelId ?? 0;
            var isEmergency = IsEmergency(req, emergencyStatusId);

            var assigned = TryAssign(employees, skills, dayLoad, weekLoad, nextOrder, planned, end, modelId, hours, isEmergency, out var userId, out var assignedDate);

            if (!assigned)
            {
                Warnings.Add(IsEnglish
                    ? $"No available employees for request #{req.ServiceRequestId}."
                    : $"Нет доступных сотрудников для заявки #{req.ServiceRequestId}.");
                continue;
            }

            req.AssignedUserAccountId = userId;
            req.PlannedDate = assignedDate;
            var key = (userId, assignedDate);
            var order = nextOrder.GetValueOrDefault(key);
            order += 1;
            nextOrder[key] = order;
            req.SortOrder = order;
        }

        await _db.SaveChangesAsync();
        await FixOverloadsAsync(emergencyStatusId);

        Messages.Add(IsEnglish ? "Auto-assignment completed." : "Автораспределение выполнено.");
    }

    private async Task FixOverloadsAsync(int emergencyStatusId)
    {
        var start = View == WorkScheduleViewMode.Week ? StartOfWeek(ReferenceDate) : ReferenceDate;
        var end = View == WorkScheduleViewMode.Week ? start.AddDays(6) : ReferenceDate;

        var requests = await _db.ServiceRequests
            .Include(x => x.VendingMachine)
            .Include(x => x.ServiceRequestType)
            .Where(x => x.PlannedDate >= start && x.PlannedDate <= end)
            .Where(x => x.AssignedUserAccountId != null)
            .ToListAsync();

        var grouped = requests.GroupBy(x => new { x.AssignedUserAccountId, x.PlannedDate });

        foreach (var group in grouped)
        {
            var total = group.Sum(CalculateTaskHours);
            if (total <= 10)
            {
                continue;
            }

            var list = group
                .OrderByDescending(x => x.SortOrder ?? 0)
                .ToList();

            foreach (var req in list)
            {
                if (total <= 10)
                {
                    break;
                }

                if (IsEmergency(req, emergencyStatusId))
                {
                    continue;
                }

                req.AssignedUserAccountId = null;
                req.SortOrder = null;
                req.PlannedDate = req.PlannedDate.AddDays(1);
                total -= CalculateTaskHours(req);

                Warnings.Add(IsEnglish
                    ? $"Request #{req.ServiceRequestId} moved to next day due to overload."
                    : $"Заявка #{req.ServiceRequestId} перенесена на следующий день из-за перегрузки.");
            }
        }

        await _db.SaveChangesAsync();
    }

    private bool TryAssign(
        IReadOnlyList<EmployeeOption> employees,
        Dictionary<int, HashSet<int>> skills,
        Dictionary<(int userId, DateOnly date), double> dayLoad,
        Dictionary<int, double> weekLoad,
        Dictionary<(int userId, DateOnly date), int> nextOrder,
        DateOnly planned,
        DateOnly end,
        int modelId,
        double hours,
        bool isEmergency,
        out int userId,
        out DateOnly assignedDate)
    {
        assignedDate = planned;
        var date = planned;
        var attempts = View == WorkScheduleViewMode.Week ? 7 : 3;

        for (var i = 0; i < attempts; i++)
        {
            var candidates = employees
                .Where(x => IsEligible(x.Id, modelId, skills))
                .Select(x => new
                {
                    Employee = x,
                    DayHours = dayLoad.GetValueOrDefault((x.Id, date)),
                    WeekHours = weekLoad.GetValueOrDefault(x.Id)
                })
                .Where(x => x.DayHours + hours <= 10 && (View != WorkScheduleViewMode.Week || x.WeekHours + hours <= 40))
                .OrderBy(x => x.DayHours)
                .ToList();

            if (candidates.Count > 0)
            {
                var chosen = candidates[0].Employee;
                userId = chosen.Id;
                assignedDate = date;
                dayLoad[(userId, date)] = candidates[0].DayHours + hours;
                weekLoad[userId] = candidates[0].WeekHours + hours;
                return true;
            }

            date = date.AddDays(1);
            if (View == WorkScheduleViewMode.Week && date > end)
            {
                break;
            }
        }

        if (isEmergency)
        {
            var fallback = employees
                .Where(x => IsEligible(x.Id, modelId, skills))
                .Select(x => new
                {
                    Employee = x,
                    DayHours = dayLoad.GetValueOrDefault((x.Id, planned))
                })
                .OrderBy(x => x.DayHours)
                .FirstOrDefault();

            if (fallback is not null)
            {
                userId = fallback.Employee.Id;
                assignedDate = planned;
                dayLoad[(userId, planned)] = fallback.DayHours + hours;
                weekLoad[userId] = weekLoad.GetValueOrDefault(userId) + hours;
                Warnings.Add(IsEnglish
                    ? $"Emergency request assigned with overload to {fallback.Employee.Name}."
                    : $"Аварийная заявка назначена с перегрузкой на {fallback.Employee.Name}.");
                return true;
            }
        }

        userId = 0;
        assignedDate = planned;
        return false;
    }

    private static bool IsEligible(int userId, int modelId, Dictionary<int, HashSet<int>> skills)
        => skills.TryGetValue(userId, out var set) && set.Contains(modelId);

    private static bool IsEmergency(ServiceRequest req, int emergencyStatusId)
    {
        if (emergencyStatusId != 0 && req.ServiceRequestStatusId == emergencyStatusId)
        {
            return true;
        }

        var typeName = req.ServiceRequestType?.Name;
        return typeName != null && typeName.Contains("Авар", StringComparison.OrdinalIgnoreCase);
    }

    private static double CalculateTaskHours(ServiceRequest req)
    {
        var serviceHours = req.VendingMachine?.ServiceDurationHours ?? 2;
        return serviceHours + 2;
    }

    private async Task ApplyScheduleAsync(DateOnly date)
    {
        var newStatusId = await GetStatusIdAsync("Новая");
        var inProgressStatusId = await GetStatusIdAsync("В работе");
        var serviceStatusId = await GetMachineStatusIdAsync("В ремонте/на обслуживании");

        if (newStatusId == 0 || inProgressStatusId == 0 || serviceStatusId == 0)
        {
            Warnings.Add(IsEnglish
                ? "Missing lookup data for status update."
                : "Не хватает справочников для смены статуса.");
            return;
        }

        var requests = await _db.ServiceRequests
            .Where(x => x.PlannedDate == date && x.ServiceRequestStatusId == newStatusId)
            .ToListAsync();

        if (requests.Count == 0)
        {
            Messages.Add(IsEnglish ? "No requests to apply." : "Нет заявок для применения.");
            return;
        }

        var now = DateTime.Now;
        foreach (var req in requests)
        {
            req.ServiceRequestStatusId = inProgressStatusId;
            _db.ServiceRequestStatusHistories.Add(new ServiceRequestStatusHistory
            {
                ServiceRequestId = req.ServiceRequestId,
                ServiceRequestStatusId = inProgressStatusId,
                ChangedAt = now,
                ChangedByUserAccountId = null
            });
        }

        var machineIds = requests.Select(x => x.VendingMachineId).Distinct().ToList();
        var machines = await _db.VendingMachines
            .Where(x => machineIds.Contains(x.VendingMachineId))
            .ToListAsync();

        foreach (var vm in machines)
        {
            if (vm.VendingMachineStatusId == serviceStatusId)
            {
                continue;
            }

            vm.VendingMachineStatusId = serviceStatusId;
            _db.VendingMachineStatusHistories.Add(new VendingMachineStatusHistory
            {
                VendingMachineId = vm.VendingMachineId,
                VendingMachineStatusId = serviceStatusId,
                ChangedAt = now,
                ChangedByUserAccountId = null
            });
        }

        await _db.SaveChangesAsync();
        Messages.Add(IsEnglish ? "Schedule applied." : "График применен.");
    }

    private async Task GenerateRequestsAsync(DateOnly targetDate)
    {
        var planTypeId = await GetTypeIdAsync("Плановое техническое обслуживание");
        var newStatusId = await GetStatusIdAsync("Новая");
        var inProgressStatusId = await GetStatusIdAsync("В работе");

        if (planTypeId == 0 || newStatusId == 0 || inProgressStatusId == 0)
        {
            Warnings.Add(IsEnglish
                ? "Missing lookup data for request generation."
                : "Не хватает справочников для генерации заявок.");
            return;
        }

        var openMachineIds = await _db.ServiceRequests
            .AsNoTracking()
            .Where(x => x.ServiceRequestStatusId == newStatusId || x.ServiceRequestStatusId == inProgressStatusId)
            .Select(x => x.VendingMachineId)
            .Distinct()
            .ToListAsync();

        var machines = await _db.VendingMachines
            .AsNoTracking()
            .ToListAsync();

        var dueMachines = machines
            .Select(x => new
            {
                x.VendingMachineId,
                Planned = GetPlannedDate(x)
            })
            .Where(x => x.Planned.HasValue && x.Planned.Value <= targetDate)
            .ToList();

        var toCreate = dueMachines
            .Where(x => !openMachineIds.Contains(x.VendingMachineId))
            .ToList();

        if (toCreate.Count == 0)
        {
            Messages.Add(IsEnglish ? "No new requests." : "Новых заявок нет.");
            return;
        }

        var now = DateTime.Now;
        var newRequests = new List<ServiceRequest>();

        foreach (var vm in toCreate)
        {
            var planned = vm.Planned ?? targetDate;
            var entity = new ServiceRequest
            {
                VendingMachineId = vm.VendingMachineId,
                ServiceRequestTypeId = planTypeId,
                ServiceRequestStatusId = newStatusId,
                PlannedDate = planned,
                AssignedUserAccountId = null,
                SortOrder = null,
                Notes = null,
                DeclineReason = null,
                CreatedAt = now
            };

            _db.ServiceRequests.Add(entity);
            newRequests.Add(entity);
        }

        await _db.SaveChangesAsync();

        foreach (var entity in newRequests)
        {
            _db.ServiceRequestStatusHistories.Add(new ServiceRequestStatusHistory
            {
                ServiceRequestId = entity.ServiceRequestId,
                ServiceRequestStatusId = newStatusId,
                ChangedAt = now,
                ChangedByUserAccountId = null
            });
        }

        await _db.SaveChangesAsync();
        Messages.Add(IsEnglish
            ? $"Generated: {toCreate.Count}."
            : $"Сформировано заявок: {toCreate.Count}.");
    }

    private static DateOnly? GetPlannedDate(VendingMachine machine)
    {
        var planned = machine.NextVerificationDate;
        if (!planned.HasValue && machine.LastVerificationDate.HasValue && machine.VerificationIntervalMonths.HasValue)
        {
            planned = machine.LastVerificationDate.Value.AddMonths(machine.VerificationIntervalMonths.Value);
        }

        if (machine.ResourceHours.HasValue)
        {
            var created = machine.CreatedAt.Date;
            var usedHours = (DateTime.Today - created).TotalDays * 8;
            var threshold = machine.ResourceHours.Value * 0.85;
            var remaining = Math.Max(0, threshold - usedHours);
            var daysLeft = remaining / 8;
            var resourceDue = DateOnly.FromDateTime(DateTime.Today.AddDays(daysLeft));

            if (!planned.HasValue || resourceDue < planned.Value)
            {
                planned = resourceDue;
            }
        }

        return planned;
    }

    private async Task<int> GetStatusIdAsync(string name)
        => await _db.ServiceRequestStatuses
            .AsNoTracking()
            .Where(x => x.Name == name)
            .Select(x => x.ServiceRequestStatusId)
            .FirstOrDefaultAsync();

    private async Task<int> GetTypeIdAsync(string name)
        => await _db.ServiceRequestTypes
            .AsNoTracking()
            .Where(x => x.Name == name)
            .Select(x => x.ServiceRequestTypeId)
            .FirstOrDefaultAsync();

    private async Task<int> GetMachineStatusIdAsync(string name)
        => await _db.VendingMachineStatuses
            .AsNoTracking()
            .Where(x => x.Name == name)
            .Select(x => x.VendingMachineStatusId)
            .FirstOrDefaultAsync();

    private async Task<int[]> GetActiveStatusIdsAsync()
    {
        var newStatusId = await GetStatusIdAsync("Новая");
        var inProgressStatusId = await GetStatusIdAsync("В работе");
        var emergencyStatusId = await GetStatusIdAsync("Авария");

        var ids = new List<int>();
        if (newStatusId != 0) ids.Add(newStatusId);
        if (inProgressStatusId != 0) ids.Add(inProgressStatusId);
        if (emergencyStatusId != 0) ids.Add(emergencyStatusId);
        return ids.ToArray();
    }

    private async Task<int> GetNextSortOrderAsync(DateOnly date, int? userId)
    {
        if (!userId.HasValue)
        {
            return 0;
        }

        var max = await _db.ServiceRequests
            .Where(x => x.PlannedDate == date && x.AssignedUserAccountId == userId.Value)
            .MaxAsync(x => (int?)x.SortOrder) ?? 0;

        return max + 1;
    }

    private static DateOnly StartOfWeek(DateOnly date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var diff = dayOfWeek == 0 ? -6 : 1 - dayOfWeek;
        return date.AddDays(diff);
    }
}

public sealed record EmployeeOption(int Id, string Name, string RoleName, bool IsVirtual);

public sealed class EmployeeSchedule
{
    public EmployeeOption Employee { get; }
    public List<ScheduleTask> Tasks { get; }

    public EmployeeSchedule(EmployeeOption employee, List<ScheduleTask> tasks)
    {
        Employee = employee;
        Tasks = tasks;
    }
}

public sealed record WeekDay(DateOnly Date, string Title);

public sealed class WeekEmployeeSchedule
{
    public EmployeeOption Employee { get; }
    public List<WeekDayTasks> Days { get; }

    public WeekEmployeeSchedule(EmployeeOption employee, List<WeekDayTasks> days)
    {
        Employee = employee;
        Days = days;
    }
}

public sealed record WeekDayTasks(DateOnly Date, List<ScheduleTask> Tasks);

public sealed class ScheduleTask
{
    public int ServiceRequestId { get; set; }
    public string VendingMachineName { get; set; } = string.Empty;
    public string ServiceTypeName { get; set; } = string.Empty;
    public DateOnly PlannedDate { get; set; }
    public int? AssignedUserId { get; set; }
    public int? SortOrder { get; set; }
    public double DurationHours { get; set; }
    public bool IsEmergency { get; set; }
    public string TimeRange { get; set; } = string.Empty;
    public string Tooltip { get; set; } = string.Empty;
    public string ShortInfo { get; set; } = string.Empty;
}

public sealed class MoveRequest
{
    public int ServiceRequestId { get; set; }
    public int UserId { get; set; }
    public DateOnly Date { get; set; }
}
