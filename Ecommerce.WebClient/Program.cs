using Ecommerce.Api.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddEcommerceApiClient(options =>
{
    options.BaseUrl = builder.Configuration["EcommerceApi:BaseUrl"] ?? "https://localhost:7001";
    options.ApiKey = builder.Configuration["EcommerceApi:ApiKey"] ?? "demo-api-key-12345";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.Run();
