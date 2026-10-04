using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using OutfitOfferService.src.OutfitOfferService.Common.DTOs;

namespace OutfitOfferService.src.OutfitOfferService.Host.Features.Services;

/// <summary>Погода недоступна з причини, яку видно користувачу/адміну (ключ, провайдер, мережа).</summary>
public sealed class WeatherUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

public interface IWeatherClient
{
    Task<OpenWeatherResponseDto> FetchWeatherAsync(double lat, double lon, CancellationToken ct = default);
}

public class OpenWeatherClient : IWeatherClient
{
    public const string ApiKeySetting = "OpenWeatherApiKey";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OpenWeatherClient(HttpClient httpClient, IConfiguration configuration)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<OpenWeatherResponseDto> FetchWeatherAsync(double lat, double lon, CancellationToken ct = default)
    {
        // Порожній рядок у appsettings.json — так само «не налаштовано», як і відсутній ключ
        var apiKey = _configuration[ApiKeySetting];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new WeatherUnavailableException(
                $"{ApiKeySetting} is not configured (set it via environment variable / backend/Microservices/.env).");
        }

        // Координати з крапкою незалежно від локалі
        string latStr = lat.ToString(CultureInfo.InvariantCulture);
        string lonStr = lon.ToString(CultureInfo.InvariantCulture);

        string url = $"/data/2.5/weather?lat={latStr}&lon={lonStr}&appid={apiKey}&units=metric";

        OpenWeatherResponseDto? response;
        try
        {
            response = await _httpClient.GetFromJsonAsync<OpenWeatherResponseDto>(url, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new WeatherUnavailableException($"OpenWeatherMap rejected {ApiKeySetting} (401 Unauthorized).", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new WeatherUnavailableException($"OpenWeatherMap request failed: {ex.Message}", ex);
        }

        if (response?.Weather == null || response.Weather.Length == 0 || response.Main == null)
        {
            throw new WeatherUnavailableException("OpenWeatherMap returned no weather data.");
        }

        return response;
    }
}
