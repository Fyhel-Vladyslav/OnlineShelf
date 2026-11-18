using OutfitNetworkService.src.OutfitNetworkService.Host.Services.OutfitCompatibilityScorer;
using OutfitNetworkService.src.OutfitNetworkService.Repository.EfCore.Entities;

namespace OutfitNetworkService.Tests;

public class OnnxGraphCompatibilityScorerTests
{
    private const double Tolerance = 1e-5;

    [Fact]
    public void Constructor_ReadsEmbeddingContractFromMetadata()
    {
        using var scorer = TestData.TinyScorer();

        Assert.Equal(TestData.EmbeddingDim, scorer.EmbeddingDim);
        Assert.Equal(TestData.ClipModel, scorer.EmbeddingModel);
        Assert.Equal("tiny-random", scorer.ModelVersion);
    }

    [Fact]
    public async Task ScoreAsync_ReturnsScoresInUnitRange_ForEveryOrderedPair()
    {
        using var scorer = TestData.TinyScorer();
        var graph = OutfitGraph.CreateComplete(TestData.Outfit());

        var result = await scorer.ScoreAsync(graph);

        Assert.InRange(result.GraphLevelScore, 0.0, 1.0);
        Assert.Equal(graph.Edges.Count, result.PairwiseScores.Count);
        Assert.All(result.PairwiseScores.Values, s => Assert.InRange(s, 0.0, 1.0));
    }

    [Fact]
    public async Task ScoreAsync_PairwiseScoresAreSymmetric()
    {
        using var scorer = TestData.TinyScorer();

        var result = await scorer.ScoreAsync(OutfitGraph.CreateComplete(TestData.Outfit()));

        foreach (var ((a, b), score) in result.PairwiseScores)
            Assert.Equal(score, result.PairwiseScores[(b, a)], Tolerance);
    }

    [Fact]
    public async Task ScoreAsync_WorksWhenNoNodeHasEmbedding()
    {
        // Раніше розмірність бралась як Max(FeatureVector.Length) = 0, і модель падала
        using var scorer = TestData.TinyScorer();
        var graph = OutfitGraph.CreateComplete([TestData.Node("top", 1), TestData.Node("bottom", 2), TestData.Node("shoes", 4)]);

        var result = await scorer.ScoreAsync(graph);

        Assert.InRange(result.GraphLevelScore, 0.0, 1.0);
    }

    [Fact]
    public async Task ScoreAsync_ForeignEmbeddingModel_IsTreatedAsMissing()
    {
        using var scorer = TestData.TinyScorer();
        var withoutVector = TestData.Outfit();
        withoutVector[1] = TestData.Node("bottom", 2);
        var foreignVector = TestData.Outfit();
        foreignVector[1] = TestData.Node("bottom", 2, TestData.Embedding(42), embeddingModel: "patrickjohncyh/fashion-clip");

        var expected = await scorer.ScoreAsync(OutfitGraph.CreateComplete(withoutVector));
        var actual = await scorer.ScoreAsync(OutfitGraph.CreateComplete(foreignVector));

        Assert.False(scorer.CanUseEmbedding(foreignVector[1]));
        Assert.Equal(expected.GraphLevelScore, actual.GraphLevelScore, Tolerance);
    }

    [Fact]
    public async Task ScoreAsync_WrongDimensionVector_IsTreatedAsMissing()
    {
        using var scorer = TestData.TinyScorer();
        var withoutVector = TestData.Outfit();
        withoutVector[0] = TestData.Node("top", 1);
        var shortVector = TestData.Outfit();
        shortVector[0] = TestData.Node("top", 1, TestData.Embedding(7, dim: 128));

        var expected = await scorer.ScoreAsync(OutfitGraph.CreateComplete(withoutVector));
        var actual = await scorer.ScoreAsync(OutfitGraph.CreateComplete(shortVector));

        Assert.Equal(expected.GraphLevelScore, actual.GraphLevelScore, Tolerance);
    }

    [Fact]
    public void CanUseEmbedding_AcceptsEmptyEmbeddingModel()
    {
        // Речі, збережені до появи поля EmbeddingModel, вважаються порахованими моделлю за замовчуванням
        using var scorer = TestData.TinyScorer();

        Assert.True(scorer.CanUseEmbedding(TestData.Node("top", 1, TestData.Embedding(1), embeddingModel: "")));
    }

    [Fact]
    public async Task ScoreAsync_EmbeddingChangesScore()
    {
        using var scorer = TestData.TinyScorer();
        var other = TestData.Outfit();
        other[0] = TestData.Node("top", 1, TestData.Embedding(100));

        var a = await scorer.ScoreAsync(OutfitGraph.CreateComplete(TestData.Outfit()));
        var b = await scorer.ScoreAsync(OutfitGraph.CreateComplete(other));

        Assert.NotEqual(a.GraphLevelScore, b.GraphLevelScore, 6);
    }

    [Fact]
    public async Task RealModel_IfPresent_LoadsAndGivesDifferentScores()
    {
        // Models/outfit_gnn.onnx копіюється з OutfitNetworkService у output; без файлу сервіс працює на заглушці
        var path = Path.Combine(AppContext.BaseDirectory, "Models", "outfit_gnn.onnx");
        if (!File.Exists(path))
            return;

        using var scorer = new OnnxGraphCompatibilityScorer(
            Microsoft.Extensions.Options.Options.Create(new GnnScorerOptions { ModelPath = path }));
        var scores = new List<double>();
        for (var seed = 0; seed < 10; seed++)
        {
            var outfit = new List<ItemNode>
            {
                TestData.Node("top", 1, TestData.Embedding(seed * 3 + 1), scorer.EmbeddingModel),
                TestData.Node("bottom", 2, TestData.Embedding(seed * 3 + 2), scorer.EmbeddingModel),
                TestData.Node("shoes", 4, TestData.Embedding(seed * 3 + 3), scorer.EmbeddingModel),
            };
            scores.Add((await scorer.ScoreAsync(OutfitGraph.CreateComplete(outfit))).GraphLevelScore);
        }

        Assert.Equal(TestData.EmbeddingDim, scorer.EmbeddingDim);
        Assert.All(scores, s => Assert.InRange(s, 0.0, 1.0));
        Assert.True(scores.Distinct().Count() > 1);
    }

    [Fact]
    public async Task ScoreAsync_OutOfRangeSlot_FallsBackToUnknown()
    {
        using var scorer = TestData.TinyScorer();
        var outOfRange = TestData.Outfit();
        outOfRange[2] = TestData.Node("shoes", 99, TestData.Embedding(3));
        var unknown = TestData.Outfit();
        unknown[2] = TestData.Node("shoes", 0, TestData.Embedding(3));

        var expected = await scorer.ScoreAsync(OutfitGraph.CreateComplete(unknown));
        var actual = await scorer.ScoreAsync(OutfitGraph.CreateComplete(outOfRange));

        Assert.Equal(expected.GraphLevelScore, actual.GraphLevelScore, Tolerance);
    }
}
