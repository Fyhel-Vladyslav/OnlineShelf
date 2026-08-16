using OutfitOfferService.OutfitOfferService.Common.DTOs;
using OutfitOfferService.OutfitOfferService.Common.Enums;

namespace OutfitOfferService.src.OutfitOfferService.Host.Features.GetWeather;

public class WeatherMappingExtension
{
    public Weather MapToCondition(string currentWeather)
    {
        if (Enum.TryParse<Weather>(currentWeather, true, out var weatherCondition))
        {
            return weatherCondition;
        }
        return Weather.Unknown;
    }
}
