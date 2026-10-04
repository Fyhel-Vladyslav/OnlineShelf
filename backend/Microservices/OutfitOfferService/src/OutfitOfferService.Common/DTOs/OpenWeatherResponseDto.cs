using System.Text.Json.Serialization;

namespace OutfitOfferService.src.OutfitOfferService.Common.DTOs;

/// <summary>Відповідь OpenWeatherMap /data/2.5/weather (лише потрібні поля).</summary>
public class OpenWeatherResponseDto
{
    [JsonPropertyName("timezone")]
    public int Timezone { get; set; }

    [JsonPropertyName("weather")]
    public WeatherCondition[] Weather { get; set; } = [];

    [JsonPropertyName("main")]
    public MainWeatherData? Main { get; set; }
}

public class WeatherCondition
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("main")]
    public string Main { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string Icon { get; set; } = string.Empty;
}

public class MainWeatherData
{
    /// <summary>Температура, °C (запит з units=metric).</summary>
    [JsonPropertyName("temp")]
    public double Temp { get; set; }

    [JsonPropertyName("feels_like")]
    public double FeelsLike { get; set; }
}
