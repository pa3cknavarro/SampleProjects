using System.Threading.RateLimiting;
using Asp.Versioning;
using Carter;
using Ecommerce.Api.Common;
using Ecommerce.Api.Data;
using Ecommerce.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("EcommerceDb"));
builder.Services.AddSingleton<IdempotencyStore>();
builder.Services.AddCarter();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"),
        new QueryStringApiVersionReader("api-version"));
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new ErrorEnvelope
        {
            TraceId = context.HttpContext.TraceIdentifier,
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Detail = "Rate limit exceeded. Please try again later."
        }, cancellationToken);
    };
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Ecommerce Location API", Version = "v1" });
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key via X-Api-Key header",
        Name = ApiConstants.ApiKeyHeaderName,
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!db.Provinces.Any())
    {
        var ncrId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var makatiId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var poblacionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        db.Provinces.Add(new Ecommerce.Api.Models.Province
        {
            ProvinceId = ncrId, ProvinceName = "Metro Manila", CreatedBy = "seed", CreatedOn = DateTime.UtcNow
        });
        db.Cities.Add(new Ecommerce.Api.Models.City
        {
            ProvinceId = ncrId, CityId = makatiId, CityName = "Makati", ZipCode = "1200",
            CreatedBy = "seed", CreatedOn = DateTime.UtcNow
        });
        db.Barangays.Add(new Ecommerce.Api.Models.Barangay
        {
            ProvinceId = ncrId, CityId = makatiId, BarangayId = poblacionId,
            BarangayName = "Poblacion", CreatedBy = "seed", CreatedOn = DateTime.UtcNow
        });
        var catId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        db.Categories.Add(new Ecommerce.Api.Models.Category
        {
            CategoryId = catId, CategoryName = "Beverages", CreatedBy = "seed", CreatedOn = DateTime.UtcNow
        });
        db.SaveChanges();
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseMiddleware<ApiKeyAuthMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ecommerce API v1"));
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapCarter();
app.Run();
