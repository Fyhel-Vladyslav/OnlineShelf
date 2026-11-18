using System.Net;
using Microsoft.Extensions.Configuration;
using OutfitOfferService.src.OutfitOfferService.Host.Features.Services;

namespace OutfitOfferService.Tests;

public class OpenWeatherClientTests
{
    private sealed class StubHandler(HttpStatusCode status, string body = "{}") : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }

    private static OpenWeatherClient Create(StubHandler handler, string? apiKey) => new(
        new HttpClient(handler) { BaseAddress = new Uri("https://api.openweathermap.org/") },
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [OpenWeatherClient.ApiKeySetting] = apiKey })
            .Build());

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MissingOrEmptyKey_FailsFast_WithoutCallingProvider(string? apiKey)
    {
        var handler = new StubHandler(HttpStatusCode.OK);

        var ex = await Assert.ThrowsAsync<WeatherUnavailableException>(() => Create(handler, apiKey).FetchWeatherAsync(50.6, 26.2));

        Assert.Contains("not configured", ex.Message);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task RejectedKey_IsReportedAsWeatherUnavailable()
    {
        var ex = await Assert.ThrowsAsync<WeatherUnavailableException>(
            () => Create(new StubHandler(HttpStatusCode.Unauthorized), "bad-key").FetchWeatherAsync(50.6, 26.2));

        Assert.Contains("401", ex.Message);
    }

    [Fact]
    public async Task ValidResponse_IsParsed()
    {
        const string body = """{"weather":[{"main":"Snow"}],"main":{"temp":-3.5}}""";

        var result = await Create(new StubHandler(HttpStatusCode.OK, body), "key").FetchWeatherAsync(50.6, 26.2);

        Assert.Equal("Snow", result.Weather[0].Main);
        Assert.Equal(-3.5, result.Main!.Temp);
    }
}
