using System.Threading.RateLimiting;
// Imports API versioning abstractions used by Carter route groups.
using Asp.Versioning;
// Imports Carter to register vertical-slice endpoint modules.
using Carter;
// Imports shared API constants and response types.
using Ecommerce.Api.Common;
// Imports EF Core database context and seed entities.
using Ecommerce.Api.Data;
// Imports custom HTTP middleware implementations.
using Ecommerce.Api.Middleware;
// Imports credential challenge services and user entity types.
using Ecommerce.Api.Features.Auth;
using Ecommerce.Api.Models;
// Imports cookie authentication, authorization policies, and password hashing.
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
// Imports EF Core provider configuration.
using Microsoft.EntityFrameworkCore;
// Imports Swagger/OpenAPI document types.
using Microsoft.OpenApi.Models;

// Creates the web application and service configuration pipeline.
var builder = WebApplication.CreateBuilder(args);

var ecommerceConnectionString = builder.Configuration.GetConnectionString("EcommerceDb");
if (string.IsNullOrWhiteSpace(ecommerceConnectionString))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Set the ConnectionStrings:EcommerceDb PostgreSQL connection string.");

    ecommerceConnectionString = "Host=localhost;Database=ecommerce";
}

// Registers the durable PostgreSQL database context.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(ecommerceConnectionString));
var dataProtectionPath = builder.Configuration["DataProtection:KeyRingPath"];
if (string.IsNullOrWhiteSpace(dataProtectionPath))
{
    var localDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    dataProtectionPath = Path.Combine(
        string.IsNullOrWhiteSpace(localDataPath) ? builder.Environment.ContentRootPath : localDataPath,
        "Ecommerce.Api",
        "DataProtection-Keys");
}
Directory.CreateDirectory(dataProtectionPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("Ecommerce.Api");
builder.Services.AddSingleton<TotpService>();
// Hashes account passwords using ASP.NET Core's versioned password-hashing format.
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
// Stores short-lived registration and password-recovery challenges in process memory.
builder.Services.AddSingleton<CredentialFlowStore>();
// Sends verification and recovery links when SMTP settings are available.
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
// Protects authenticated frontend sessions with an encrypted, HttpOnly cookie.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Ecommerce.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
// Requires authentication by default; public credential routes opt out explicitly.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.AddPolicy("Administrator", policy => policy.RequireRole("Admin"));
});
// Registers the process-local idempotency store.
builder.Services.AddSingleton<IdempotencyStore>();
// Discovers and registers Carter endpoint modules.
builder.Services.AddCarter();
// Registers an explicit CORS policy for the React development origins.
builder.Services.AddCors(options =>
{
    // Creates a named policy shared by application middleware.
    options.AddPolicy("EcommerceMain", policy =>
        // Allows only known development origins.
        policy.WithOrigins(
                // Allows localhost through HTTP.
                "http://localhost:5173",
                // Allows loopback-IP development.
                "http://127.0.0.1:5173",
                // Allows the HTTPS-origin variant.
                "https://localhost:5173")
            // Allows request headers such as API-key and idempotency headers.
            .AllowAnyHeader()
            // Allows the CRUD HTTP methods used by the API.
            .AllowAnyMethod());
});

// Registers API version discovery and route substitution.
builder.Services.AddApiVersioning(options =>
{
    // Defaults calls to API major version one.
    options.DefaultApiVersion = new ApiVersion(1, 0);
    // Assumes version one when no version marker is supplied.
    options.AssumeDefaultVersionWhenUnspecified = true;
    // Reports supported versions in response headers.
    options.ReportApiVersions = true;
    // Accepts version information from URL, header, or query string.
    options.ApiVersionReader = ApiVersionReader.Combine(
        // Reads /api/v{version}/... route segments.
        new UrlSegmentApiVersionReader(),
        // Reads X-Api-Version for clients that use headers.
        new HeaderApiVersionReader("X-Api-Version"),
        // Reads api-version as a query parameter.
        new QueryStringApiVersionReader("api-version"));
}).AddApiExplorer(options =>
{
    // Groups Swagger operations into their versioned document names.
    options.GroupNameFormat = "'v'VVV";
    // Substitutes the discovered version into route templates.
    options.SubstituteApiVersionInUrl = true;
});

