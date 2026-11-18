namespace OutfitOfferService.src.OutfitOfferService.Common.Interfaces
{
    //public interface IRequestContextBuilder
    //{
    //    Task<RequestContext> BuildAsync(Guid userId, Occasion occasion, CancellationToken ct);
    //}

    //public sealed class RequestContextBuilder : IRequestContextBuilder
    //{
    //    private readonly IUserClient _userClient;
    //    private readonly IWeatherProvider _weatherProvider; // кешований, окремо

    //    public async Task<RequestContext> BuildAsync(Guid userId, Occasion occasion, CancellationToken ct)
    //    {
    //        var location = await _userClient.GetUserLocationAsync(userId, ct);
    //        var weather = await _weatherProvider.GetCurrentAsync(location, ct);

    //        return new RequestContext
    //        {
    //            Weather = weather,
    //            Occasion = occasion,
    //            Season = DeriveSeason(weather, DateTime.UtcNow),
    //            RequestedAt = Timestamp.FromDateTime(DateTime.UtcNow)
    //        };
    //    }
    //}
}
