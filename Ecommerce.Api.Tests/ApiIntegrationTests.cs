// Imports JSON support for decoding paginated API responses.
using System.Net.Http.Json;
using System.Text.Json;
// Imports the test HTTP factory used to start the API in memory.
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
// Imports xUnit test attributes and assertions.
using Xunit;

// Hosts API integration tests in an isolated WebApplicationFactory process.
public class ApiIntegrationTests : IClassFixture<TestApiFactory>
{
    // Stores the factory so every test receives the same application host.
    private readonly TestApiFactory _factory;

    // Creates the test fixture and supplies the application factory.
    public ApiIntegrationTests(TestApiFactory factory)
    {
        // Assigns the factory to the test instance for reuse by test methods.
        _factory = factory;
    }

    // Verifies that the versioned products route accepts the configured API key.
    [PostgreSqlFact]
    public async Task ProductsEndpointReturnsPaginatedPayload()
    {
        // Creates an isolated HTTP client whose host is the test API.
        var client = _factory.CreateClient();
        // Adds the demo API key to authenticate the request.
        client.DefaultRequestHeaders.Add("X-Api-Key", "demo-api-key-12345");
        // Sends the request through the actual Carter endpoint.
        var response = await client.GetAsync("/api/v1/products?page=1&pageSize=12");
        // Requires a successful HTTP status before inspecting the payload.
        response.EnsureSuccessStatusCode();
        // Deserializes the response using a minimal contract for this test.
        var payload = await response.Content.ReadFromJsonAsync<ProductPayload>();
        // Confirms pagination metadata and product collection presence.
        Assert.NotNull(payload);
        Assert.Equal(1, payload.Page);
        Assert.True(payload.TotalCount >= 1);
        Assert.NotEmpty(payload.Items);
    }

    // Verifies that unauthenticated API requests receive a controlled 401 envelope.
    [PostgreSqlFact]
    public async Task ProductsEndpointRejectsMissingApiKey()
    {
        // Creates an HTTP client without authentication headers.
        var client = _factory.CreateClient();
        // Sends a request to the protected products endpoint.
        var response = await client.GetAsync("/api/v1/products");
        // Requires the API to return the documented unauthorized status.
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [PostgreSqlFact]
    public async Task RegistrationVerificationAndPasswordResetUseHashedCookieSessions()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "demo-api-key-12345");
        var email = $"user-{Guid.NewGuid():N}@example.com";
        const string initialPassword = "Initial-password-123";
        const string replacementPassword = "Replacement-password-456";

