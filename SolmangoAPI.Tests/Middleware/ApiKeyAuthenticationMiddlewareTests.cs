using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using SolmangoAPI.Middleware;
using Xunit;

namespace SolmangoAPI.Tests.Middleware;

public class ApiKeyAuthenticationMiddlewareTests
{
    [Fact]
    public async void ShouldAuthorizeRequest()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        using var app = builder.Build();
        app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
        app.MapGet("/allow_anonymus", () => Results.Ok()).AllowAnonymous();
        app.MapGet("/api_key_required", () => Results.Ok());
        await app.StartAsync();

        HttpClient testClient = app.GetTestClient();
        var response = await testClient.GetAsync("/allow_anonymus");
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        string[] apiKeys = app.Configuration.GetSection("Security:ApiKeys").Get<string[]>();
        foreach (string apiKey in apiKeys)
        {
            testClient.DefaultRequestHeaders.Clear();
            testClient.DefaultRequestHeaders.Add("Api-Key", apiKey);
            response = await testClient.GetAsync("/api_key_required");
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async void ShouldNotAuthorizeRequest()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        using var app = builder.Build();
        app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
        app.MapGet("/api_key_required", () => Results.Ok());
        await app.StartAsync();

        HttpClient testClient = app.GetTestClient();
        var response = await testClient.GetAsync("/api_key_required");
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);

        testClient.DefaultRequestHeaders.Clear();
        testClient.DefaultRequestHeaders.Add("Api-Key", "invalid_dummy_key");
        response = await testClient.GetAsync("/api_key_required");
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }
}