// Registers fixed-window IP rate limiting.
builder.Services.AddRateLimiter(options =>
{
    // Creates a partitioned limiter keyed by remote IP address.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        // Applies a fixed-window policy to each partition.
        RateLimitPartition.GetFixedWindowLimiter(
            // Falls back to an anonymous partition when no address exists.
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            // Defines the limit and queue behavior for each partition.
            factory: _ => new FixedWindowRateLimiterOptions
            {
                // Allows at most 100 requests in each minute.
                PermitLimit = 100,
                // Uses a one-minute fixed window.
                Window = TimeSpan.FromMinutes(1),
                // Processes queued requests oldest first.
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                // Allows ten requests to wait behind the limit.
                QueueLimit = 10
            }));
    // Converts limiter rejection into the project's JSON error format.
    options.OnRejected = async (context, cancellationToken) =>
    {
        // Sets the standard Too Many Requests status.
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        // Writes a structured, traceable rate-limit envelope.
        await context.HttpContext.Response.WriteAsJsonAsync(new ErrorEnvelope
        {
            // Links the response to the request trace.
            TraceId = context.HttpContext.TraceIdentifier,
            // Reports the HTTP status consistently.
            Status = StatusCodes.Status429TooManyRequests,
            // Names the failure.
            Title = "Too Many Requests",
            // Gives a safe retry instruction.
            Detail = "Rate limit exceeded. Please try again later."
        }, cancellationToken);
    };
    options.AddPolicy("AuthSensitive", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

// Configures HTTP JSON serialization behavior.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Converts C# PascalCase properties to camelCase JSON properties.
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// Registers endpoint metadata discovery for Swagger.
builder.Services.AddEndpointsApiExplorer();
// Registers the Swagger document generator and security scheme.
builder.Services.AddSwaggerGen(options =>
{
    // Defines the version-one API title and document version.
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Ecommerce Location API", Version = "v1" });
    // Documents the custom API-key header.
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        // Explains how clients supply the demo credential.
        Description = "API Key via X-Api-Key header",
        // Uses the shared API-key header name.
        Name = ApiConstants.ApiKeyHeaderName,
        // Places the credential in the HTTP header.
        In = ParameterLocation.Header,
        // Declares this as an API-key security scheme.
        Type = SecuritySchemeType.ApiKey
    });
    // Applies the API-key requirement to generated operations.
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            // References the named API-key security scheme.
            new OpenApiSecurityScheme
            {
                // Resolves the local security scheme by identifier.
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        }
    });
});

// Builds the configured application pipeline.
var app = builder.Build();

