// Entry point for the Razor Pages sample that consumes Ecommerce.Api via the typed client.

using Ecommerce.Api.Client;
using Ecommerce.Api.Client.Services;

var builder = WebApplication.CreateBuilder(args);

// Enable Razor Pages so .cshtml pages under Pages/ are routed
builder.Services.AddRazorPages();

// Bind the "EcommerceApi" section from appsettings.json into options
builder.Services.Configure<EcommerceApiOptions>(
    builder.Configuration.GetSection("EcommerceApi"));

// Also register a concrete instance for constructor injection into EcommerceApiClient
var apiOptions = new EcommerceApiOptions();
builder.Configuration.GetSection("EcommerceApi").Bind(apiOptions);
builder.Services.AddSingleton(apiOptions);

// Register the typed HttpClient; in Development accept the local Kestrel self-signed cert
builder.Services.AddEcommerceApiClient(acceptAnyServerCertificate: builder.Environment.IsDevelopment());

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
