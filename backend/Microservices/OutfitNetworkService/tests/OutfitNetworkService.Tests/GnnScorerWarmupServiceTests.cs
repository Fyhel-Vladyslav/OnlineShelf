using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OutfitNetworkService.src.OutfitNetworkService.Common.Interfaces;
using OutfitNetworkService.src.OutfitNetworkService.Host.Services.OutfitCompatibilityScorer;
using OutfitNetworkService.src.OutfitNetworkService.Host.Services.PenaltyCalculator;
using OutfitNetworkService.src.OutfitNetworkService.Repository.EfCore.Entities;
using OrchestratorImpl = OutfitNetworkService.src.OutfitNetworkService.Host.Services.ScoringOrchestrator.ScoringOrchestrator;

namespace OutfitNetworkService.Tests;

public class GnnScorerWarmupServiceTests
{
    private sealed class CountingScorer : IGraphCompatibilityScorer
    {
        public List<int> GraphSizes { get; } = [];

        public Task<GraphScoringResult> ScoreAsync(OutfitGraph graph, CancellationToken cancellationToken = default)
        {
            GraphSizes.Add(graph.Nodes.Count);
            return Task.FromResult(new GraphScoringResult(0.5, new Dictionary<(string, string), double>()));
        }
    }

    private static GnnScorerWarmupService CreateService(IGraphCompatibilityScorer scorer)
    {
        var services = new ServiceCollection();
        services.AddSingleton(scorer);
        services.AddScoped<IMultiplicativePenaltyCalculator>(_ => new MultiplicativePenaltyCalculator([]));
        services.AddScoped<IScoringOrchestrator, OrchestratorImpl>();
        var provider = services.BuildServiceProvider();
        return new GnnScorerWarmupService(scorer, provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<GnnScorerWarmupService>.Instance);
    }

    [Fact]
    public void Warmup_RunsEveryGraphSize_WithoutChangingLaterScores()
    {
        using var scorer = TestData.TinyScorer();
        var graph = OutfitGraph.CreateComplete(TestData.Outfit());
        var before = scorer.ScoreAsync(graph).Result.GraphLevelScore;

        scorer.Warmup(GnnScorerWarmupService.MaxWarmupNodes);

        Assert.Equal(before, scorer.ScoreAsync(graph).Result.GraphLevelScore, 9);
    }

    [Fact]
    public void WarmupNodes_UseModelEmbeddingContract()
    {
        using var scorer = TestData.TinyScorer();

        var nodes = OnnxGraphCompatibilityScorer.WarmupNodes(4, scorer.EmbeddingDim, scorer.EmbeddingModel);

        Assert.Equal(4, nodes.Select(n => n.ItemId).Distinct().Count());
        Assert.All(nodes, n => Assert.True(scorer.CanUseEmbedding(n)));
        Assert.All(nodes, n => Assert.InRange(n.Slot, 1, OnnxGraphCompatibilityScorer.CategoryVocabSize - 1));
    }

    [Fact]
    public async Task StartAsync_WarmsUpOnnxScorerThroughOrchestrator()
    {
        using var scorer = TestData.TinyScorer();

        await CreateService(scorer).StartAsync(CancellationToken.None);

        var result = await scorer.ScoreAsync(OutfitGraph.CreateComplete(TestData.Outfit()));
        Assert.InRange(result.GraphLevelScore, 0.0, 1.0);
    }

    [Fact]
    public async Task StartAsync_StubScorer_RunsOnlyOrchestratorPath()
    {
        var scorer = new CountingScorer();

        await CreateService(scorer).StartAsync(CancellationToken.None);

        Assert.Equal([3], scorer.GraphSizes);
    }
}
