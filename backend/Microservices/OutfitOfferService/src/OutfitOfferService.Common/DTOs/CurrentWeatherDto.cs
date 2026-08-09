namespace OutfitOfferService.OutfitOfferService.Common.DTOs;

public class CurrentWeatherDto
{
    public CurrentWeatherDto( string temperatureC, string condition, string description)
    {
        TemperatureC = temperatureC;
        Condition = condition;
        Description = description;
    }
        
    public string TemperatureC { get; set; }
    public string Condition { get; set; }
    public string Description { get; set; }

}