// Opens a dependency-injection scope for startup seeding.
using (var scope = app.Services.CreateScope())
{
    // Resolves the EF Core context used to inspect and seed data.
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Applies pending PostgreSQL schema migrations before the API serves requests.
    // Test hosts use SQLite's model-created schema because committed migrations target PostgreSQL.
    if (builder.Environment.IsDevelopment() &&
        builder.Configuration.GetValue<bool>("Database:EnsureCreated"))
        db.Database.EnsureCreated();
    else
        db.Database.Migrate();
    // Seeds demo data only when the location database is empty.
    if (!db.Provinces.Any())
    {
        // Creates stable seed IDs for location and catalog relationships.
        var ncrId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        // Creates the Makati city seed identifier.
        var makatiId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        // Creates the Poblacion barangay seed identifier.
        var poblacionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        // Creates the Beverages category seed identifier.
        var catId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        // Creates the Bottle unit-of-measure seed identifier.
        var uomId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        // Creates the Retail customer-class seed identifier.
        var customerClassId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        // Creates the Demo Customer seed identifier.
        var customerId = Guid.Parse("ffffffff-ffff-ffff-ffff-fffffffffffe");
        // Creates the demo user seed identifier.
        var userId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        // Adds Metro Manila to the in-memory location hierarchy.
        db.Provinces.Add(new Ecommerce.Api.Models.Province
        {
            // Links the province to the stable identifier.
            ProvinceId = ncrId, ProvinceName = "Metro Manila", CreatedBy = "seed", CreatedOn = DateTime.UtcNow
        });
        // Adds Makati beneath Metro Manila.
        db.Cities.Add(new Ecommerce.Api.Models.City
        {
            // Supplies the city relationship and audit fields.
            ProvinceId = ncrId, CityId = makatiId, CityName = "Makati", ZipCode = "1200",
            CreatedBy = "seed", CreatedOn = DateTime.UtcNow
        });
        // Adds Poblacion beneath Makati.
        db.Barangays.Add(new Ecommerce.Api.Models.Barangay
        {
            // Supplies compound location identifiers and audit fields.
            ProvinceId = ncrId, CityId = makatiId, BarangayId = poblacionId,
            BarangayName = "Poblacion", CreatedBy = "seed", CreatedOn = DateTime.UtcNow
        });
        // Creates the category entity once so products can reference it.
        var category = new Ecommerce.Api.Models.Category
        {
            // Supplies the Beverages category identity and audit fields.
            CategoryId = catId, CategoryName = "Beverages", CreatedBy = "seed", CreatedOn = DateTime.UtcNow
        };
        // Adds the category entity to the context.
        db.Categories.Add(category);
        // Adds two demo products for the storefront.
        db.Products.AddRange(
            new Ecommerce.Api.Models.Product
            {
                // Assigns a stable identifier to Cold Brew Coffee.
                ProductId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                // Supplies the product name.
                ProductName = "Cold Brew Coffee",
                // Supplies the product description.
                ProductDescription = "Smooth, ready-to-drink cold brew.",
                // Links the product to Beverages.
                CategoryId = catId,
                // Reuses the tracked category entity.
                Category = category,
                // Marks the product available.
                InStock = true,
                // Supplies demo audit attribution.
                CreatedBy = "seed",
                // Supplies the UTC seed timestamp.
                CreatedOn = DateTime.UtcNow
            },
            new Ecommerce.Api.Models.Product
            {
                // Assigns a stable identifier to Sparkling Citrus.
                ProductId = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                // Supplies the product name.
                ProductName = "Sparkling Citrus",
                // Supplies the product description.
                ProductDescription = "Refreshing sparkling citrus beverage.",
                // Links the product to Beverages.
                CategoryId = catId,
                // Reuses the tracked category entity.
                Category = category,
                // Marks the product available.
                InStock = true,
                // Supplies demo audit attribution.
                CreatedBy = "seed",
                // Supplies the UTC seed timestamp.
                CreatedOn = DateTime.UtcNow
            });
        var bottle = new Ecommerce.Api.Models.UnitOfMeasure
        {
            Id = uomId,
            Uom = "Btl",
            Description = "Bottle",
            CreatedBy = "seed",
            CreatedOn = DateTime.UtcNow
        };
        db.UnitOfMeasures.Add(bottle);
        var coldBrew = db.Products.Local.Single(product =>
            product.ProductId == Guid.Parse("10000000-0000-0000-0000-000000000001"));
        var sparklingCitrus = db.Products.Local.Single(product =>
            product.ProductId == Guid.Parse("10000000-0000-0000-0000-000000000002"));
        var metroManila = db.Provinces.Local.Single(province => province.ProvinceId == ncrId);
        db.UnitPrices.AddRange(
            new Ecommerce.Api.Models.UnitPrice
            {
                UnitPriceId = Guid.NewGuid(),
                ProductId = coldBrew.ProductId,
                Product = coldBrew,
                UomId = uomId,
                UnitOfMeasure = bottle,
                Price = 5.99m
            },
            new Ecommerce.Api.Models.UnitPrice
            {
                UnitPriceId = Guid.NewGuid(),
                ProductId = sparklingCitrus.ProductId,
                Product = sparklingCitrus,
                UomId = uomId,
                UnitOfMeasure = bottle,
                Price = 3.49m
            });
        db.AreasAvailable.AddRange(
            new Ecommerce.Api.Models.AreaAvailable
            {
                AreaAvailableId = Guid.NewGuid(),
                ProductId = coldBrew.ProductId,
                Product = coldBrew,
                ProvinceId = ncrId,
                Province = metroManila,
                IsAvailable = true
            },
            new Ecommerce.Api.Models.AreaAvailable
            {
                AreaAvailableId = Guid.NewGuid(),
                ProductId = sparklingCitrus.ProductId,
                Product = sparklingCitrus,
                ProvinceId = ncrId,
                Province = metroManila,
                IsAvailable = true
            });
        // Adds a retail customer class for the demo customer.
        db.CustomerClasses.Add(new Ecommerce.Api.Models.CustomerClass
        {
            // Supplies the customer-class identity and metadata.
            CustomerClassId = customerClassId,
            ClassName = "Retail",
            Description = "Retail customer",
            CreatedBy = "seed",
            CreatedOn = DateTime.UtcNow
        });
        // Adds a demo user for identity-like cart and order references.
        var demoUser = new Ecommerce.Api.Models.User
        {
            // Supplies stable user and company identifiers.
            Id = userId,
            CompanyId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            // Stores the demo account's recovery address and a one-way password hash.
            Username = "demo",
            Email = "shopper@example.com",
            CustomerId = customerId,
            // Assigns the currently supported user role.
            Role = Ecommerce.Api.Models.Enumerations.Roles.User,
            // Marks the demo user active.
            IsActive = true,
            // Supplies demo audit attribution.
            CreatedBy = "seed",
            // Supplies the UTC seed timestamp.
            CreatedOn = DateTime.UtcNow
        };
        demoUser.PasswordHash = new PasswordHasher<User>().HashPassword(demoUser, "demo");
        db.Users.Add(demoUser);
        // Adds a demo customer for checkout and location integration.
        db.Customers.Add(new Ecommerce.Api.Models.Customer
        {
            // Supplies stable customer and classification identifiers.
            CustomerId = customerId,
            CompanyName = "Demo Customer",
            CustomerClassId = customerClassId,
            // Supplies demo contact details.
            ContactPerson = "Demo Shopper",
            EmailAddress = "shopper@example.com",
            ContactNumber = "00000000000",
            // Supplies the complete location hierarchy.
            ProvinceId = ncrId,
            CityId = makatiId,
            BarangayId = poblacionId,
            // Supplies the demo street-level address.
            Address = "Poblacion",
            // Marks the customer active.
            IsActive = true,
            // Supplies demo audit attribution.
            CreatedBy = "seed",
            // Supplies the UTC seed timestamp.
            CreatedOn = DateTime.UtcNow
        });
        // Persists all startup seed entities together.
        db.SaveChanges();
    }

    if (app.Environment.IsDevelopment() && !db.Users.Any(user => user.Role == Ecommerce.Api.Models.Enumerations.Roles.Admin))
    {
        var administrator = new User
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@localhost",
            Role = Ecommerce.Api.Models.Enumerations.Roles.Admin,
            IsActive = true,
            CreatedBy = "seed",
            CreatedOn = DateTime.UtcNow
        };
        administrator.PasswordHash = new PasswordHasher<User>().HashPassword(administrator, "admin-local-only");
        db.Users.Add(administrator);
        db.SaveChanges();
    }
}

