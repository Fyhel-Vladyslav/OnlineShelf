using OutfitNetworkService.src.OutfitNetworkService.Common.Interfaces;
using OutfitNetworkService.src.OutfitNetworkService.Host.Services.PenaltyCalculator;
using OutfitNetworkService.src.OutfitNetworkService.Repository.EfCore.Entities;
using OrchestratorImpl = OutfitNetworkService.src.OutfitNetworkService.Host.Services.ScoringOrchestrator.ScoringOrchestrator;

namespace OutfitNetworkService.Tests;

public class ScoringOrchestratorTests
{
    private sealed class FakeScorer(double score) : IGraphCompatibilityScorer
    {
        public OutfitGraph? LastGraph { get; private set; }

        public Task<GraphScoringResult> ScoreAsync(OutfitGraph graph, CancellationToken cancellationToken = default)
        {
            LastGraph = graph;
            var pairwise = graph.Edges
                .Select((_, e) => graph.GetEdgeNodes(e))
                .ToDictionary(p => (p.Source.ItemId, p.Target.ItemId), _ => score);
            return Task.FromResult(new GraphScoringResult(score, pairwise));
        }
    }

    private sealed class FixedRule(string name, double factor) : IPenaltyRule
    {
        public string Name => name;
        public double Evaluate(OutfitGraph graph) => factor;
    }

    [Fact]
    public async Task EvaluateAsync_FinalScoreIsGraphScoreTimesPenalties()
    {
        var scorer = new FakeScorer(0.8);
        var penalties = new MultiplicativePenaltyCalculator([new FixedRule("a", 0.5), new FixedRule("b", 0.9)]);
        var orchestrator = new OrchestratorImpl(scorer, penalties);

        var result = await orchestrator.EvaluateAsync(TestData.Outfit());

        Assert.Equal(0.8, result.GraphLevelScore, 9);
        Assert.Equal(0.45, result.PenaltyMultiplier, 9);
        Assert.Equal(0.36, result.FinalScore, 9);
        Assert.Equal(2, result.AppliedPenalties.Count);
    }

    [Fact]
    public async Task EvaluateAsync_BuildsCompleteGraphAndKeepsPairwiseScores()
    {
        var scorer = new FakeScorer(0.7);
        var orchestrator = new OrchestratorImpl(scorer, new MultiplicativePenaltyCalculator([]));

        var result = await orchestrator.EvaluateAsync(TestData.Outfit());

        Assert.NotNull(scorer.LastGraph);
        Assert.Equal(3 * 2, scorer.LastGraph!.Edges.Count);
        Assert.Equal(6, result.PairwiseScores.Count);
        Assert.Equal(0.7, result.FinalScore, 9);
    }

    [Fact]
    public async Task EvaluateAsync_WithTinyOnnxModel_ReturnsUnitRangeScore()
    {
        using var scorer = TestData.TinyScorer();
        var orchestrator = new OrchestratorImpl(scorer, new MultiplicativePenaltyCalculator([new FixedRule("a", 0.5)]));

        var result = await orchestrator.EvaluateAsync(TestData.Outfit());

        Assert.InRange(result.FinalScore, 0.0, 0.5);
        Assert.Equal(result.GraphLevelScore * 0.5, result.FinalScore, 9);
    }
}
