
using OutfitNetworkService.src.OutfitNetworkService.Common.Interfaces;
using OutfitNetworkService.src.OutfitNetworkService.Repository.EfCore.Entities;

namespace OutfitNetworkService.src.OutfitNetworkService.Host.Services.OutfitCompatibilityScorer;

/// <summary>
/// Тимчасова заглушка IGraphCompatibilityScorer для локальної розробки/тестів,
/// поки немає реального натренованого .onnx. Повертає фіксований базовий скор,
/// щоб решта пайплайну (candidate generation, penalty rules, endpoint) можна було
/// розробляти й дебажити незалежно від готовності ML-моделі.
/// Реєструвати ЛИШЕ в Development — ніколи в Production.
/// </summary>
public sealed class StubGraphCompatibilityScorer : IGraphCompatibilityScorer
{
    private const double PlaceholderPairwiseScore = 0.75;

    public Task<GraphScoringResult> ScoreAsync(OutfitGraph graph, CancellationToken cancellationToken = default)
    {
        var pairwise = graph.Edges.ToDictionary(
            e => (graph.Nodes[e.Source].ItemId, graph.Nodes[e.Target].ItemId),
            _ => PlaceholderPairwiseScore);

        return Task.FromResult(new GraphScoringResult(PlaceholderPairwiseScore, pairwise));
    }
}
