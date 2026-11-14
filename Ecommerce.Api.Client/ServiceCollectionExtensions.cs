using Ecommerce.Api.Client.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Api.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEcommerceApiClient(
        this IServiceCollection services,
        Action<EcommerceApiOptions> configure,
        bool acceptAnyServerCertificate = false)
    {
        var options = new EcommerceApiOptions();
        configure(options);
        services.AddSingleton(options);

        var builder = services.AddHttpClient<EcommerceApiClient>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        if (acceptAnyServerCertificate)
        {
            builder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
        }

        return services;
    }

    public static IServiceCollection AddEcommerceApiClient(
        this IServiceCollection services,
        bool acceptAnyServerCertificate = false)
    {
        var builder = services.AddHttpClient<EcommerceApiClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<EcommerceApiOptions>();
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        if (acceptAnyServerCertificate)
        {
            builder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });
        }

        return services;
    }
}
