using OutfitOfferService.OutfitOfferService.Common.Enums;
using FastEndpoints;
using OutfitOfferService.src.OutfitOfferService.Common;
using OutfitOfferService.src.OutfitOfferService.Host.Features.Services;

namespace OutfitOfferService.src.OutfitOfferService.Host.Features.GetWeather
{
    // Властивості, а не поля: FastEndpoints біндить query-параметри лише у властивості
    sealed record GetCurrentWeatherRequest
    {
        public double Latitude { get; init; }
        public double Longitude { get; init; }
    }

    sealed record GetCurrentWeatherResponse(Weather CurrentWeather, double TemperatureCelsius);

    class GetCurrentWeather : Endpoint<GetCurrentWeatherRequest, GetCurrentWeatherResponse>
    {
        private readonly IWeatherService _weatherService;

        public GetCurrentWeather(IWeatherService weatherService)
        {
            _weatherService = weatherService;
        }

        public override void Configure()
        {
            Get(ApiRoutes.GetWeather);
            AllowAnonymous();
        }

        public override async Task HandleAsync(GetCurrentWeatherRequest req, CancellationToken ct)
        {
            try
            {
                var weather = await _weatherService.GetCurrentWeatherAsync(req.Latitude, req.Longitude, ct);
                await Send.OkAsync(new GetCurrentWeatherResponse(weather.Condition, weather.TemperatureCelsius), ct);
            }
            catch (WeatherUnavailableException ex)
            {
                Logger.LogError(ex, "Weather is unavailable");
                await Send.ResultAsync(TypedResults.Problem(
                    detail: $"Weather service is unavailable: {ex.Message}",
                    statusCode: StatusCodes.Status503ServiceUnavailable));
            }
        }
    }
}
