using Microsoft.Extensions.Options;
using OutfitNetworkService.src.OutfitNetworkService.Host.Services.OutfitCompatibilityScorer;
using OutfitNetworkService.src.OutfitNetworkService.Repository.EfCore.Entities;

namespace OutfitNetworkService.Tests;

internal static class TestData
{
    public const string ClipModel = "openai/clip-vit-base-patch32";
    public const int EmbeddingDim = 512;

    public static string TinyModelPath => Path.Combine(AppContext.BaseDirectory, "Fixtures", "tiny_scorer.onnx");

    public static OnnxGraphCompatibilityScorer TinyScorer() =>
        new(Options.Create(new GnnScorerOptions { ModelPath = TinyModelPath }));

    /// <summary>Детермінований L2-нормований вектор — різний для різних seed.</summary>
    public static float[] Embedding(int seed, int dim = EmbeddingDim)
    {
        var random = new Random(seed);
        var vector = Enumerable.Range(0, dim).Select(_ => (float)(random.NextDouble() * 2 - 1)).ToArray();
        var norm = MathF.Sqrt(vector.Sum(v => v * v));
        return vector.Select(v => v / norm).ToArray();
    }

    public static ItemNode Node(string id, int slot, float[]? vector = null, string embeddingModel = ClipModel) => new()
    {
        ItemId = id,
        AttributeType = 0,
        IsVirtual = false,
        FeatureVector = vector ?? [],
        EmbeddingModel = embeddingModel,
        Slot = slot,
        AttributeColorMain = string.Empty,
        AttributeColorSecond = string.Empty,
        AttributeSeason = 0,
        AttributePattern = 0,
        AttributeMatterial = 0,
    };

    /// <summary>Top + Bottom + Shoes з ембедингами — типовий кандидат з OutfitOfferService.</summary>
    public static List<ItemNode> Outfit() =>
    [
        Node("top", slot: 1, Embedding(1)),
        Node("bottom", slot: 2, Embedding(2)),
        Node("shoes", slot: 4, Embedding(3)),
    ];
}
