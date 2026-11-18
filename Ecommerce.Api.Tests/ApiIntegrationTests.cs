// Imports JSON support for decoding paginated API responses.
using System.Net.Http.Json;
// Imports the test HTTP factory used to start the API in memory.
using Microsoft.AspNetCore.Mvc.Testing;
// Imports xUnit test attributes and assertions.
using Xunit;

// Hosts API integration tests in an isolated WebApplicationFactory process.
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    // Stores the factory so every test receives the same application host.
    private readonly WebApplicationFactory<Program> _factory;

    // Creates the test fixture and supplies the application factory.
    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
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

    // Defines only the fields needed to verify the paginated product contract.
    private sealed class ProductPayload
    {
        // Holds the returned product collection.
        public List<object> Items { get; set; } = [];
        // Holds the total matching record count.
        public int TotalCount { get; set; }
        // Holds the requested page number.
        public int Page { get; set; }
    }
}
