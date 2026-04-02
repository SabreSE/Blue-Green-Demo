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

    [Fact]
    public async Task HomePage_ReturnsHtmlSuccess()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Titan Blue/Green Deployment Demo", body);
        Assert.Contains("<html", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Info_ReturnsExpectedContract()
    {
        using var client = _factory.CreateClient();
        var payload = await client.GetFromJsonAsync<InfoResponse>("/info");

        Assert.NotNull(payload);
        Assert.Equal("TitanDemo.Api", payload!.Service);
        Assert.False(string.IsNullOrWhiteSpace(payload.Version));
        Assert.False(string.IsNullOrWhiteSpace(payload.ImageTag));
        Assert.False(string.IsNullOrWhiteSpace(payload.CommitSha));
        Assert.False(string.IsNullOrWhiteSpace(payload.Color));
    }

    private sealed record RootResponse(string Message, string Color, DateTimeOffset Utc);
    private sealed record InfoResponse(string Service, string Version, string ImageTag, string CommitSha, string Color);
}
