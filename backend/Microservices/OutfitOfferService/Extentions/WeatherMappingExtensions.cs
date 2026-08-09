using OutfitOfferService.OutfitOfferService.Common.Enums;

namespace OutfitOfferService.Extentions;

public class WeatherMappingExtensions
{
    public Weather MapToCondition(            
    )
    {
        var weather = new Weather();
        return Weather.Clear;
    }
}