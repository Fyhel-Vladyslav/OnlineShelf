using FastEndpoints;
using OutfitOfferService.src.OutfitOfferService.Common;
using OutfitOfferService.src.OutfitOfferService.Host.Features.Services;

namespace OutfitOfferService.OutfitOfferService.Host.Features.GenerateOutfit;

public class GenerateOutfit
{
    sealed record GenerateOutfitRequest() 
    { 
        public double longatude;
        public double latitude; 
    };

    sealed record GenerateOutfitResponse();
    class GetCurrentWeather : Endpoint<GenerateOutfitRequest, GenerateOutfitResponse>
    {

        public override void Configure()
        {
            Get(ApiRoutes.GetWeather);
            AllowAnonymous();
            //Policies("AdminPolicy");      
        }

        public override async Task<GenerateOutfitResponse> ExecuteAsync(GenerateOutfitRequest req, CancellationToken ct)
        {
            /*var currentWeatherUnmapped = await _weatherService.GetCurrentWeatherAsync(req.latitude, req.longatude, ct);

            var currentWeather = _weatherMapper.MapToCondition(currentWeatherUnmapped);*/
            var response = new GenerateOutfitResponse();
            return await Task.FromResult(response);
        }
    }
}