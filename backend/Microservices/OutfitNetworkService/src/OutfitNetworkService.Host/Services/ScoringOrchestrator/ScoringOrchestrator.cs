using OutfitNetworkService.src.OutfitNetworkService.Common.Interfaces;
using OutfitNetworkService.src.OutfitNetworkService.Host.Services.OutfitCompatibilityScorer;
using OutfitNetworkService.src.OutfitNetworkService.Host.Services.PenaltyCalculator;
using OutfitNetworkService.src.OutfitNetworkService.Repository.EfCore.Entities;

namespace OutfitNetworkService.src.OutfitNetworkService.Host.Services.ScoringOrchestrator;

/// <summary>
/// Ядро сервісу: приймає кандидата-образ (набір ItemNode від Candidate Generation),
/// будує граф, отримує learned-скор від GNN (замінює ручну зважену суму w_i*x_i)
/// і накладає поверх детерміновані мультиплікативні штрафи Π C_j.
/// </summary>
public sealed class ScoringOrchestrator : IScoringOrchestrator
{
    private readonly IGraphCompatibilityScorer _graphScorer;
    private readonly IMultiplicativePenaltyCalculator _penaltyCalculator;

    public ScoringOrchestrator(
        IGraphCompatibilityScorer graphScorer,
        IMultiplicativePenaltyCalculator penaltyCalculator)
    {
        _graphScorer = graphScorer;
        _penaltyCalculator = penaltyCalculator;
    }

    public async Task<OutfitCompatibilityResult> EvaluateAsync(
        IReadOnlyList<ItemNode> candidateItems,
        CancellationToken cancellationToken = default)
    {
        var graph = OutfitGraph.CreateComplete(candidateItems);

        var scoring = await _graphScorer.ScoreAsync(graph, cancellationToken);
        var penalties = _penaltyCalculator.Calculate(graph);

        var finalScore = scoring.GraphLevelScore * penalties.TotalMultiplier;

        return new OutfitCompatibilityResult(
            FinalScore: finalScore,
            GraphLevelScore: scoring.GraphLevelScore,
            PenaltyMultiplier: penalties.TotalMultiplier,
            PairwiseScores: scoring.PairwiseScores,
            AppliedPenalties: penalties.AppliedRules);
    }
}
