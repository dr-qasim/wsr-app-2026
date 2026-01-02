using System.Windows;
using VendingService.WPF.Contracts.Companies;
using VendingService.WPF.Services.Api;

namespace VendingService.WPF.ViewModels;

public sealed class CompanyEditViewModel : ObservableObject
{
    private readonly ApiClient _apiClient;
    private readonly int? _companyId;

    private bool _isLoading;
    private bool _isSaving;
    private string? _errorMessage;

    private string _name = string.Empty;
    private string? _phone;
    private string? _email;
    private string? _address;

    public event EventHandler? Saved;

    public string Title => _companyId is null ? "Создание компании" : $"Редактирование компании #{_companyId}";

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

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string? Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string? Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string? Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public AsyncRelayCommand InitializeCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }

    public CompanyEditViewModel(int? companyId = null)
    {
        _apiClient = ((App)Application.Current).Services.ApiClient;
        _companyId = companyId;

        InitializeCommand = new AsyncRelayCommand(InitializeAsync, () => !IsLoading);
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsLoading && !IsSaving);
    }

    public async Task InitializeAsync()
    {
        if (_companyId is null)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var details = await _apiClient.GetCompanyAsync(_companyId.Value);
            Name = details.Name;
            Phone = details.Phone;
            Email = details.Email;
            Address = details.Address;
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

    private async Task SaveAsync()
    {
        ErrorMessage = null;
        IsSaving = true;

        try
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Заполните поле «Название»";
                return;
            }

            if (_companyId is null)
            {
                var create = new CreateCompanyRequest(
                    Name.Trim(),
                    string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                    string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                    string.IsNullOrWhiteSpace(Address) ? null : Address.Trim());

                await _apiClient.CreateCompanyAsync(create);
            }
            else
            {
                var update = new UpdateCompanyRequest(
                    Name.Trim(),
                    string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                    string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
                    string.IsNullOrWhiteSpace(Address) ? null : Address.Trim());

                await _apiClient.UpdateCompanyAsync(_companyId.Value, update);
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
}

