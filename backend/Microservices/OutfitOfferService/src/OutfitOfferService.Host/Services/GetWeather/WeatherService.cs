using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using OutfitOfferService.src.OutfitOfferService.Common.Models;
using OutfitOfferService.src.OutfitOfferService.Host.Features.GetWeather;

namespace OutfitOfferService.src.OutfitOfferService.Host.Features.Services;

public sealed class WeatherOptions
{
    public const string SectionName = "Weather";

    /// <summary>Час життя запису в кеші погоди.</summary>
    public int CacheTtlMinutes { get; set; } = 30;

    /// <summary>Округлення координат для ключа кешу (2 знаки ≈ 1 км): сусідні запити ділять один запис.</summary>
    public int CoordinateDecimals { get; set; } = 2;
}

public interface IWeatherService
{
    Task<WeatherSnapshot> GetCurrentWeatherAsync(double lat, double lon, CancellationToken ct = default);
}

/// <summary>
/// Погода з кешем. Координати не зберігаються ніде, крім ключа in-memory кешу (NFR-2).
/// SemaphoreSlim на ключ захищає від thundering herd: при промаху кешу в провайдера йде лише один запит.
/// </summary>
public class WeatherService : IWeatherService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    private readonly IWeatherClient _weatherClient;
    private readonly IDistributedCache _cache;
    private readonly WeatherMappingExtension _mapper;
    private readonly WeatherOptions _options;

    public WeatherService(
        IWeatherClient weatherClient,
        IDistributedCache cache,
        WeatherMappingExtension mapper,
        IOptions<WeatherOptions> options)
    {
        _weatherClient = weatherClient;
        _cache = cache;
        _mapper = mapper;
        _options = options.Value;
    }

    public async Task<WeatherSnapshot> GetCurrentWeatherAsync(double lat, double lon, CancellationToken ct = default)
    {
        var roundedLat = Math.Round(lat, _options.CoordinateDecimals);
        var roundedLon = Math.Round(lon, _options.CoordinateDecimals);
        var cacheKey = $"weather:{roundedLat},{roundedLon}";

        var cached = await TryGetCachedAsync(cacheKey, ct);
        if (cached is not null)
        {
            return cached;
        }

        var gate = Locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            // Поки чекали, інший запит міг уже заповнити кеш
            cached = await TryGetCachedAsync(cacheKey, ct);
            if (cached is not null)
            {
                return cached;
            }

            var raw = await _weatherClient.FetchWeatherAsync(roundedLat, roundedLon, ct);
            var snapshot = new WeatherSnapshot(_mapper.MapToCondition(raw.Weather[0].Main), raw.Main!.Temp);

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(snapshot),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheTtlMinutes)
                },
                ct);

            return snapshot;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<WeatherSnapshot?> TryGetCachedAsync(string cacheKey, CancellationToken ct)
    {
        var cachedData = await _cache.GetStringAsync(cacheKey, ct);
        return string.IsNullOrEmpty(cachedData) ? null : JsonSerializer.Deserialize<WeatherSnapshot>(cachedData);
    }
}
