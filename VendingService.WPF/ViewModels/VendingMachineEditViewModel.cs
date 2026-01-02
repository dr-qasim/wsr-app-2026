using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using VendingService.WPF.Contracts.Lookups;
using VendingService.WPF.Contracts.VendingMachines;
using VendingService.WPF.Services.Api;

namespace VendingService.WPF.ViewModels;

public sealed class VendingMachineEditViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;
    private readonly int? _vendingMachineId;

    private bool _isLoading;
    private bool _isSaving;
    private string? _errorMessage;

    private string _name = string.Empty;
    private string _address = string.Empty;
    private string _place = string.Empty;
    private string _inventoryNumber = string.Empty;
    private string _serialNumber = string.Empty;
    private string? _latitudeText;
    private string? _longitudeText;
    private string? _notes;

    private int _manufacturerId;
    private int _modelId;
    private int _workModeId;
    private int _timeZoneId;
    private int _statusId;
    private int _servicePriorityId;
    private int _productMatrixId;
    private int _countryId;
    private int? _companyId;
    private int? _modemId;
    private int? _criticalValuesTemplateId;
    private int? _notificationTemplateId;

    private DateTime _manufactureDate = DateTime.Today;
    private DateTime _commissioningDate = DateTime.Today;

    public event EventHandler? Saved;

    public string Title => _vendingMachineId is null ? "Создание торгового автомата" : $"Редактирование торгового автомата #{_vendingMachineId}";

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                InitializeCommand.RaiseCanExecuteChanged();
                SaveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (SetProperty(ref _isSaving, value))
            {
                SaveCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public ObservableCollection<LookupItem> Manufacturers { get; } = new();
    public ObservableCollection<VendingMachineModelItem> Models { get; } = new();
    public ObservableCollection<VendingMachineModelItem> FilteredModels { get; } = new();

    public ObservableCollection<LookupItem> WorkModes { get; } = new();
    public ObservableCollection<LookupItem> TimeZones { get; } = new();
    public ObservableCollection<LookupItem> Statuses { get; } = new();
    public ObservableCollection<LookupItem> ServicePriorities { get; } = new();
    public ObservableCollection<LookupItem> ProductMatrices { get; } = new();
    public ObservableCollection<LookupItem> Countries { get; } = new();
    public ObservableCollection<LookupItem> Companies { get; } = new();
    public ObservableCollection<ModemItem> Modems { get; } = new();
    public ObservableCollection<LookupItem> CriticalValuesTemplates { get; } = new();
    public ObservableCollection<LookupItem> NotificationTemplates { get; } = new();

    public ObservableCollection<CheckableLookupItem> PaymentSystems { get; } = new();

    public int ManufacturerId
    {
        get => _manufacturerId;
        set
        {
            if (SetProperty(ref _manufacturerId, value))
            {
                RebuildFilteredModels();
            }
        }
    }

    public int ModelId
    {
        get => _modelId;
        set => SetProperty(ref _modelId, value);
    }

    public int WorkModeId
    {
        get => _workModeId;
        set => SetProperty(ref _workModeId, value);
    }

    public int TimeZoneId
    {
        get => _timeZoneId;
        set => SetProperty(ref _timeZoneId, value);
    }

    public int StatusId
    {
        get => _statusId;
        set => SetProperty(ref _statusId, value);
    }

    public int ServicePriorityId
    {
        get => _servicePriorityId;
        set => SetProperty(ref _servicePriorityId, value);
    }

    public int ProductMatrixId
    {
        get => _productMatrixId;
        set => SetProperty(ref _productMatrixId, value);
    }

    public int CountryId
    {
        get => _countryId;
        set => SetProperty(ref _countryId, value);
    }

    public int? CompanyId
    {
        get => _companyId;
        set => SetProperty(ref _companyId, value);
    }

    public int? ModemId
    {
        get => _modemId;
        set => SetProperty(ref _modemId, value);
    }

    public int? CriticalValuesTemplateId
    {
        get => _criticalValuesTemplateId;
        set => SetProperty(ref _criticalValuesTemplateId, value);
    }

    public int? NotificationTemplateId
    {
        get => _notificationTemplateId;
        set => SetProperty(ref _notificationTemplateId, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public string Place
    {
        get => _place;
        set => SetProperty(ref _place, value);
    }

    public string InventoryNumber
    {
        get => _inventoryNumber;
        set => SetProperty(ref _inventoryNumber, value);
    }

    public string SerialNumber
    {
        get => _serialNumber;
        set => SetProperty(ref _serialNumber, value);
    }

    public string? LatitudeText
    {
        get => _latitudeText;
        set => SetProperty(ref _latitudeText, value);
    }

    public string? LongitudeText
    {
        get => _longitudeText;
        set => SetProperty(ref _longitudeText, value);
    }

    public DateTime ManufactureDate
    {
        get => _manufactureDate;
        set => SetProperty(ref _manufactureDate, value);
    }

    public DateTime CommissioningDate
    {
        get => _commissioningDate;
        set => SetProperty(ref _commissioningDate, value);
    }

    public string? Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public AsyncRelayCommand InitializeCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }

    public VendingMachineEditViewModel(int? vendingMachineId = null)
    {
        _apiClient = ((App)Application.Current).Services.ApiClient;
        _vendingMachineId = vendingMachineId;

        InitializeCommand = new AsyncRelayCommand(InitializeAsync, () => !IsLoading);
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsLoading && !IsSaving);
    }

    public async Task InitializeAsync()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var lookups = await _apiClient.GetVendingMachineCreateLookupsAsync();
            FillLookups(lookups);

            if (_vendingMachineId is not null)
            {
                var details = await _apiClient.GetVendingMachineAsync(_vendingMachineId.Value);
                FillFromDetails(details);
            }
            else
            {
                ApplyDefaults();
            }
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void FillLookups(VendingMachineCreateLookups lookups)
    {
        Manufacturers.Clear();
        foreach (var x in lookups.Manufacturers)
        {
            Manufacturers.Add(x);
        }

        Models.Clear();
        foreach (var x in lookups.Models)
        {
            Models.Add(x);
        }

        WorkModes.Clear();
        foreach (var x in lookups.WorkModes)
        {
            WorkModes.Add(x);
        }

        TimeZones.Clear();
        foreach (var x in lookups.TimeZones)
        {
            TimeZones.Add(x);
        }

        Statuses.Clear();
        foreach (var x in lookups.VendingMachineStatuses)
        {
            Statuses.Add(x);
        }

        ServicePriorities.Clear();
        foreach (var x in lookups.ServicePriorities)
        {
            ServicePriorities.Add(x);
        }

        ProductMatrices.Clear();
        foreach (var x in lookups.ProductMatrices)
        {
            ProductMatrices.Add(x);
        }

        Countries.Clear();
        foreach (var x in lookups.Countries)
        {
            Countries.Add(x);
        }

        Companies.Clear();
        foreach (var x in lookups.Companies)
        {
            Companies.Add(x);
        }

        Modems.Clear();
        foreach (var x in lookups.Modems)
        {
            Modems.Add(x);
        }

        CriticalValuesTemplates.Clear();
        foreach (var x in lookups.CriticalValuesTemplates)
        {
            CriticalValuesTemplates.Add(x);
        }

        NotificationTemplates.Clear();
        foreach (var x in lookups.NotificationTemplates)
        {
            NotificationTemplates.Add(x);
        }

        PaymentSystems.Clear();
        foreach (var x in lookups.PaymentSystems)
        {
            PaymentSystems.Add(new CheckableLookupItem(x.Id, x.Name));
        }
    }

    private void ApplyDefaults()
    {
        ManufacturerId = Manufacturers.FirstOrDefault()?.Id ?? 0;
        WorkModeId = WorkModes.FirstOrDefault()?.Id ?? 0;
        TimeZoneId = TimeZones.FirstOrDefault(x => x.Name.Contains("UTC+3", StringComparison.OrdinalIgnoreCase))?.Id
            ?? TimeZones.FirstOrDefault()?.Id
            ?? 0;
        StatusId = Statuses.FirstOrDefault(x => x.Name.Contains("Работает", StringComparison.OrdinalIgnoreCase))?.Id
            ?? Statuses.FirstOrDefault()?.Id
            ?? 0;
        ServicePriorityId = ServicePriorities.FirstOrDefault(x => x.Name.Contains("Средний", StringComparison.OrdinalIgnoreCase))?.Id
            ?? ServicePriorities.FirstOrDefault()?.Id
            ?? 0;
        ProductMatrixId = ProductMatrices.FirstOrDefault()?.Id ?? 0;
        CountryId = Countries.FirstOrDefault()?.Id ?? 0;

        RebuildFilteredModels();
        ModelId = FilteredModels.FirstOrDefault()?.Id ?? 0;

        if (PaymentSystems.Count > 0)
        {
            PaymentSystems[0].IsSelected = true;
        }
    }

    private void FillFromDetails(VendingMachineDetails details)
    {
        Name = details.Name;
        Address = details.Address;
        Place = details.Place;
        InventoryNumber = details.InventoryNumber;
        SerialNumber = details.SerialNumber;

        LatitudeText = details.Latitude?.ToString(CultureInfo.InvariantCulture);
        LongitudeText = details.Longitude?.ToString(CultureInfo.InvariantCulture);

        WorkModeId = details.WorkModeId;
        TimeZoneId = details.TimeZoneId;
        StatusId = details.VendingMachineStatusId;
        ServicePriorityId = details.ServicePriorityId;
        ProductMatrixId = details.ProductMatrixId;
        CountryId = details.CountryId;
        CompanyId = details.CompanyId;

        ModemId = details.ModemId == -1 ? null : details.ModemId;

        CriticalValuesTemplateId = details.CriticalValuesTemplateId;
        NotificationTemplateId = details.NotificationTemplateId;

        ManufactureDate = details.ManufactureDate.ToDateTime(TimeOnly.MinValue);
        CommissioningDate = details.CommissioningDate.ToDateTime(TimeOnly.MinValue);

        Notes = details.Notes;

        var model = Models.FirstOrDefault(x => x.Id == details.VendingMachineModelId);
        if (model is not null)
        {
            ManufacturerId = model.ManufacturerId;
            RebuildFilteredModels();
            ModelId = model.Id;
        }
        else
        {
            RebuildFilteredModels();
            ModelId = details.VendingMachineModelId;
        }

        var selectedIds = details.PaymentSystemIds.ToHashSet();
        foreach (var ps in PaymentSystems)
        {
            ps.IsSelected = selectedIds.Contains(ps.Id);
        }
    }

    private void RebuildFilteredModels()
    {
        FilteredModels.Clear();
        foreach (var model in Models.Where(x => x.ManufacturerId == ManufacturerId))
        {
            FilteredModels.Add(model);
        }

        if (FilteredModels.All(x => x.Id != ModelId))
        {
            ModelId = FilteredModels.FirstOrDefault()?.Id ?? 0;
        }
    }

    private async Task SaveAsync()
    {
        ErrorMessage = null;
        IsSaving = true;

        try
        {
            if (!Validate(out var paymentSystemIds))
            {
                return;
            }

            var lat = ParseNullableDecimal(LatitudeText);
            var lon = ParseNullableDecimal(LongitudeText);

            var requestManufactureDate = DateOnly.FromDateTime(ManufactureDate.Date);
            var requestCommissioningDate = DateOnly.FromDateTime(CommissioningDate.Date);

            if (_vendingMachineId is null)
            {
                var create = new CreateVendingMachineRequest(
                    Name.Trim(),
                    ModelId,
                    WorkModeId,
                    TimeZoneId,
                    StatusId,
                    ServicePriorityId,
                    ProductMatrixId,
                    CompanyId,
                    ModemId,
                    Address.Trim(),
                    Place.Trim(),
                    lat,
                    lon,
                    InventoryNumber.Trim(),
                    SerialNumber.Trim(),
                    requestManufactureDate,
                    requestCommissioningDate,
                    LastVerificationDate: null,
                    VerificationIntervalMonths: null,
                    ResourceHours: null,
                    NextServiceDate: null,
                    ServiceDurationHours: null,
                    InventoryDate: null,
                    CountryId,
                    LastVerificationUserAccountId: null,
                    WorkingTimeFrom: null,
                    WorkingTimeTo: null,
                    CriticalValuesTemplateId,
                    NotificationTemplateId,
                    ManagerUserAccountId: null,
                    EngineerUserAccountId: null,
                    TechnicianOperatorUserAccountId: null,
                    KitOnlineCashRegisterId: null,
                    Notes: string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                    paymentSystemIds);

                await _apiClient.CreateVendingMachineAsync(create);
            }
            else
            {
                var update = new UpdateVendingMachineRequest(
                    Name.Trim(),
                    ModelId,
                    WorkModeId,
                    TimeZoneId,
                    StatusId,
                    ServicePriorityId,
                    ProductMatrixId,
                    CompanyId,
                    ModemId,
                    Address.Trim(),
                    Place.Trim(),
                    lat,
                    lon,
                    InventoryNumber.Trim(),
                    SerialNumber.Trim(),
                    requestManufactureDate,
                    requestCommissioningDate,
                    LastVerificationDate: null,
                    VerificationIntervalMonths: null,
                    ResourceHours: null,
                    NextServiceDate: null,
                    ServiceDurationHours: null,
                    InventoryDate: null,
                    CountryId,
                    LastVerificationUserAccountId: null,
                    WorkingTimeFrom: null,
                    WorkingTimeTo: null,
                    CriticalValuesTemplateId,
                    NotificationTemplateId,
                    ManagerUserAccountId: null,
                    EngineerUserAccountId: null,
                    TechnicianOperatorUserAccountId: null,
                    KitOnlineCashRegisterId: null,
                    Notes: string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                    paymentSystemIds);

                await _apiClient.UpdateVendingMachineAsync(_vendingMachineId.Value, update);
            }

            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool Validate(out List<int> paymentSystemIds)
    {
        paymentSystemIds = PaymentSystems.Where(x => x.IsSelected).Select(x => x.Id).ToList();

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Заполните поле «Название ТА»";
            return false;
        }

        if (ManufacturerId <= 0)
        {
            ErrorMessage = "Выберите производителя";
            return false;
        }

        if (ModelId <= 0)
        {
            ErrorMessage = "Выберите модель";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Address))
        {
            ErrorMessage = "Заполните поле «Адрес»";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Place))
        {
            ErrorMessage = "Заполните поле «Место»";
            return false;
        }

        if (string.IsNullOrWhiteSpace(InventoryNumber))
        {
            ErrorMessage = "Заполните поле «Инвентарный номер»";
            return false;
        }

        if (string.IsNullOrWhiteSpace(SerialNumber))
        {
            ErrorMessage = "Заполните поле «Серийный номер»";
            return false;
        }

        if (WorkModeId <= 0 || TimeZoneId <= 0 || StatusId <= 0 || ServicePriorityId <= 0 || ProductMatrixId <= 0 || CountryId <= 0)
        {
            ErrorMessage = "Заполните обязательные справочные поля (режим/часовой пояс/статус/приоритет/матрица/страна).";
            return false;
        }

        if (paymentSystemIds.Count == 0)
        {
            ErrorMessage = "Выберите хотя бы одну платежную систему.";
            return false;
        }

        return true;
    }

    private static decimal? ParseNullableDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
        {
            return d;
        }

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out d))
        {
            return d;
        }

        return null;
    }
}

