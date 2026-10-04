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

        public override async Task<GetCurrentWeatherResponse> ExecuteAsync(GetCurrentWeatherRequest req, CancellationToken ct)
        {
            var weather = await _weatherService.GetCurrentWeatherAsync(req.Latitude, req.Longitude, ct);
            return new GetCurrentWeatherResponse(weather.Condition, weather.TemperatureCelsius);
        }
    }
}
