using System.Globalization;
using System.Net.Http.Json;
using OutfitOfferService.src.OutfitOfferService.Common.DTOs;

namespace OutfitOfferService.src.OutfitOfferService.Host.Features.Services;

public interface IWeatherClient
{
    Task<OpenWeatherResponseDto> FetchWeatherAsync(double lat, double lon, CancellationToken ct = default);
}

public class OpenWeatherClient : IWeatherClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OpenWeatherClient(HttpClient httpClient, IConfiguration configuration)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<OpenWeatherResponseDto> FetchWeatherAsync(double lat, double lon, CancellationToken ct = default)
    {
        string apiKey = _configuration["OpenWeatherApiKey"]
            ?? throw new InvalidOperationException("OpenWeatherApiKey is not configured.");

        // Координати з крапкою незалежно від локалі
        string latStr = lat.ToString(CultureInfo.InvariantCulture);
        string lonStr = lon.ToString(CultureInfo.InvariantCulture);

        string url = $"/data/2.5/weather?lat={latStr}&lon={lonStr}&appid={apiKey}&units=metric";

        var response = await _httpClient.GetFromJsonAsync<OpenWeatherResponseDto>(url, ct);

        if (response?.Weather == null || response.Weather.Length == 0 || response.Main == null)
        {
            throw new InvalidOperationException("Дані про погоду недоступні");
        }

        return response;
    }
}
