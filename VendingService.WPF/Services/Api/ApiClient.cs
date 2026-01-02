using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using VendingService.WPF.Contracts.Auth;
using VendingService.WPF.Contracts.Companies;
using VendingService.WPF.Contracts.Common;
using VendingService.WPF.Contracts.Dashboard;
using VendingService.WPF.Contracts.Lookups;
using VendingService.WPF.Contracts.Monitor;
using VendingService.WPF.Contracts.VendingMachines;

namespace VendingService.WPF.Services.Api;

public sealed class ApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public void SetBearerToken(string? accessToken)
    {
        httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(accessToken)
            ? null
            : new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/auth/login", request, JsonOptions, cancellationToken);
        return await ReadJsonOrThrow<LoginResponse>(response, cancellationToken);
    }

    public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/auth/refresh-token", request, JsonOptions, cancellationToken);
        return await ReadJsonOrThrow<LoginResponse>(response, cancellationToken);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/auth/logout", request, JsonOptions, cancellationToken);
        await EnsureSuccessOrThrow(response, cancellationToken);
    }

    public async Task<VendingMachineCreateLookups> GetVendingMachineCreateLookupsAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("/lookups/vending-machine-create", cancellationToken);
        return await ReadJsonOrThrow<VendingMachineCreateLookups>(response, cancellationToken);
    }

    public async Task<PagedResult<VendingMachineListItem>> GetVendingMachinesAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var url = $"/vending-machines?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"&search={Uri.EscapeDataString(search.Trim())}";
        }

        var response = await httpClient.GetAsync(url, cancellationToken);
        return await ReadJsonOrThrow<PagedResult<VendingMachineListItem>>(response, cancellationToken);
    }

    public async Task<PagedResult<CompanyListItem>> GetCompaniesAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var url = $"/companies?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search))
        {
            url += $"&search={Uri.EscapeDataString(search.Trim())}";
        }

        var response = await httpClient.GetAsync(url, cancellationToken);
        return await ReadJsonOrThrow<PagedResult<CompanyListItem>>(response, cancellationToken);
    }

    public async Task<CompanyDetails> GetCompanyAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/companies/{companyId}", cancellationToken);
        return await ReadJsonOrThrow<CompanyDetails>(response, cancellationToken);
    }

    public async Task<CompanyDetails> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/companies", request, JsonOptions, cancellationToken);
        return await ReadJsonOrThrow<CompanyDetails>(response, cancellationToken);
    }

    public async Task<CompanyDetails> UpdateCompanyAsync(int companyId, UpdateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/companies/{companyId}", request, JsonOptions, cancellationToken);
        return await ReadJsonOrThrow<CompanyDetails>(response, cancellationToken);
    }

    public async Task DeleteCompanyAsync(int companyId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/companies/{companyId}", cancellationToken);
        await EnsureSuccessOrThrow(response, cancellationToken);
    }

    public async Task<VendingMachineDetails> GetVendingMachineAsync(int vendingMachineId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/vending-machines/{vendingMachineId}", cancellationToken);
        return await ReadJsonOrThrow<VendingMachineDetails>(response, cancellationToken);
    }

    public async Task<VendingMachineDetails> CreateVendingMachineAsync(CreateVendingMachineRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/vending-machines", request, JsonOptions, cancellationToken);
        return await ReadJsonOrThrow<VendingMachineDetails>(response, cancellationToken);
    }

    public async Task<VendingMachineDetails> UpdateVendingMachineAsync(int vendingMachineId, UpdateVendingMachineRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/vending-machines/{vendingMachineId}", request, JsonOptions, cancellationToken);
        return await ReadJsonOrThrow<VendingMachineDetails>(response, cancellationToken);
    }

    public async Task DetachVendingMachineModemAsync(int vendingMachineId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync($"/vending-machines/{vendingMachineId}/detach-modem", content: null, cancellationToken);
        await EnsureSuccessOrThrow(response, cancellationToken);
    }

    public async Task DeleteVendingMachineAsync(int vendingMachineId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/vending-machines/{vendingMachineId}", cancellationToken);
        await EnsureSuccessOrThrow(response, cancellationToken);
    }

    public async Task<MonitorLookups> GetMonitorLookupsAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("/monitor/lookups", cancellationToken);
        return await ReadJsonOrThrow<MonitorLookups>(response, cancellationToken);
    }

    public async Task<MonitorSnapshotResponse> GetMonitorSnapshotAsync(int afterEventId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/monitor/snapshot?afterEventId={afterEventId}", cancellationToken);
        return await ReadJsonOrThrow<MonitorSnapshotResponse>(response, cancellationToken);
    }

    public async Task<DashboardOverviewResponse> GetDashboardOverviewAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("/dashboard/overview", cancellationToken);
        return await ReadJsonOrThrow<DashboardOverviewResponse>(response, cancellationToken);
    }

    private static async Task<T> ReadJsonOrThrow<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            if (data is null)
            {
                throw new ApiException(response.StatusCode, "Empty response body.");
            }

            return data;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new ApiException(response.StatusCode, ExtractMessage(body) ?? response.ReasonPhrase ?? "Request failed.");
    }

    private static async Task EnsureSuccessOrThrow(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new ApiException(response.StatusCode, ExtractMessage(body) ?? response.ReasonPhrase ?? "Request failed.");
    }

    private static string? ExtractMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return body;
            }

            if (doc.RootElement.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
            {
                var text = message.GetString();
                if (doc.RootElement.TryGetProperty("details", out var details) && details.ValueKind == JsonValueKind.String)
                {
                    var detailsText = details.GetString();
                    if (!string.IsNullOrWhiteSpace(detailsText))
                    {
                        text = string.IsNullOrWhiteSpace(text) ? detailsText : $"{text}\n{detailsText}";
                    }
                }

                return text;
            }

            if (doc.RootElement.TryGetProperty("Message", out var message2) && message2.ValueKind == JsonValueKind.String)
            {
                var text = message2.GetString();
                if (doc.RootElement.TryGetProperty("Details", out var details) && details.ValueKind == JsonValueKind.String)
                {
                    var detailsText = details.GetString();
                    if (!string.IsNullOrWhiteSpace(detailsText))
                    {
                        text = string.IsNullOrWhiteSpace(text) ? detailsText : $"{text}\n{detailsText}";
                    }
                }

                return text;
            }

            // ValidationProblemDetails (RFC 9110 / ASP.NET Core default)
            if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in errors.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        var first = prop.Value.EnumerateArray().FirstOrDefault();
                        if (first.ValueKind == JsonValueKind.String)
                        {
                            return first.GetString();
                        }
                    }
                }
            }

            if (doc.RootElement.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
            {
                var titleText = title.GetString();
                if (doc.RootElement.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                {
                    var detailText = detail.GetString();
                    if (!string.IsNullOrWhiteSpace(detailText))
                    {
                        return string.IsNullOrWhiteSpace(titleText) ? detailText : $"{titleText}\n{detailText}";
                    }
                }

                return titleText;
            }
        }
        catch (JsonException)
        {
            // ignore
        }

        return body;
    }
}

public sealed class ApiException(HttpStatusCode statusCode, string message) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    public override string ToString() => $"API {(int)StatusCode} {StatusCode}: {Message}";
}
