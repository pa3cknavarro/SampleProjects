using System.Net.Http.Json;
using System.Text.Json;
using Ecommerce.Api.Client.Models;

namespace Ecommerce.Api.Client.Services;

public class EcommerceApiClient
{
    private readonly HttpClient _http;
    private readonly EcommerceApiOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public EcommerceApiClient(HttpClient http, EcommerceApiOptions options)
    {
        _http = http;
        _options = options;

        if (_http.BaseAddress is null && !string.IsNullOrWhiteSpace(_options.BaseUrl))
            _http.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");

        if (!_http.DefaultRequestHeaders.Contains("X-Api-Key"))
            _http.DefaultRequestHeaders.Add("X-Api-Key", _options.ApiKey);
    }

    public Task<ApiResult<PaginatedResponse<ProvinceResponse>>> GetProvincesAsync(
        ListQuery? query = null, CancellationToken ct = default)
        => GetAsync<PaginatedResponse<ProvinceResponse>>(BuildListPath("provinces", query), ct);

    public Task<ApiResult<ProvinceResponse>> GetProvinceAsync(Guid provinceId, CancellationToken ct = default)
        => GetAsync<ProvinceResponse>($"api/{_options.ApiVersion}/provinces/{provinceId}", ct);

    public Task<ApiResult<ProvinceResponse>> CreateProvinceAsync(
        CreateProvinceRequest request, string? idempotencyKey = null, CancellationToken ct = default)
        => PostAsync<ProvinceResponse>($"api/{_options.ApiVersion}/provinces", request, idempotencyKey, ct);

    public Task<ApiResult<object?>> DeleteProvinceAsync(Guid provinceId, CancellationToken ct = default)
        => SendAsync<object?>(HttpMethod.Delete, $"api/{_options.ApiVersion}/provinces/{provinceId}", null, ct);

    public Task<ApiResult<PaginatedResponse<CityResponse>>> GetCitiesAsync(
        ListQuery? query = null, Guid? provinceId = null, CancellationToken ct = default)
    {
        var extra = provinceId is null
            ? Array.Empty<(string, string)>()
            : new[] { ("provinceId", provinceId.Value.ToString()) };
        return GetAsync<PaginatedResponse<CityResponse>>(BuildListPath("cities", query, extra), ct);
    }

    public Task<ApiResult<CityResponse>> CreateCityAsync(
        CreateCityRequest request, string? idempotencyKey = null, CancellationToken ct = default)
        => PostAsync<CityResponse>($"api/{_options.ApiVersion}/cities", request, idempotencyKey, ct);

    public Task<ApiResult<PaginatedResponse<BarangayResponse>>> GetBarangaysAsync(
        ListQuery? query = null, Guid? provinceId = null, Guid? cityId = null, CancellationToken ct = default)
    {
        var extra = new List<(string, string)>();
        if (provinceId.HasValue) extra.Add(("provinceId", provinceId.Value.ToString()));
        if (cityId.HasValue) extra.Add(("cityId", cityId.Value.ToString()));
        return GetAsync<PaginatedResponse<BarangayResponse>>(BuildListPath("barangays", query, extra.ToArray()), ct);
    }

    public Task<ApiResult<BarangayResponse>> CreateBarangayAsync(
        CreateBarangayRequest request, string? idempotencyKey = null, CancellationToken ct = default)
        => PostAsync<BarangayResponse>($"api/{_options.ApiVersion}/barangays", request, idempotencyKey, ct);

    private static string BuildListPath(string resource, ListQuery? query, params (string Key, string Value)[] extra)
    {
        var q = query ?? new ListQuery();
        var parts = new List<string> { $"page={q.Page}", $"pageSize={q.PageSize}" };
        if (!string.IsNullOrWhiteSpace(q.Search)) parts.Add($"search={Uri.EscapeDataString(q.Search)}");
        if (!string.IsNullOrWhiteSpace(q.SortBy)) parts.Add($"sortBy={Uri.EscapeDataString(q.SortBy)}");
        if (!string.IsNullOrWhiteSpace(q.SortOrder)) parts.Add($"sortOrder={Uri.EscapeDataString(q.SortOrder)}");
        foreach (var (key, value) in extra)
            parts.Add($"{key}={Uri.EscapeDataString(value)}");
        return $"api/v1/{resource}?{string.Join("&", parts)}";
    }

    private Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct)
        => SendAsync<T>(HttpMethod.Get, path, null, ct);

    private Task<ApiResult<T>> PostAsync<T>(string path, object body, string? idempotencyKey, CancellationToken ct)
        => SendAsync<T>(HttpMethod.Post, path, body, ct, idempotencyKey);

    private async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method, string path, object? body, CancellationToken ct, string? idempotencyKey = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            request.Headers.TryAddWithoutValidation("X-Idempotency-Key", idempotencyKey);

        using var res = await _http.SendAsync(request, ct);
        if (res.IsSuccessStatusCode)
        {
            if (res.StatusCode == System.Net.HttpStatusCode.NoContent)
                return ApiResult<T>.Success((int)res.StatusCode, default!);
            var data = await res.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
            return ApiResult<T>.Success((int)res.StatusCode, data!);
        }

        ErrorEnvelope? error = null;
        try
        {
            error = await res.Content.ReadFromJsonAsync<ErrorEnvelope>(JsonOptions, ct);
        }
        catch
        {
            var text = await res.Content.ReadAsStringAsync(ct);
            error = new ErrorEnvelope { Status = (int)res.StatusCode, Title = res.ReasonPhrase ?? "Error", Detail = text };
        }

        return ApiResult<T>.Failure((int)res.StatusCode, error);
    }
}
