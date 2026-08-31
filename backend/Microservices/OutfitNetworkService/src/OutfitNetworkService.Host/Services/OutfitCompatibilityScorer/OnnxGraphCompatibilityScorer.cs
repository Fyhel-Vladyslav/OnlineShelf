using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OutfitNetworkService.src.OutfitNetworkService.Common.Interfaces;
using OutfitNetworkService.src.OutfitNetworkService.Repository.EfCore.Entities;

namespace OutfitNetworkService.src.OutfitNetworkService.Host.Services.OutfitCompatibilityScorer;

/// <summary>
/// Реалізація IGraphCompatibilityScorer через попередньо натреновану GNN-модель,
/// натреновану в PyTorch Geometric і експортовану в ONNX.
///
/// Контракт моделі (узгодити з ML-командою при експорті):
///   вхід  "node_features": float32[N, F]  — фічі вузлів (F = розмір FeatureVector)
///   вхід  "edge_index":    int64[2, E]    — [джерела; цілі], як у PyTorch Geometric
///   вихід "graph_score":   float32[1]     — агрегований readout-скор усього образу
///   вихід "edge_scores":   float32[E]     — попарний скор для кожного ребра (для explainability)
/// </summary>
/// <summary>
/// Реалізація IGraphCompatibilityScorer через попередньо натреновану GNN-модель,
/// натреновану в PyTorch Geometric і експортовану в ONNX.
///
/// Контракт моделі (узгодити з ML-командою при експорті):
///   вхід  "node_features": float32[N, F]  — фічі вузлів (F = розмір FeatureVector)
///   вхід  "edge_index":    int64[2, E]    — [джерела; цілі], як у PyTorch Geometric
///   вихід "graph_score":   float32[1]     — агрегований readout-скор усього образу
///   вихід "edge_scores":   float32[E]     — попарний скор для кожного ребра (для explainability)
/// </summary>
public sealed class OnnxGraphCompatibilityScorer : IGraphCompatibilityScorer, IDisposable
{
    private readonly InferenceSession _session;

    public OnnxGraphCompatibilityScorer(IOptions<GnnScorerOptions> options)
    {
        // IOptions<T> — це вже зареєстрований у DI сервіс (після services.Configure<T>(...)),
        // тож контейнер резолвить конструктор автоматично — жодного сирого string тут більше немає.
        // InferenceSession потокобезпечна для паралельних викликів Run() —
        // реєструвати як Singleton у DI, а не створювати на кожен запит.
        _session = new InferenceSession(options.Value.ModelPath);
    }

    public Task<GraphScoringResult> ScoreAsync(OutfitGraph graph, CancellationToken cancellationToken = default)
    {
        var nodeFeatures = BuildNodeFeatureTensor(graph);
        var edgeIndex = BuildEdgeIndexTensor(graph);

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("node_features", nodeFeatures),
            NamedOnnxValue.CreateFromTensor("edge_index", edgeIndex),
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
        var featureLength = graph.Nodes[0].FeatureVector.Length;
        var tensor = new DenseTensor<float>(new[] { graph.Nodes.Count, featureLength });

        for (var i = 0; i < graph.Nodes.Count; i++)
        {
            for (var f = 0; f < featureLength; f++)
                tensor[i, f] = graph.Nodes[i].FeatureVector[f];
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
