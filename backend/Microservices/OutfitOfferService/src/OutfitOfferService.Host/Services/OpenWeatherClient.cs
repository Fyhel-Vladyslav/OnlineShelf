using System.Net.Http.Json;
using OutfitOfferService.OutfitOfferService.Common.DTOs;

namespace OutfitOfferService.OutfitOfferService.Host.Services;

public interface IWeatherClient
{
    Task<CurrentWeatherDto> FetchWeatherAsync(string city, CancellationToken ct = default);
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

    public async Task<CurrentWeatherDto> FetchWeatherAsync(string city, CancellationToken ct = default)
    {
        string _openWeatherApiKey = _configuration["OpenWeatherApiKey"] ;
        // Запит до безкоштовного API
        var response = await _httpClient.GetFromJsonAsync<OpenWeatherResponseDto>(
            $"data/2.5/weather?q={city}&appid={_openWeatherApiKey}&units=metric", ct);

        /*
        if (response == null || response.Weather.Count == 0)
            throw new Exception("Дані про погоду недоступні");
            */

        var firstWeather = response.Weather[0];

        // Мапінг у вашу внутрішню DTO (як обговорювали раніше)
        return new CurrentWeatherDto(
            temperatureC: "",
            condition: "",
            description: ""
        );
    }
    
    private class OpenWeatherResponseDto()
    {
        public int[] Weather;
    }
}