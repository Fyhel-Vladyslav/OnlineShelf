using FastEndpoints;
using Grpc.Core;
using OutfitOfferService.OutfitOfferService.Common.Enums;
using OutfitOfferService.src.OutfitOfferService.Common;
using OutfitOfferService.src.OutfitOfferService.Common.Models;
using OutfitOfferService.src.OutfitOfferService.Host.Services.OutfitGeneration;

namespace OutfitOfferService.OutfitOfferService.Host.Features.GenerateOutfit;

public sealed record GenerateOutfitRequest
{
    /// <summary>Тимчасово з тіла запиту: коли додамо JWT, userId братиметься з токена.</summary>
    public Guid UserId { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public List<Guid> IncludeItemIds { get; init; } = [];
    public List<Guid> ExcludeItemIds { get; init; } = [];
    public bool OnlyFavorite { get; init; }
    public int? TopN { get; init; }
}

public sealed record OutfitItemResponse(Guid ItemId, string Name, string Slot, string? BigImage, bool IsVirtual);

public sealed record OutfitResponse(
    List<OutfitItemResponse> Items,
    double FinalScore,
    double GraphLevelScore,
    double PenaltyMultiplier,
    IReadOnlyDictionary<string, double> AppliedPenalties,
    IReadOnlyList<PairwiseScore> PairwiseScores);

public sealed record WeatherResponse(Weather Condition, double TemperatureCelsius);

public sealed record GenerateOutfitResponse(
    List<OutfitResponse> Outfits,
    WeatherResponse? Weather,
    int SeasonPhase,
    IReadOnlyList<string> Warnings,
    long ElapsedMs);

public sealed class GenerateOutfitEndpoint(IOutfitOfferOrchestrator orchestrator) : Endpoint<GenerateOutfitRequest, GenerateOutfitResponse>
{
    public override void Configure()
    {
        Post(ApiRoutes.GenerateOutfits);
        AllowAnonymous(); // авторизацію додамо разом для всіх сервісів
    }

    public override async Task HandleAsync(GenerateOutfitRequest req, CancellationToken ct)
    {
        if (req.UserId == Guid.Empty)
        {
            AddError(r => r.UserId, "UserId is required.");
        }
        if (req.Latitude.HasValue != req.Longitude.HasValue)
        {
            AddError("Latitude and Longitude must be provided together.");
        }
        ThrowIfAnyErrors();

        var startedAt = TimeProvider.System.GetTimestamp();
        OfferResult result;
        try
        {
            result = await orchestrator.GenerateAsync(new GenerateOfferCommand
            {
                UserId = req.UserId,
                Latitude = req.Latitude,
                Longitude = req.Longitude,
                IncludeItemIds = req.IncludeItemIds,
                ExcludeItemIds = req.ExcludeItemIds,
                OnlyFavorite = req.OnlyFavorite,
                TopN = req.TopN,
            }, ct);
        }
        catch (RpcException ex)
        {
            Logger.LogError(ex, "Downstream gRPC call failed while generating outfits");
            await Send.ResultAsync(TypedResults.Problem(
                detail: $"Downstream service is unavailable: {ex.Status.Detail}",
                statusCode: StatusCodes.Status503ServiceUnavailable));
            return;
        }

        if (!result.IsSuccess)
        {
            foreach (var error in result.Errors)
            {
                AddError(error);
            }
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        var elapsedMs = (long)TimeProvider.System.GetElapsedTime(startedAt).TotalMilliseconds;
        await Send.OkAsync(new GenerateOutfitResponse(
            result.Outfits.Select(ToResponse).ToList(),
            result.Weather is { } w ? new WeatherResponse(w.Condition, w.TemperatureCelsius) : null,
            result.SeasonPhase,
            result.Warnings,
            elapsedMs), ct);
    }

    private static OutfitResponse ToResponse(ScoredOutfit outfit) => new(
        outfit.Items
            .Select(i => new OutfitItemResponse(i.Item.Id, i.Item.Name, i.Slot.ToString(), i.Item.BigImage, i.Item.IsVirtual))
            .ToList(),
        outfit.Score.FinalScore,
        outfit.Score.GraphLevelScore,
        outfit.Score.PenaltyMultiplier,
        outfit.Score.AppliedPenalties,
        outfit.Score.PairwiseScores);
}
