namespace OutfitNetworkService.src.OutfitNetworkService.Host.Services.OutfitCompatibilityScorer;

/// <summary>
/// Фінальний результат оцінки образу: FinalScore = GraphLevelScore * PenaltyMultiplier
/// (тобто S = learned-скор GNN * Π C_j). Розбивка по компонентах зберігається
/// для explainability — щоб згодом можна було показати користувачу/дизайнеру,
/// чому саме такий скор.
/// </summary>
public sealed record OutfitCompatibilityResult(
    double FinalScore,
    double GraphLevelScore,
    double PenaltyMultiplier,
    IReadOnlyDictionary<(string SourceItemId, string TargetItemId), double> PairwiseScores,
    IReadOnlyDictionary<string, double> AppliedPenalties);