// Translates unhandled exceptions into controlled API envelopes.
app.UseMiddleware<ExceptionHandlingMiddleware>();
// Redirects HTTP requests when HTTPS redirection is configured.
app.UseHttpsRedirection();
// Applies the explicit React-origin CORS policy.
app.UseCors("EcommerceMain");
// Applies the fixed-window rate limiter.
app.UseRateLimiter();
// Requires the demo API-key header for protected routes.
app.UseMiddleware<ApiKeyAuthMiddleware>();
// Applies idempotency behavior to supported mutations.
app.UseMiddleware<IdempotencyMiddleware>();
// Resolves encrypted session cookies into the request user principal.
app.UseAuthentication();
// Enforces endpoint authorization policies after routing.
app.UseAuthorization();

// Enables Swagger only in Development to avoid exposing docs in Production.
if (app.Environment.IsDevelopment())
{
    // Serves the generated OpenAPI JSON document.
    app.UseSwagger();
    // Serves the interactive Swagger UI.
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ecommerce API v1"));
}

// Allows default document handling.
app.UseDefaultFiles();
// Allows static files in the API host.
app.UseStaticFiles();
// Discovers Carter routes and exposes endpoint handlers.
app.MapCarter();
// Starts the web application and blocks until shutdown.
app.Run();

// Exposes the top-level application class for WebApplicationFactory integration tests.
public partial class Program { }
