using OutfitNetworkService.src.OutfitNetworkService.Repository.EfCore.Entities;

namespace OutfitNetworkService.src.OutfitNetworkService.Common.Interfaces;

/// <summary>
/// Сирий результат GNN-інференсу до застосування мультиплікативних штрафів.
/// PairwiseScores зберігається для explainability (пояснення користувачу/дизайнеру,
/// які саме пари "тягнуть" фінальний скор вниз).
/// </summary>
public sealed record GraphScoringResult(
    double GraphLevelScore,
    IReadOnlyDictionary<(string SourceItemId, string TargetItemId), double> PairwiseScores);

/// <summary>
/// Абстракція над GNN-скорером сумісності. Дозволяє підміняти реалізацію
/// (ONNX-модель, mock для юніт-тестів, майбутня версія моделі) без зміни OfferNetworkService.
/// </summary>
public interface IGraphCompatibilityScorer
{
    Task<GraphScoringResult> ScoreAsync(OutfitGraph graph, CancellationToken cancellationToken = default);
}
