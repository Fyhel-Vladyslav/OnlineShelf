using OutfitOfferService.OutfitOfferService.Common.DTOs;

namespace OutfitOfferService.src.OutfitOfferService.Host.Features.Services;

using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using static FastEndpoints.Ep;

public interface IWeatherService
{
    Task<string> GetCurrentWeatherAsync(double lat, double lon, CancellationToken ct = default);
}

public class WeatherService : IWeatherService
{
    private readonly IWeatherClient _weatherClient;
    private readonly IDistributedCache _cache;

    public WeatherService(IWeatherClient weatherClient, IDistributedCache cache)
    {
        _weatherClient = weatherClient;
        _cache = cache;
    }

    public async Task<string> GetCurrentWeatherAsync(double lat, double lon, CancellationToken ct = default)
    {
        var trimedLat = Math.Round(lat, 2);
        var trimedLon = Math.Round(lon, 2);


        string cacheKey = $"weather:{trimedLat},{trimedLon}";

        var cachedData = await _cache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrEmpty(cachedData))
        {
            return JsonSerializer.Deserialize<string>(cachedData)!;
        }

        var weather = await _weatherClient.FetchWeatherAsync(trimedLat, trimedLon, ct);

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
        };

        var serializedData = JsonSerializer.Serialize(weather);
        await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions, ct);

        return weather;
    }
}