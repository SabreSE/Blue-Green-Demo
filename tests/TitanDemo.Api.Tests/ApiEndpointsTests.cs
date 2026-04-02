using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TitanDemo.Api.Tests;

public class ApiEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Root_ReturnsTitanDemoPayload()
    {
        using var client = _factory.CreateClient();
        var payload = await client.GetFromJsonAsync<RootResponse>("/api");

        Assert.NotNull(payload);
        Assert.Equal("Titan Demo API", payload!.Message);
    }

    private sealed record RootResponse(string Message, string Color, DateTimeOffset Utc);
}
