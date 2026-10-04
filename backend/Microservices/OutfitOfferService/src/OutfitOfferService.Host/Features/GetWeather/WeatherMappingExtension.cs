using OutfitOfferService.OutfitOfferService.Common.Enums;

namespace OutfitOfferService.src.OutfitOfferService.Host.Features.GetWeather;

public class WeatherMappingExtension
{
    /// <summary>
    /// Мапінг групи погоди OpenWeatherMap (поле weather[0].main) на внутрішній enum.
    /// Назви груп не збігаються з enum (Clouds/Rain/Snow…), тому Enum.TryParse тут не підходить.
    /// </summary>
    public Weather MapToCondition(string currentWeather) => currentWeather?.Trim().ToLowerInvariant() switch
    {
        "clear" => Weather.Clear,
        "clouds" => Weather.Cloudy,
        "rain" or "drizzle" => Weather.Rainy,
        "snow" => Weather.Snowy,
        "thunderstorm" => Weather.Thunderstorm,
        _ => Enum.TryParse<Weather>(currentWeather, true, out var parsed) ? parsed : Weather.Unknown
    };
}
