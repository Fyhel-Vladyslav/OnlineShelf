using System.Globalization;
using System.Net.Http.Json;
using OutfitOfferService.OutfitOfferService.Common.DTOs;
using OutfitOfferService.src.OutfitOfferService.Common.DTOs;

namespace OutfitOfferService.src.OutfitOfferService.Host.Features.Services;

public interface IWeatherClient
{
    Task<string> FetchWeatherAsync(double lat, double lon, CancellationToken ct = default);
}

public class OpenWeatherClient : IWeatherClient
{
    private readonly HttpClient _httpClient;
    private IConfiguration _configuration;

    public OpenWeatherClient(
        HttpClient httpClient,
        IConfiguration configuration
    )
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<string> FetchWeatherAsync(double lat, double lon, CancellationToken ct = default)
    {
        string apiKey = _configuration["OpenWeatherApiKey"];

        // Форматуємо координати з крапкою (InvariantCulture)
        string latStr = lat.ToString(CultureInfo.InvariantCulture);
        string lonStr = lon.ToString(CultureInfo.InvariantCulture);

        // Новий URL за твоєю документацією
        string url = $"/data/2.5/weather?lat={latStr}&lon={lonStr}&appid={apiKey}&units=metric";

        var response = await _httpClient.GetFromJsonAsync<OpenWeatherResponseDto>(url, ct);

        // Використовуємо DTO, яке ми створили в попередньому кроці (з масивом Data)
        if (response?.Weather == null || response.Weather.Length == 0)
        {
            throw new Exception("Дані про погоду недоступні");
        }

        var weatherCondition = response.Weather[0];
        return weatherCondition.Main; // Або weatherCondition.Description, якщо треба детальніше
    }
}