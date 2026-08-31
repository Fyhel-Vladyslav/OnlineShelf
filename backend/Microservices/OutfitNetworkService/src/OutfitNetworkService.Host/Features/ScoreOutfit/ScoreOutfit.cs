using FastEndpoints;
using OutfitNetworkService.src.OutfitNetworkService.Common.DTOs;
using OutfitNetworkService.src.OutfitNetworkService.Common.Interfaces;
using OutfitNetworkService.src.OutfitNetworkService.Host.Services.OfferGeneratorService;
using OutfitNetworkService.src.OutfitNetworkService.Repository.EfCore.Entities;

namespace OutfitNetworkService.src.OutfitNetworkService.Host.Features.ScoreOutfit;

/// <summary>
/// Vertical slice: єдина точка входу для оцінки сумісності кандидата-образу.
/// Уся логіка "фічі" (request/response/endpoint) зібрана поруч в одній папці,
/// а спільна доменна логіка (граф, скорер, штрафи) винесена в Domain/Services,
/// бо буде повторно використовуватись іншими фічами (наприклад, Gap Analysis).
/// </summary>
public sealed class ScoreOutfitEndpoint : Endpoint<ScoreOutfitRequest, ScoreOutfitResponse>
{
    private readonly IOfferGeneratorService _offerGeneratorService;

    public ScoreOutfitEndpoint(IOfferGeneratorService offerGeneratorService)
    {
        _offerGeneratorService = offerGeneratorService;
    }

    public override void Configure()
    {
        Post("/outfit-offer/score");
        AllowAnonymous(); // TODO: замінити на реальну політику авторизації сервісу
    }

    public override async Task HandleAsync(ScoreOutfitRequest req, CancellationToken ct)
    {
        var nodes = req.Items.Select(i => new ItemNode
        {
            ItemId = i.ItemId,
            AttributeType = i.AttributeType,
            IsVirtual = i.IsVirtual,
            IsPinned = i.IsPinned,
            FeatureVector = i.FeatureVector,
            AttributeColorMain = i.AttributeColorMain,
            AttributeColorSecond = i.AttributeColorSecond,
            AttributeSeason = i.AttributeSeason,
            AttributePattern = i.AttributePattern,
            AttributeMatterial = i.AttributeMatterial,
        }).ToList();

        var result = await _offerGeneratorService.EvaluateAsync(nodes, ct);

        await Send.OkAsync(new ScoreOutfitResponse
        {
            FinalScore = result.FinalScore,
            GraphLevelScore = result.GraphLevelScore,
            PenaltyMultiplier = result.PenaltyMultiplier,
            AppliedPenalties = result.AppliedPenalties.ToDictionary(kv => kv.Key, kv => kv.Value),
        }, cancellation: ct);
    }
}
