using OutfitOfferService.OutfitOfferService.Common.DTOs;

namespace OutfitOfferService.src.OutfitOfferService.Host.Features.Services;

using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

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
        string cacheKey = $"weather:{lat},{lon}";

        // 1. Шукаємо в Redis
        var cachedData = await _cache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrEmpty(cachedData))
        {
            return JsonSerializer.Deserialize<string>(cachedData)!;
        }

        // 2. Якщо в кеші немає — йдемо в API
        var weather = await _weatherClient.FetchWeatherAsync(lat, lon, ct);

        // 3. Зберігаємо в Redis (наприклад, на 15 хвилин)
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
        };

        var serializedData = JsonSerializer.Serialize(weather);
        await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions, ct);

        return weather;
    }
}