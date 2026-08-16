using System.Text.Json.Serialization;

namespace OutfitOfferService.src.OutfitOfferService.Common.DTOs;
public class OpenWeatherResponseDto
{
    // 1. Змінюємо string на int (або видаляємо це поле, якщо воно не треба)
    [JsonPropertyName("timezone")]
    public int Timezone { get; set; }

    // 2. У версії 2.5 масив погоди лежить прямо в корені
    [JsonPropertyName("weather")]
    public WeatherCondition[] Weather { get; set; }

    // (інші поля, якщо ти їх залишив, наприклад temp, тиск тощо)
}

public class WeatherCondition
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("main")]
    public string Main { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; }
}