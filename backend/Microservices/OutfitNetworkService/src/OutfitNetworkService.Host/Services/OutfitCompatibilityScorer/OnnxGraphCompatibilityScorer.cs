using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OutfitNetworkService.src.OutfitNetworkService.Common.Interfaces;
using OutfitNetworkService.src.OutfitNetworkService.Repository.EfCore.Entities;

namespace OutfitNetworkService.src.OutfitNetworkService.Host.Services.OutfitCompatibilityScorer;

/// <summary>
/// Реалізація IGraphCompatibilityScorer через попередньо натреновану GNN-модель
/// (Type-Aware/CSA-Net попарна сумісність + NGNN-агрегація), експортовану з PyTorch в ONNX.
///
/// Контракт моделі (узгодити при експорті):
///   вхід  "node_features":    float32[N, F] — візуальний ембединг вузла (CLIP, L2-норм.); відсутній вектор = нулі
///   вхід  "node_categorical": int64[N, 4]   — [AttributeType, AttributeSeason, AttributePattern, AttributeMatterial];
///                                             embedding-таблиці категорій і проєкція в спільний простір — частина моделі
///   вхід  "edge_index":       int64[2, E]   — [джерела; цілі], як у PyTorch Geometric
///   вихід "graph_score":      float32[1]    — агрегований скор усього образу, [0; 1]
///   вихід "edge_scores":      float32[E]    — попарний скор для кожного ребра (для explainability)
/// </summary>
public sealed class OnnxGraphCompatibilityScorer : IGraphCompatibilityScorer, IDisposable
{
    public const int CategoricalFeatureCount = 4;

    private readonly InferenceSession _session;

    public OnnxGraphCompatibilityScorer(IOptions<GnnScorerOptions> options)
    {
        // InferenceSession потокобезпечна для паралельних викликів Run() — реєструється як Singleton
        _session = new InferenceSession(options.Value.ModelPath);
    }

    public Task<GraphScoringResult> ScoreAsync(OutfitGraph graph, CancellationToken cancellationToken = default)
    {
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("node_features", BuildNodeFeatureTensor(graph)),
            NamedOnnxValue.CreateFromTensor("node_categorical", BuildNodeCategoricalTensor(graph)),
            NamedOnnxValue.CreateFromTensor("edge_index", BuildEdgeIndexTensor(graph)),
        };

        using var outputs = _session.Run(inputs);

        var graphScore = outputs.First(o => o.Name == "graph_score").AsEnumerable<float>().First();
        var edgeScores = outputs.First(o => o.Name == "edge_scores").AsEnumerable<float>().ToArray();

        var pairwise = new Dictionary<(string, string), double>();
        for (var e = 0; e < graph.Edges.Count; e++)
        {
            var (source, target) = graph.GetEdgeNodes(e);
            pairwise[(source.ItemId, target.ItemId)] = edgeScores[e];
        }

        return Task.FromResult(new GraphScoringResult(graphScore, pairwise));
    }

    private static DenseTensor<float> BuildNodeFeatureTensor(OutfitGraph graph)
    {
        // Речі без ембединга (ще не пораховано / віртуальні) заповнюються нулями до спільної довжини
        var featureLength = graph.Nodes.Max(n => n.FeatureVector.Length);
        var tensor = new DenseTensor<float>(new[] { graph.Nodes.Count, featureLength });

        for (var i = 0; i < graph.Nodes.Count; i++)
        {
            var vector = graph.Nodes[i].FeatureVector;
            for (var f = 0; f < vector.Length; f++)
                tensor[i, f] = vector[f];
        }

        return tensor;
    }

    private static DenseTensor<long> BuildNodeCategoricalTensor(OutfitGraph graph)
    {
        var tensor = new DenseTensor<long>(new[] { graph.Nodes.Count, CategoricalFeatureCount });

        for (var i = 0; i < graph.Nodes.Count; i++)
        {
            var node = graph.Nodes[i];
            tensor[i, 0] = node.AttributeType;
            tensor[i, 1] = node.AttributeSeason;
            tensor[i, 2] = node.AttributePattern;
            tensor[i, 3] = node.AttributeMatterial;
        }

        return tensor;
    }

    private static DenseTensor<long> BuildEdgeIndexTensor(OutfitGraph graph)
    {
        // PyTorch Geometric очікує edge_index форми [2, E]: перший рядок — джерела, другий — цілі.
        var tensor = new DenseTensor<long>(new[] { 2, graph.Edges.Count });

        for (var e = 0; e < graph.Edges.Count; e++)
        {
            tensor[0, e] = graph.Edges[e].Source;
            tensor[1, e] = graph.Edges[e].Target;
        }

        return tensor;
    }

    public void Dispose() => _session.Dispose();
}
