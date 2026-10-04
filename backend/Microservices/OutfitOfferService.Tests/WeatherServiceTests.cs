using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using OutfitOfferService.OutfitOfferService.Common.Enums;
using OutfitOfferService.src.OutfitOfferService.Common.DTOs;
using OutfitOfferService.src.OutfitOfferService.Host.Features.GetWeather;
using OutfitOfferService.src.OutfitOfferService.Host.Features.Services;

namespace OutfitOfferService.Tests;

public class WeatherServiceTests
{
    private sealed class SlowWeatherClient : IWeatherClient
    {
        private int _calls;
        public int Calls => _calls;

        public async Task<OpenWeatherResponseDto> FetchWeatherAsync(double lat, double lon, CancellationToken ct = default)
        {
            Interlocked.Increment(ref _calls);
            await Task.Delay(100, ct);
            return new OpenWeatherResponseDto
            {
                Weather = [new WeatherCondition { Main = "Rain" }],
                Main = new MainWeatherData { Temp = 9.5 },
            };
        }
    }

    private static WeatherService Create(IWeatherClient client) => new(
        client,
        new MemoryDistributedCache(Microsoft.Extensions.Options.Options.Create(new MemoryDistributedCacheOptions())),
        new WeatherMappingExtension(),
        Microsoft.Extensions.Options.Options.Create(new WeatherOptions()));

    [Fact]
    public async Task ConcurrentRequests_ForNearbyCoordinates_HitProviderOnce()
    {
        var client = new SlowWeatherClient();
        var service = Create(client);

        // Координати відрізняються лише в 4-му знаку — після округлення це той самий ключ кешу
        var results = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(i => service.GetCurrentWeatherAsync(50.6199 + i * 0.00001, 26.2516)));

        Assert.Equal(1, client.Calls);
        Assert.All(results, r =>
        {
            Assert.Equal(Weather.Rainy, r.Condition);
            Assert.Equal(9.5, r.TemperatureCelsius);
        });
    }

    [Theory]
    [InlineData("Clear", Weather.Clear)]
    [InlineData("Clouds", Weather.Cloudy)]
    [InlineData("Rain", Weather.Rainy)]
    [InlineData("Drizzle", Weather.Rainy)]
    [InlineData("Snow", Weather.Snowy)]
    [InlineData("Thunderstorm", Weather.Thunderstorm)]
    [InlineData("Mist", Weather.Unknown)]
    public void MapToCondition_MapsOpenWeatherGroups(string group, Weather expected)
    {
        Assert.Equal(expected, new WeatherMappingExtension().MapToCondition(group));
    }
}
