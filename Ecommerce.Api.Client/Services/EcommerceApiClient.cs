using System.Net.Http.Json;
using Ecommerce.Api.Client.Models;

namespace Ecommerce.Api.Client.Services;

public class EcommerceApiClient
{
    private readonly HttpClient _http;

    public EcommerceApiClient(HttpClient http) => _http = http;

    public async Task<ApiResult<PaginatedResponse<ProvinceResponse>>> GetProvincesAsync(
        int page = 1, int pageSize = 10, string? search = null, CancellationToken ct = default)
    {
        var url = $"api/v1/provinces?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(search)) url += $"&search={Uri.EscapeDataString(search)}";
        return await SendAsync<PaginatedResponse<ProvinceResponse>>(HttpMethod.Get, url, null, ct);
    }

    public async Task<ApiResult<ProvinceResponse>> CreateProvinceAsync(
        CreateProvinceRequest request, string? idempotencyKey = null, CancellationToken ct = default)
    {
        return await SendAsync<ProvinceResponse>(HttpMethod.Post, "api/v1/provinces", request, ct, idempotencyKey);
    }

    public async Task<ApiResult<PaginatedResponse<CityResponse>>> GetCitiesAsync(
        int page = 1, int pageSize = 10, Guid? provinceId = null, CancellationToken ct = default)
    {
        var url = $"api/v1/cities?page={page}&pageSize={pageSize}";
        if (provinceId.HasValue) url += $"&provinceId={provinceId}";
        return await SendAsync<PaginatedResponse<CityResponse>>(HttpMethod.Get, url, null, ct);
    }

    public async Task<ApiResult<PaginatedResponse<BarangayResponse>>> GetBarangaysAsync(
        int page = 1, int pageSize = 10, Guid? provinceId = null, Guid? cityId = null, CancellationToken ct = default)
    {
        var url = $"api/v1/barangays?page={page}&pageSize={pageSize}";
        if (provinceId.HasValue) url += $"&provinceId={provinceId}";
        if (cityId.HasValue) url += $"&cityId={cityId}";
        return await SendAsync<PaginatedResponse<BarangayResponse>>(HttpMethod.Get, url, null, ct);
    }

    private async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method, string url, object? body, CancellationToken ct, string? idempotencyKey = null)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            request.Headers.TryAddWithoutValidation("X-Idempotency-Key", idempotencyKey);

        using var response = await _http.SendAsync(request, ct);
        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                return ApiResult<T>.Success(default!, (int)response.StatusCode);
            var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            return ApiResult<T>.Success(data!, (int)response.StatusCode);
        }

        var errorText = await response.Content.ReadAsStringAsync(ct);
        return ApiResult<T>.Failure((int)response.StatusCode, errorText);
    }
}
