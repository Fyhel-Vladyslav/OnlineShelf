using OutfitOfferService.OutfitOfferService.Common.Enums;
using FastEndpoints;
using OutfitOfferService.src.OutfitOfferService.Common;
using OutfitOfferService.src.OutfitOfferService.Host.Features.Services;

namespace OutfitOfferService.src.OutfitOfferService.Host.Features.GetWeather
{
    sealed record GetCurrentWeatherResponse(Weather currentWeather);
    class GetCurrentWeather : EndpointWithoutRequest<GetCurrentWeatherResponse>
    {
        private readonly WeatherMappingExtension _weatherMapper;
        private readonly IWeatherService _weatherService;

        public GetCurrentWeather(WeatherMappingExtension weatherMapper, IWeatherService weatherService)
        {
            _weatherMapper = weatherMapper;
            _weatherService = weatherService;
        }

        public override void Configure()
        {
            Get(ApiRoutes.GetWeather);
            AllowAnonymous();
            //Policies("AdminPolicy");      
        }

        public override async Task<GetCurrentWeatherResponse> ExecuteAsync(CancellationToken ct)
        {
            var currentWeatherUnmapped = await _weatherService.GetCurrentWeatherAsync(50.61, 26.25, ct);

            var currentWeather = _weatherMapper.MapToCondition(currentWeatherUnmapped);
            return await Task.FromResult(new GetCurrentWeatherResponse(currentWeather));
        }
    }

}