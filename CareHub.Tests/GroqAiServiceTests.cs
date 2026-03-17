using System.Net;
using System.Text;
using System.Text.Json;
using CareHub.Api.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CareHub.Tests;

public class GroqAiServiceTests
{
    private static GroqAiService CreateService(HttpMessageHandler handler, string? model = null)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.groq.test/") };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(model != null
                ? new Dictionary<string, string?> { ["Groq:Model"] = model }
                : [])
            .Build();
        return new GroqAiService(http, config);
    }

    [Fact]
    public async Task CompleteAsync_ParsesValidResponse()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content = "Hello from Groq!" } }
            }
        });

        var handler = new FakeHandler(responseJson);
        var service = CreateService(handler);

        var result = await service.CompleteAsync("You are helpful.", "Say hi");

        Assert.Equal("Hello from Groq!", result);
    }

    [Fact]
    public async Task CompleteAsync_SendsCorrectRequestBody()
    {
        var handler = new FakeHandler(JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content = "ok" } } }
        }));

        var service = CreateService(handler, model: "llama-3.3-70b-versatile");

        await service.CompleteAsync("system prompt", "user prompt");

        Assert.NotNull(handler.LastRequestBody);
        var body = JsonSerializer.Deserialize<JsonElement>(handler.LastRequestBody);

        Assert.Equal("llama-3.3-70b-versatile", body.GetProperty("model").GetString());

        var messages = body.GetProperty("messages");
        Assert.Equal(2, messages.GetArrayLength());
        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Equal("system prompt", messages[0].GetProperty("content").GetString());
        Assert.Equal("user", messages[1].GetProperty("role").GetString());
        Assert.Equal("user prompt", messages[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task CompleteAsync_UsesDefaultModel_WhenNotConfigured()
    {
        var handler = new FakeHandler(JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content = "ok" } } }
        }));

        var service = CreateService(handler, model: null);
        await service.CompleteAsync("sys", "usr");

        var body = JsonSerializer.Deserialize<JsonElement>(handler.LastRequestBody!);
        Assert.Equal("llama-3.3-70b-versatile", body.GetProperty("model").GetString());
    }

    [Fact]
    public async Task CompleteAsync_ThrowsOnHttpError()
    {
        var handler = new FakeHandler("error", HttpStatusCode.TooManyRequests);
        var service = CreateService(handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.CompleteAsync("sys", "usr"));
    }

    [Fact]
    public async Task CompleteAsync_PostsToCorrectEndpoint()
    {
        var handler = new FakeHandler(JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content = "ok" } } }
        }));

        var service = CreateService(handler);
        await service.CompleteAsync("sys", "usr");

        Assert.Equal("https://api.groq.test/openai/v1/chat/completions", handler.LastRequestUri?.ToString());
    }

    /// <summary>Simple HttpMessageHandler that returns a canned response.</summary>
    private sealed class FakeHandler : HttpMessageHandler
    {
        private readonly string _responseBody;
        private readonly HttpStatusCode _statusCode;

        public string? LastRequestBody { get; private set; }
        public Uri? LastRequestUri { get; private set; }

        public FakeHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responseBody = responseBody;
            _statusCode = statusCode;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            if (request.Content != null)
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
