using Ecommerce.Api.Client;
using Ecommerce.Api.Client.Services;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddEcommerceApiClient(options =>
{
    // Update port to match your running API
    options.BaseUrl = "https://localhost:7001";
    options.ApiKey = "demo-api-key-12345";
});

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<EcommerceApiClient>();

Console.WriteLine("Fetching provinces...");
var result = await client.GetProvincesAsync(page: 1, pageSize: 10);
if (result.IsSuccess && result.Data is not null)
{
    Console.WriteLine($"Total: {result.Data.TotalCount}");
    foreach (var p in result.Data.Items)
        Console.WriteLine($" - {p.ProvinceName} ({p.ProvinceId})");
}
else
{
    Console.WriteLine($"Failed ({result.StatusCode}): {result.Error}");
}
