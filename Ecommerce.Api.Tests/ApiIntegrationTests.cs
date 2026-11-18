// Imports JSON support for decoding paginated API responses.
using System.Net.Http.Json;
using System.Text.Json;
using System.Buffers.Binary;
using System.Security.Cryptography;
// Imports the test HTTP factory used to start the API in memory.
using Ecommerce.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    [Fact]
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
    [Fact]
    public async Task ProductsEndpointRejectsMissingApiKey()
    {
        // Creates an HTTP client without authentication headers.
        var client = _factory.CreateClient();
        // Sends a request to the protected products endpoint.
        var response = await client.GetAsync("/api/v1/products");
        // Requires the API to return the documented unauthorized status.
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
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

        var mfaSetup = await client.PostAsJsonAsync("/api/v1/auth/mfa/setup", new { currentPassword = initialPassword });
        mfaSetup.EnsureSuccessStatusCode();
        var mfaSetupPayload = await mfaSetup.Content.ReadFromJsonAsync<MfaSetupPayload>();
        Assert.NotNull(mfaSetupPayload?.Secret);
        var mfaEnabled = await client.PostAsJsonAsync("/api/v1/auth/mfa/enable", new
        {
            code = CreateTotp(mfaSetupPayload.Secret)
        });
        mfaEnabled.EnsureSuccessStatusCode();
        var recoveryCodes = await mfaEnabled.Content.ReadFromJsonAsync<MfaEnabledPayload>();
        Assert.Equal(10, recoveryCodes?.RecoveryCodes.Count);

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

        var mfaRequiredLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = email,
            password = replacementPassword
        });
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, mfaRequiredLogin.StatusCode);
        var mfaRequired = await mfaRequiredLogin.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(mfaRequired.GetProperty("requiresTwoFactor").GetBoolean());
        var recoveryCode = recoveryCodes!.RecoveryCodes[0];
        var recoveryLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = email,
            password = replacementPassword,
            twoFactorCode = recoveryCode
        });
        recoveryLogin.EnsureSuccessStatusCode();
        await client.PostAsync("/api/v1/auth/logout", content: null);
        var reusedRecoveryCode = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = email,
            password = replacementPassword,
            twoFactorCode = recoveryCode
        });
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, reusedRecoveryCode.StatusCode);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = email,
            password = replacementPassword,
            twoFactorCode = CreateTotp(mfaSetupPayload!.Secret)
        });
        login.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SqliteDataSurvivesAHostRestart()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"ecommerce-{Guid.NewGuid():N}.db");
        var productId = Guid.Empty;
        try
        {
            using (var firstHost = TestApiFactory.Create(databasePath, deleteOnDispose: false))
            {
                var client = firstHost.CreateClient();
                client.DefaultRequestHeaders.Add("X-Api-Key", "demo-api-key-12345");
                using var products = await client.GetAsync("/api/v1/products?page=1&pageSize=1");
                products.EnsureSuccessStatusCode();
                var payload = await products.Content.ReadFromJsonAsync<JsonElement>();
                productId = payload.GetProperty("items")[0].GetProperty("productId").GetGuid();
            }

            using (var restartedHost = TestApiFactory.Create(databasePath, deleteOnDispose: false))
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
            if (File.Exists(databasePath)) File.Delete(databasePath);
            if (File.Exists($"{databasePath}-shm")) File.Delete($"{databasePath}-shm");
            if (File.Exists($"{databasePath}-wal")) File.Delete($"{databasePath}-wal");
            if (Directory.Exists($"{databasePath}.keys")) Directory.Delete($"{databasePath}.keys", recursive: true);
        }
    }

    private static string CreateTotp(string secret)
    {
        var key = DecodeBase32(secret);
        var counter = new byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(counter, DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        var digest = HMACSHA1.HashData(key, counter);
        var offset = digest[^1] & 0x0f;
        var value = ((digest[offset] & 0x7f) << 24)
            | ((digest[offset + 1] & 0xff) << 16)
            | ((digest[offset + 2] & 0xff) << 8)
            | (digest[offset + 3] & 0xff);
        return (value % 1_000_000).ToString("D6");
    }

    private static byte[] DecodeBase32(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new List<byte>();
        var buffer = 0;
        var bits = 0;
        foreach (var character in value)
        {
            buffer = (buffer << 5) | alphabet.IndexOf(character);
            bits += 5;
            if (bits >= 8)
            {
                output.Add((byte)(buffer >> (bits - 8)));
                bits -= 8;
            }
        }

        return output.ToArray();
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

    private sealed class MfaSetupPayload
    {
        public string Secret { get; set; } = string.Empty;
    }

    private sealed class MfaEnabledPayload
    {
        public List<string> RecoveryCodes { get; set; } = [];
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
    private readonly string _databasePath;
    private readonly bool _deleteOnDispose;

    public TestApiFactory() : this(Path.Combine(Path.GetTempPath(), $"ecommerce-tests-{Guid.NewGuid():N}.db"), deleteOnDispose: true)
    {
    }

    private TestApiFactory(string databasePath, bool deleteOnDispose)
    {
        SQLitePCL.Batteries_V2.Init();
        _databasePath = databasePath;
        _deleteOnDispose = deleteOnDispose;
    }

    public static TestApiFactory Create(string databasePath, bool deleteOnDispose) =>
        new(databasePath, deleteOnDispose);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:EcommerceDb"] = "Host=localhost;Database=ecommerce_test;Username=postgres",
                ["DataProtection:KeyRingPath"] = $"{_databasePath}.keys",
                ["Database:EnsureCreated"] = "true"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={_databasePath};Pooling=False"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!_deleteOnDispose) return;
        if (File.Exists(_databasePath)) File.Delete(_databasePath);
        if (File.Exists($"{_databasePath}-shm")) File.Delete($"{_databasePath}-shm");
        if (File.Exists($"{_databasePath}-wal")) File.Delete($"{_databasePath}-wal");
        if (Directory.Exists($"{_databasePath}.keys")) Directory.Delete($"{_databasePath}.keys", recursive: true);
    }
}
