// Sample console application that consumes Ecommerce.Api via Ecommerce.Api.Client.
// Demonstrates list, create, and cleanup flows against the vertical-slice API.

using Ecommerce.Api.Client;
using Ecommerce.Api.Client.Models;
using Ecommerce.Api.Client.Services;
using Microsoft.Extensions.DependencyInjection;

// Prefer environment variables so you do not hard-code ports:
//   ECOMMERCE_API_BASE_URL  e.g. https://localhost:7123
//   ECOMMERCE_API_KEY       defaults to demo-api-key-12345
var apiBaseUrl = Environment.GetEnvironmentVariable("ECOMMERCE_API_BASE_URL")
    ?? "https://localhost:5001";
var apiKey = Environment.GetEnvironmentVariable("ECOMMERCE_API_KEY")
    ?? "demo-api-key-12345";

Console.WriteLine("=== Ecommerce.Api Console Client ===\n");
Console.WriteLine($"Target API: {apiBaseUrl}");
Console.WriteLine("Ensure Ecommerce.Api is running (dotnet run in Ecommerce.Api).\n");

var services = new ServiceCollection();

services.AddHttpClient<EcommerceApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

services.AddSingleton(new EcommerceApiOptions
{
    BaseUrl = apiBaseUrl,
    ApiKey = apiKey
});

await using var sp = services.BuildServiceProvider();
var api = sp.GetRequiredService<EcommerceApiClient>();

try
{
    Console.WriteLine("--- List Provinces ---");
    var provinces = await api.GetProvincesAsync(new ListQuery { Page = 1, PageSize = 10, SortBy = "name" });
    if (!provinces.IsSuccess)
    {
        PrintError(provinces);
    }
    else
    {
        Console.WriteLine($"Total: {provinces.Data!.TotalCount}");
        foreach (var p in provinces.Data.Items)
            Console.WriteLine($"  {p.ProvinceName} ({p.ProvinceId})");
    }

    Console.WriteLine("\n--- Create Province ---");
    var createReq = new CreateProvinceRequest
    {
        ProvinceName = $"Console Demo {DateTime.UtcNow:HHmmss}",
        CreatedBy = "console-client"
    };
    var created = await api.CreateProvinceAsync(createReq, Guid.NewGuid().ToString());
    if (!created.IsSuccess)
    {
        PrintError(created);
        return;
    }

    Console.WriteLine($"  Created: {created.Data!.ProvinceName} / {created.Data.ProvinceId}");
    var parentId = created.Data.ProvinceId;

    Console.WriteLine("\n--- Get Province By Id ---");
    var one = await api.GetProvinceAsync(parentId);
    Console.WriteLine(one.IsSuccess
        ? $"  Found: {one.Data!.ProvinceName}"
        : $"  Failed: {one.Error?.Detail}");

    Console.WriteLine($"\n--- List Cities (province {parentId}) ---");
    var cities = await api.GetCitiesAsync(new ListQuery { PageSize = 5 }, provinceId: parentId);
    if (cities.IsSuccess)
    {
        foreach (var c in cities.Data!.Items)
            Console.WriteLine($"  {c.CityName} ({c.ZipCode})");
    }

    Console.WriteLine("\n--- Create City ---");
    var cityReq = new CreateCityRequest
    {
        ProvinceId = parentId,
        CityName = "Demo City",
        ZipCode = "9999",
        CreatedBy = "console-client"
    };
    var city = await api.CreateCityAsync(cityReq, Guid.NewGuid().ToString());
    if (city.IsSuccess)
    {
        Console.WriteLine($"  Created city: {city.Data!.CityName} / {city.Data.CityId}");

        Console.WriteLine("\n--- Create Barangay ---");
        var brgyReq = new CreateBarangayRequest
        {
            ProvinceId = city.Data.ProvinceId,
            CityId = city.Data.CityId,
            BarangayName = "Demo Barangay",
            CreatedBy = "console-client"
        };
        var brgy = await api.CreateBarangayAsync(brgyReq, Guid.NewGuid().ToString());
        if (brgy.IsSuccess)
            Console.WriteLine($"  Created barangay: {brgy.Data!.BarangayName} / {brgy.Data.BarangayId}");
        else
            PrintError(brgy);
    }
    else
    {
        PrintError(city);
    }

    Console.WriteLine("\n--- Cleanup (delete created province) ---");
    var del = await api.DeleteProvinceAsync(parentId);
    Console.WriteLine(del.IsSuccess
        ? "  Deleted."
        : $"  Delete status {del.StatusCode}: {del.Error?.Detail ?? "remove children first"}");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"\nConnection failed: {ex.Message}");
    Console.WriteLine("Set ECOMMERCE_API_BASE_URL to the HTTPS URL printed by `dotnet run` in Ecommerce.Api.");
}
catch (Exception ex)
{
    Console.WriteLine($"\nUnexpected error: {ex.Message}");
}

Console.WriteLine("\nDone.");

static void PrintError<T>(ApiResult<T> result)
{
    Console.WriteLine($"  ERROR {result.StatusCode}: {result.Error?.Title} – {result.Error?.Detail}");
}
