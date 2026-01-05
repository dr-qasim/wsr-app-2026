using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using VendingService.API.Data;
using VendingService.API.Models;

namespace VendingService.API.Pages.Web;

public sealed class MachinesModel : PageModel
{
    private static readonly string[] RequiredKeys =
    [
        "Name",
        "Manufacturer",
        "Model",
        "InventoryNumber",
        "SerialNumber",
        "Address",
        "Place"
    ];

    private static readonly Dictionary<string, string> HeaderMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Name"] = "Name",
        ["Название"] = "Name",
        ["Наименование"] = "Name",

        ["Manufacturer"] = "Manufacturer",
        ["Производитель"] = "Manufacturer",
        ["Фирма-изготовитель"] = "Manufacturer",

        ["Model"] = "Model",
        ["Модель"] = "Model",

        ["InventoryNumber"] = "InventoryNumber",
        ["Инвентарный номер"] = "InventoryNumber",
        ["Инвентарный номер ТА"] = "InventoryNumber",

        ["SerialNumber"] = "SerialNumber",
        ["Серийный номер"] = "SerialNumber",

        ["Address"] = "Address",
        ["Адрес"] = "Address",

        ["Place"] = "Place",
        ["Место"] = "Place",

        ["Company"] = "Company",
        ["Компания"] = "Company"
    };

    private readonly VendingServiceDbContext _db;

    public bool IsEnglish => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";

    [BindProperty]
    public IFormFile? Upload { get; set; }

    public List<string> Errors { get; } = new();
    public List<string> Messages { get; } = new();

    public MachinesModel(VendingServiceDbContext db)
    {
        _db = db;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostUploadAsync()
    {
        if (Upload is null || Upload.Length == 0)
        {
            Errors.Add(IsEnglish ? "File is not selected." : "Файл не выбран.");
            return Page();
        }

        var extension = Path.GetExtension(Upload.FileName).ToLowerInvariant();
        if (extension != ".csv" && extension != ".xlsx")
        {
            Errors.Add(IsEnglish ? "Only .csv or .xlsx files are supported." : "Поддерживаются только файлы .csv или .xlsx.");
            return Page();
        }

        UploadTable table;
        await using (var stream = Upload.OpenReadStream())
        {
            table = extension == ".csv" ? ReadCsv(stream) : ReadXlsx(stream);
        }

        if (table.Headers.Count == 0 || table.Rows.Count == 0)
        {
            Errors.Add(IsEnglish ? "The file does not contain data." : "Файл не содержит данных.");
            return Page();
        }

        var columnMap = MapHeaders(table.Headers);
        if (columnMap.Errors.Count > 0)
        {
            Errors.AddRange(columnMap.Errors);
            return Page();
        }

        var existingInventory = await _db.VendingMachines
            .AsNoTracking()
            .Select(x => x.InventoryNumber)
            .ToListAsync();
        var existingSerials = await _db.VendingMachines
            .AsNoTracking()
            .Select(x => x.SerialNumber)
            .ToListAsync();

        var inventorySet = new HashSet<string>(existingInventory, StringComparer.OrdinalIgnoreCase);
        var serialSet = new HashSet<string>(existingSerials, StringComparer.OrdinalIgnoreCase);
        var inventoryFileSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var serialFileSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var manufacturers = await _db.VendingMachineManufacturers.AsNoTracking().ToListAsync();
        var manufacturerByName = manufacturers.ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);

        var models = await _db.VendingMachineModels.AsNoTracking().ToListAsync();

        var companies = await _db.Companies.AsNoTracking().ToListAsync();
        var companyByName = companies.ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);

        var defaults = await LoadDefaultsAsync();
        if (defaults.Errors.Count > 0)
        {
            Errors.AddRange(defaults.Errors);
            return Page();
        }

        var toInsert = new List<UploadMachineRow>();

        for (var i = 0; i < table.Rows.Count; i++)
        {
            var rowNumber = i + 2;
            var row = table.Rows[i];

            var item = ReadRow(row, columnMap.ColumnIndexes, rowNumber, IsEnglish);
            if (item.Errors.Count > 0)
            {
                Errors.AddRange(item.Errors);
                continue;
            }

            if (!inventoryFileSet.Add(item.InventoryNumber!))
            {
                Errors.Add(FormatRowError(rowNumber, IsEnglish ? "Duplicate InventoryNumber in file." : "Дублируется инвентарный номер в файле.", IsEnglish));
                continue;
            }

            if (!serialFileSet.Add(item.SerialNumber!))
            {
                Errors.Add(FormatRowError(rowNumber, IsEnglish ? "Duplicate SerialNumber in file." : "Дублируется серийный номер в файле.", IsEnglish));
                continue;
            }

            if (inventorySet.Contains(item.InventoryNumber!))
            {
                Errors.Add(FormatRowError(rowNumber, IsEnglish ? "InventoryNumber already exists in DB." : "Инвентарный номер уже существует в БД.", IsEnglish));
                continue;
            }

            if (serialSet.Contains(item.SerialNumber!))
            {
                Errors.Add(FormatRowError(rowNumber, IsEnglish ? "SerialNumber already exists in DB." : "Серийный номер уже существует в БД.", IsEnglish));
                continue;
            }

            if (!manufacturerByName.TryGetValue(item.Manufacturer!, out var manufacturer))
            {
                Errors.Add(FormatRowError(rowNumber, IsEnglish ? "Manufacturer not found." : "Производитель не найден.", IsEnglish));
                continue;
            }

            var model = models.FirstOrDefault(x =>
                x.VendingMachineManufacturerId == manufacturer.VendingMachineManufacturerId
                && string.Equals(x.Name, item.Model, StringComparison.OrdinalIgnoreCase));

            if (model is null)
            {
                Errors.Add(FormatRowError(rowNumber, IsEnglish ? "Model not found for manufacturer." : "Модель не найдена у производителя.", IsEnglish));
                continue;
            }

            int? companyId = null;
            if (!string.IsNullOrWhiteSpace(item.Company))
            {
                if (companyByName.TryGetValue(item.Company, out var company))
                {
                    companyId = company.CompanyId;
                }
                else
                {
                    Errors.Add(FormatRowError(rowNumber, IsEnglish ? "Company not found." : "Компания не найдена.", IsEnglish));
                    continue;
                }
            }

            toInsert.Add(item with
            {
                ManufacturerId = manufacturer.VendingMachineManufacturerId,
                ModelId = model.VendingMachineModelId,
                CompanyId = companyId
            });
        }

        if (Errors.Count > 0)
        {
            return Page();
        }

        var now = DateTime.Now;
        var manufactureDate = DateOnly.FromDateTime(now.Date.AddDays(-7));
        var commissioningDate = DateOnly.FromDateTime(now.Date);

        foreach (var item in toInsert)
        {
            var entity = new VendingMachine
            {
                Name = item.Name!,
                VendingMachineModelId = item.ModelId!.Value,
                WorkModeId = defaults.WorkModeId,
                TimeZoneId = defaults.TimeZoneId,
                VendingMachineStatusId = defaults.StatusId,
                ServicePriorityId = defaults.ServicePriorityId,
                ProductMatrixId = defaults.ProductMatrixId,
                CompanyId = item.CompanyId,
                ModemId = null,
                Address = item.Address!,
                Place = item.Place!,
                Latitude = null,
                Longitude = null,
                InventoryNumber = item.InventoryNumber!,
                SerialNumber = item.SerialNumber!,
                ManufactureDate = manufactureDate,
                CommissioningDate = commissioningDate,
                LastVerificationDate = null,
                VerificationIntervalMonths = null,
                ResourceHours = null,
                NextServiceDate = null,
                ServiceDurationHours = 2,
                InventoryDate = null,
                CountryId = defaults.CountryId,
                LastVerificationUserAccountId = null,
                WorkingTimeFrom = null,
                WorkingTimeTo = null,
                CriticalValuesTemplateId = defaults.CriticalValuesTemplateId,
                NotificationTemplateId = defaults.NotificationTemplateId,
                ManagerUserAccountId = null,
                EngineerUserAccountId = null,
                TechnicianOperatorUserAccountId = null,
                KitOnlineCashRegisterId = null,
                Notes = null,
                CreatedAt = now,
                PaymentSystems = defaults.PaymentSystemIds
                    .Select(id => new VendingMachinePaymentSystem { PaymentSystemId = id })
                    .ToList()
            };

            _db.VendingMachines.Add(entity);
        }

        await _db.SaveChangesAsync();

        Messages.Add(IsEnglish
            ? $"Imported machines: {toInsert.Count}."
            : $"Импортировано торговых автоматов: {toInsert.Count}.");

        return Page();
    }

    private static UploadMachineRow ReadRow(
        string[] row,
        Dictionary<string, int> indexes,
        int rowNumber,
        bool isEnglish)
    {
        var item = new UploadMachineRow
        {
            RowNumber = rowNumber,
            Name = GetValue(row, indexes, "Name"),
            Manufacturer = GetValue(row, indexes, "Manufacturer"),
            Model = GetValue(row, indexes, "Model"),
            InventoryNumber = GetValue(row, indexes, "InventoryNumber"),
            SerialNumber = GetValue(row, indexes, "SerialNumber"),
            Address = GetValue(row, indexes, "Address"),
            Place = GetValue(row, indexes, "Place"),
            Company = GetValue(row, indexes, "Company")
        };

        foreach (var key in RequiredKeys)
        {
            var value = GetValue(row, indexes, key);
            if (string.IsNullOrWhiteSpace(value))
            {
                item.Errors.Add(FormatRowError(rowNumber, $"Missing {key}.", isEnglish));
            }
        }

        return item;
    }

    private static string? GetValue(string[] row, Dictionary<string, int> indexes, string key)
    {
        if (!indexes.TryGetValue(key, out var index))
        {
            return null;
        }

        if (index < 0 || index >= row.Length)
        {
            return null;
        }

        var value = row[index]?.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string FormatRowError(int rowNumber, string message, bool isEnglish)
        => isEnglish ? $"Row {rowNumber}: {message}" : $"Строка {rowNumber}: {message}";

    private HeaderMapResult MapHeaders(IReadOnlyList<string> headers)
    {
        var result = new HeaderMapResult();

        for (var i = 0; i < headers.Count; i++)
        {
            var raw = headers[i]?.Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            if (HeaderMap.TryGetValue(raw, out var key))
            {
                result.ColumnIndexes[key] = i;
            }
        }

        foreach (var key in RequiredKeys)
        {
            if (!result.ColumnIndexes.ContainsKey(key))
            {
                result.Errors.Add(IsEnglish
                    ? $"Missing column: {key}."
                    : $"Отсутствует столбец: {key}.");
            }
        }

        if (result.ColumnIndexes.Count < 6)
        {
            result.Errors.Add(IsEnglish
                ? "At least 6 columns are required."
                : "Необходимо минимум 6 столбцов.");
        }

        return result;
    }

    private async Task<DefaultsResult> LoadDefaultsAsync()
    {
        var result = new DefaultsResult();

        result.WorkModeId = await _db.WorkModes.AsNoTracking().Select(x => x.WorkModeId).FirstOrDefaultAsync();
        result.TimeZoneId = await _db.TimeZones.AsNoTracking().Select(x => x.TimeZoneId).FirstOrDefaultAsync();
        result.StatusId = await _db.VendingMachineStatuses.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .Select(x => x.VendingMachineStatusId)
            .FirstOrDefaultAsync();
        result.ServicePriorityId = await _db.ServicePriorities.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .Select(x => x.ServicePriorityId)
            .FirstOrDefaultAsync();
        result.ProductMatrixId = await _db.ProductMatrices.AsNoTracking().Select(x => x.ProductMatrixId).FirstOrDefaultAsync();
        result.CountryId = await _db.Countries.AsNoTracking().Select(x => x.CountryId).FirstOrDefaultAsync();
        result.CriticalValuesTemplateId = await _db.CriticalValuesTemplates.AsNoTracking().Select(x => x.CriticalValuesTemplateId).FirstOrDefaultAsync();
        result.NotificationTemplateId = await _db.NotificationTemplates.AsNoTracking().Select(x => x.NotificationTemplateId).FirstOrDefaultAsync();
        result.PaymentSystemIds = await _db.PaymentSystems.AsNoTracking()
            .Select(x => x.PaymentSystemId)
            .ToListAsync();

        if (result.WorkModeId == 0 || result.TimeZoneId == 0 || result.StatusId == 0
            || result.ServicePriorityId == 0 || result.ProductMatrixId == 0 || result.CountryId == 0
            || result.CriticalValuesTemplateId == 0 || result.NotificationTemplateId == 0
            || result.PaymentSystemIds.Count == 0)
        {
            result.Errors.Add(IsEnglish
                ? "Reference data is missing. Import aborted."
                : "Не хватает справочных данных. Импорт прерван.");
        }

        return result;
    }

    private static UploadTable ReadCsv(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var lines = new List<string[]>();
        string? line;
        char delimiter = ';';
        var first = reader.ReadLine();
        if (first is null)
        {
            return new UploadTable();
        }

        delimiter = first.Contains(';') ? ';' : ',';
        lines.Add(ParseCsvLine(first, delimiter));

        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            lines.Add(ParseCsvLine(line, delimiter));
        }

        return UploadTable.From(lines);
    }

    private static UploadTable ReadXlsx(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var sharedStrings = LoadSharedStrings(archive);
        var sheetEntry = archive.Entries
            .Where(x => x.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.FullName)
            .FirstOrDefault();

        if (sheetEntry is null)
        {
            return new UploadTable();
        }

        using var sheetStream = sheetEntry.Open();
        var doc = XDocument.Load(sheetStream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        var rows = new List<string[]>();
        foreach (var row in doc.Descendants(ns + "row"))
        {
            var cells = row.Elements(ns + "c");
            var values = new Dictionary<int, string>();
            var maxCol = -1;

            foreach (var cell in cells)
            {
                var cellRef = cell.Attribute("r")?.Value;
                if (string.IsNullOrWhiteSpace(cellRef))
                {
                    continue;
                }

                var colIndex = ColumnToIndex(cellRef);
                maxCol = Math.Max(maxCol, colIndex);

                var type = cell.Attribute("t")?.Value;
                var value = string.Empty;
                if (type == "s")
                {
                    var idxText = cell.Element(ns + "v")?.Value;
                    if (int.TryParse(idxText, out var idx) && idx >= 0 && idx < sharedStrings.Count)
                    {
                        value = sharedStrings[idx];
                    }
                }
                else if (type == "inlineStr")
                {
                    value = cell.Element(ns + "is")?.Element(ns + "t")?.Value ?? string.Empty;
                }
                else
                {
                    value = cell.Element(ns + "v")?.Value ?? string.Empty;
                }

                values[colIndex] = value;
            }

            if (maxCol < 0)
            {
                continue;
            }

            var rowValues = new string[maxCol + 1];
            foreach (var entry in values)
            {
                rowValues[entry.Key] = entry.Value;
            }

            rows.Add(rowValues);
        }

        return UploadTable.From(rows);
    }

    private static List<string> LoadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return [];
        }

        using var stream = entry.Open();
        var doc = XDocument.Load(stream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return doc.Descendants(ns + "si")
            .Select(si => string.Concat(si.Descendants(ns + "t").Select(t => t.Value)))
            .ToList();
    }

    private static int ColumnToIndex(string cellRef)
    {
        var letters = new string(cellRef.TakeWhile(char.IsLetter).ToArray());
        var sum = 0;
        foreach (var ch in letters)
        {
            sum = sum * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
        }

        return sum - 1;
    }

    private static string[] ParseCsvLine(string line, char delimiter)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];

            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (ch == delimiter && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
                continue;
            }

            sb.Append(ch);
        }

        result.Add(sb.ToString());
        return result.ToArray();
    }

    private sealed class UploadTable
    {
        public List<string> Headers { get; } = new();
        public List<string[]> Rows { get; } = new();

        public static UploadTable From(IReadOnlyList<string[]> rows)
        {
            var table = new UploadTable();
            if (rows.Count == 0)
            {
                return table;
            }

            table.Headers.AddRange(rows[0]);
            for (var i = 1; i < rows.Count; i++)
            {
                table.Rows.Add(rows[i]);
            }

            return table;
        }
    }

    private sealed class HeaderMapResult
    {
        public Dictionary<string, int> ColumnIndexes { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Errors { get; } = new();
    }

    private sealed record UploadMachineRow
    {
        public int RowNumber { get; init; }
        public string? Name { get; init; }
        public string? Manufacturer { get; init; }
        public string? Model { get; init; }
        public string? InventoryNumber { get; init; }
        public string? SerialNumber { get; init; }
        public string? Address { get; init; }
        public string? Place { get; init; }
        public string? Company { get; init; }
        public int? ManufacturerId { get; init; }
        public int? ModelId { get; init; }
        public int? CompanyId { get; init; }
        public List<string> Errors { get; } = new();
    }

    private sealed class DefaultsResult
    {
        public int WorkModeId { get; set; }
        public int TimeZoneId { get; set; }
        public int StatusId { get; set; }
        public int ServicePriorityId { get; set; }
        public int ProductMatrixId { get; set; }
        public int CountryId { get; set; }
        public int CriticalValuesTemplateId { get; set; }
        public int NotificationTemplateId { get; set; }
        public List<int> PaymentSystemIds { get; set; } = new();
        public List<string> Errors { get; } = new();
    }
}