        var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = initialPassword,
            companyName = "Test Company"
        });
        registration.EnsureSuccessStatusCode();
        var verification = await registration.Content.ReadFromJsonAsync<VerificationPayload>();
        Assert.NotNull(verification?.VerificationCode);
        Assert.Equal(6, verification.VerificationCode.Length);

        var verified = await client.PostAsJsonAsync("/api/v1/auth/verify-email", new
        {
            email,
            code = verification.VerificationCode
        });
        verified.EnsureSuccessStatusCode();

        var profile = await client.GetFromJsonAsync<AuthUserPayload>("/api/v1/auth/me");
        Assert.Equal(email, profile?.Email);
        Assert.NotNull(profile?.CustomerId);

        var customer = await client.GetFromJsonAsync<CustomerProfilePayload>("/api/v1/customers/me");
        Assert.Equal("Test Company", customer?.CompanyName);
        Assert.Equal(profile.CustomerId, customer?.CustomerId);

        var orderResponse = await client.PostAsJsonAsync("/api/v1/orders", new
        {
            customerId = Guid.Empty,
            orderFreight = 0,
            discountAmount = 0,
            comment = "Integration test",
            createdBy = email,
            modifiedBy = email,
            orderDetail = new[]
            {
                new
                {
                    productId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    uomId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                    quantity = 2,
                    unitPrice = 5.99,
                    itemFreight = 0,
                    discountAmount = 0,
                    comment = ""
                }
            }
        });
        Assert.True(
            orderResponse.StatusCode == System.Net.HttpStatusCode.Created,
            await orderResponse.Content.ReadAsStringAsync());
        var orderPayload = await orderResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = orderPayload.GetProperty("orderId").GetGuid();
        var createdOrder = await client.GetFromJsonAsync<OrderPayload>($"/api/v1/orders/{orderId}");
        Assert.Equal(profile.CustomerId, createdOrder?.CustomerId);
        Assert.Equal(11.98m, createdOrder?.GrandTotal);
        var notifications = await client.GetFromJsonAsync<NotificationListPayload>("/api/v1/notifications?page=1&pageSize=50");
        Assert.Contains(notifications?.Items ?? [], item => item.Title == "Order received");
        var orderNotification = notifications!.Items.First(item => item.Title == "Order received");
        var markRead = await client.PostAsync($"/api/v1/notifications/{orderNotification.NotificationId}/read", content: null);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, markRead.StatusCode);

        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Add("X-Api-Key", "demo-api-key-12345");
        var adminLogin = await adminClient.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = "admin",
            password = "admin-local-only"
        });
        adminLogin.EnsureSuccessStatusCode();
        var statusUpdate = await adminClient.PutAsJsonAsync($"/api/v1/orders/{orderId}", new
        {
            orderStatus = "Shipped"
        });
        statusUpdate.EnsureSuccessStatusCode();

        var productCreate = await adminClient.PostAsJsonAsync("/api/v1/products", new
        {
            productName = "Admin Test Product",
            productDescription = "Created by the integration test.",
            categoryId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            productImageUrl = "",
            inStock = true
        });
        Assert.Equal(System.Net.HttpStatusCode.Created, productCreate.StatusCode);
        var createdProductPayload = await productCreate.Content.ReadFromJsonAsync<JsonElement>();
        var createdProductId = createdProductPayload.GetProperty("productId").GetGuid();
        var productUpdate = await adminClient.PutAsJsonAsync($"/api/v1/products/{createdProductId}", new
        {
            productName = "Updated Test Product",
            productDescription = "Updated by the integration test.",
            categoryId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            productImageUrl = "",
            inStock = true
        });
        Assert.Equal(System.Net.HttpStatusCode.OK, productUpdate.StatusCode);
        var unitPriceCreate = await adminClient.PostAsJsonAsync("/api/v1/unit-prices", new
        {
            productId = createdProductId,
            uomId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            price = 8.25m
        });
        Assert.Equal(System.Net.HttpStatusCode.Created, unitPriceCreate.StatusCode);
        var unitPricePayload = await unitPriceCreate.Content.ReadFromJsonAsync<JsonElement>();
        var unitPriceId = unitPricePayload.GetProperty("unitPriceId").GetGuid();
        var newProductOrder = new
        {
            customerId = Guid.Empty,
            shippingProvinceId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            orderFreight = 0,
            discountAmount = 0,
            orderDetail = new[]
            {
                new
                {
                    productId = createdProductId,
                    uomId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                    quantity = 1,
                    unitPrice = 8.25m,
                    itemFreight = 0,
                    discountAmount = 0
                }
            }
        };
        var missingAvailabilityOrder = await client.PostAsJsonAsync("/api/v1/orders", newProductOrder);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, missingAvailabilityOrder.StatusCode);

        var unitPriceUpdate = await adminClient.PutAsJsonAsync(
            $"/api/v1/unit-prices/{createdProductId}/dddddddd-dddd-dddd-dddd-dddddddddddd/{unitPriceId}",
            new { price = 9.25m });
        Assert.Equal(System.Net.HttpStatusCode.OK, unitPriceUpdate.StatusCode);
        var availabilityCreate = await adminClient.PostAsJsonAsync("/api/v1/areas-available", new
        {
            productId = createdProductId,
            provinceId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            isAvailable = true
        });
        Assert.Equal(System.Net.HttpStatusCode.Created, availabilityCreate.StatusCode);
        var availabilityPayload = await availabilityCreate.Content.ReadFromJsonAsync<JsonElement>();
        var availabilityId = availabilityPayload.GetProperty("areaAvailableId").GetGuid();
        var outOfStockUpdate = await adminClient.PutAsJsonAsync($"/api/v1/products/{createdProductId}", new
        {
            productName = "Updated Test Product",
            productDescription = "Updated by the integration test.",
            categoryId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            productImageUrl = "",
            inStock = false
        });
        Assert.Equal(System.Net.HttpStatusCode.OK, outOfStockUpdate.StatusCode);
        var outOfStockOrder = await client.PostAsJsonAsync("/api/v1/orders", newProductOrder);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, outOfStockOrder.StatusCode);
        var availabilityUpdate = await adminClient.PutAsJsonAsync($"/api/v1/areas-available/{availabilityId}", new
        {
            isAvailable = false
        });
        Assert.Equal(System.Net.HttpStatusCode.OK, availabilityUpdate.StatusCode);
        var availableProducts = await client.GetFromJsonAsync<ProductPayload>(
            "/api/v1/products?page=1&pageSize=100&provinceId=11111111-1111-1111-1111-111111111111");
        Assert.DoesNotContain(availableProducts!.Items, item =>
            item.GetProperty("productId").GetGuid() == createdProductId);

        var recovery = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email });
        recovery.EnsureSuccessStatusCode();
        var recoveryResult = await recovery.Content.ReadFromJsonAsync<PasswordResetPayload>();
        Assert.NotNull(recoveryResult?.ResetUrl);
        var resetToken = Uri.UnescapeDataString(new Uri(recoveryResult.ResetUrl).Query["?token=".Length..]);

        var reset = await client.PostAsJsonAsync("/api/v1/auth/reset-password", new
        {
            token = resetToken,
            newPassword = replacementPassword
        });
        Assert.Equal(System.Net.HttpStatusCode.NoContent, reset.StatusCode);

        await client.PostAsync("/api/v1/auth/logout", content: null);
        var loggedOutProfile = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, loggedOutProfile.StatusCode);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = email,
            password = replacementPassword
        });
        login.EnsureSuccessStatusCode();
    }

    [PostgreSqlFact]
    public async Task PostgresDataSurvivesAHostRestart()
    {
        var keyRingPath = Path.Combine(Path.GetTempPath(), $"ecommerce-keys-{Guid.NewGuid():N}");
        var productId = Guid.Empty;
        try
        {
            using (var firstHost = TestApiFactory.Create(keyRingPath))
            {
                var client = firstHost.CreateClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", "demo-api-key-12345");
                using var products = await client.GetAsync("/api/v1/products?page=1&pageSize=1");
                products.EnsureSuccessStatusCode();
                var payload = await products.Content.ReadFromJsonAsync<JsonElement>();
                productId = payload.GetProperty("items")[0].GetProperty("productId").GetGuid();
            }

            using (var restartedHost = TestApiFactory.Create(keyRingPath))
            {
                var client = restartedHost.CreateClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", "demo-api-key-12345");
                using var product = await client.GetAsync($"/api/v1/products/{productId}");
                product.EnsureSuccessStatusCode();
                var payload = await product.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal(productId, payload.GetProperty("productId").GetGuid());
                Assert.True(payload.GetProperty("defaultPrice").GetProperty("price").GetDecimal() > 0);
            }
        }
        finally
        {
            if (Directory.Exists(keyRingPath)) Directory.Delete(keyRingPath, recursive: true);
        }
    }

    // Defines only the fields needed to verify the paginated product contract.
    private sealed class ProductPayload
    {
        // Holds the returned product collection.
        public List<JsonElement> Items { get; set; } = [];
        // Holds the total matching record count.
        public int TotalCount { get; set; }
        // Holds the requested page number.
        public int Page { get; set; }
    }

    private sealed class VerificationPayload
    {
        public string VerificationCode { get; set; } = string.Empty;
    }

    private sealed class PasswordResetPayload
    {
        public string ResetUrl { get; set; } = string.Empty;
    }

    private sealed class AuthUserPayload
    {
        public string Email { get; set; } = string.Empty;
        public Guid? CustomerId { get; set; }
    }

    private sealed class CustomerProfilePayload
    {
        public Guid CustomerId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
    }

    private sealed class OrderPayload
    {
        public Guid CustomerId { get; set; }
        public decimal GrandTotal { get; set; }
    }

    private sealed class NotificationListPayload
    {
        public List<NotificationPayload> Items { get; set; } = [];
        public int UnreadCount { get; set; }
    }

    private sealed class NotificationPayload
    {
        public Guid NotificationId { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly string _keyRingPath;

    public TestApiFactory() : this(Path.Combine(Path.GetTempPath(), $"ecommerce-keys-{Guid.NewGuid():N}"))
    {
    }

    private TestApiFactory(string keyRingPath)
    {
        _keyRingPath = keyRingPath;
    }

    public static TestApiFactory Create(string keyRingPath) => new(keyRingPath);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataProtection:KeyRingPath"] = _keyRingPath
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (Directory.Exists(_keyRingPath)) Directory.Delete(_keyRingPath, recursive: true);
    }
}

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__EcommerceDb")))
            Skip = "Set ConnectionStrings__EcommerceDb to run tests that require PostgreSQL.";
    }
}
