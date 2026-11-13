using Ecommerce.Api.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Api.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEcommerceApiClient(
        this IServiceCollection services,
        Action<EcommerceApiOptions> configure)
    {
        var options = new EcommerceApiOptions();
        configure(options);

        services.AddHttpClient<EcommerceApiClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        return services;
    }
}
