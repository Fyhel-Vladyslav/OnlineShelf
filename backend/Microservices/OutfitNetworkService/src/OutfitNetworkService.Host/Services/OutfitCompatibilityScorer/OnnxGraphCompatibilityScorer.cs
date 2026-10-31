using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OutfitNetworkService.src.OutfitNetworkService.Common.Interfaces;
using OutfitNetworkService.src.OutfitNetworkService.Repository.EfCore.Entities;

namespace OutfitNetworkService.src.OutfitNetworkService.Host.Services.OutfitCompatibilityScorer;

/// <summary>
/// Реалізація IGraphCompatibilityScorer через натреновану модель сумісності
/// (CSA-Net попарна сумісність + NGNN-lite агрегація), експортовану з PyTorch в ONNX (ml/outfit_scorer).
///
/// Контракт моделі:
///   вхід  "node_features":      float32[N, D] — CLIP-ембединг вузла (L2-норм.), D = embedding_dim з метаданих;
///                                               відсутній вектор або вектор іншої моделі = нулі
///   вхід  "node_category":      int64[N]      — OutfitSlot (0..6)
///   вхід  "node_has_embedding": float32[N]    — 1/0; при 0 модель підставляє навчений вектор «немає фото»
///   вхід  "edge_index":         int64[2, E]   — [джерела; цілі]
///   вихід "graph_score":        float32[1]    — агрегований скор усього образу, [0; 1]
///   вихід "edge_scores":        float32[E]    — попарний скор для кожного ребра (для explainability), [0; 1]
/// Метадані (metadata_props): embedding_model, embedding_dim, category_vocab, model_version, trained_on, metrics_json.
/// </summary>
public sealed class OnnxGraphCompatibilityScorer : IGraphCompatibilityScorer, IDisposable
{
    public const int CategoryVocabSize = 7;

    private readonly InferenceSession _session;

    /// <summary>Розмірність ембединга, на якій навчена модель (з метаданих).</summary>
    public int EmbeddingDim { get; }

    /// <summary>Модель ембедингів, на якій навчена модель (з метаданих); вузли з іншою — без вектора.</summary>
    public string EmbeddingModel { get; }

    public string ModelVersion { get; }

    public OnnxGraphCompatibilityScorer(IOptions<GnnScorerOptions> options, ILogger? logger = null)
    {
        logger ??= NullLogger.Instance;

        // InferenceSession потокобезпечна для паралельних викликів Run() — реєструється як Singleton
        _session = new InferenceSession(options.Value.ModelPath);

        var metadata = _session.ModelMetadata.CustomMetadataMap;
        if (!metadata.TryGetValue("embedding_dim", out var dimText)
            || !int.TryParse(dimText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dim) || dim <= 0)
        {
            _session.Dispose();
            throw new InvalidOperationException($"ONNX model {options.Value.ModelPath} has no valid 'embedding_dim' metadata");
        }

        EmbeddingDim = dim;
        EmbeddingModel = metadata.GetValueOrDefault("embedding_model", string.Empty);
        ModelVersion = metadata.GetValueOrDefault("model_version", "unknown");

        logger.LogInformation(
            "Loaded outfit compatibility model {ModelVersion} ({TrainedOn}); embeddings {EmbeddingModel} x{EmbeddingDim}; metrics {Metrics}",
            ModelVersion, metadata.GetValueOrDefault("trained_on", "unknown"), EmbeddingModel, EmbeddingDim,
            metadata.GetValueOrDefault("metrics_json", "{}"));
    }

    public Task<GraphScoringResult> ScoreAsync(OutfitGraph graph, CancellationToken cancellationToken = default)
    {
        var (features, hasEmbedding) = BuildNodeFeatureTensors(graph);
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("node_features", features),
            NamedOnnxValue.CreateFromTensor("node_category", BuildNodeCategoryTensor(graph)),
            NamedOnnxValue.CreateFromTensor("node_has_embedding", hasEmbedding),
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

    /// <summary>
    /// Прогрів: по одному інференсу на кожен розмір графа 2..maxNodes. ONNX Runtime готує виконання
    /// окремо для кожної нової форми входу, тож без прогріву перші запити з новою кількістю речей повільніші.
    /// </summary>
    public void Warmup(int maxNodes = 6)
    {
        for (var n = 2; n <= maxNodes; n++)
            ScoreAsync(OutfitGraph.CreateComplete(WarmupNodes(n, EmbeddingDim, EmbeddingModel))).GetAwaiter().GetResult();
    }

    /// <summary>Пробні вузли для прогріву: слоти 1..6 по колу, однаковий одиничний вектор (dim = 0 — без вектора).</summary>
    public static List<ItemNode> WarmupNodes(int count, int embeddingDim, string embeddingModel)
    {
        var vector = new float[embeddingDim];
        if (embeddingDim > 0)
            vector[0] = 1f;

        return Enumerable.Range(0, count).Select(i => new ItemNode
        {
            ItemId = $"warmup-{i}",
            AttributeType = 0,
            IsVirtual = false,
            FeatureVector = vector,
            EmbeddingModel = embeddingModel,
            Slot = i % (CategoryVocabSize - 1) + 1,
            AttributeColorMain = string.Empty,
            AttributeColorSecond = string.Empty,
            AttributeSeason = 0,
            AttributePattern = 0,
            AttributeMatterial = 0,
        }).ToList();
    }

    /// <summary>
    /// Вектор використовується, лише якщо він потрібної розмірності й порахований тією ж моделлю ембедингів
    /// (порожній EmbeddingModel вважається сумісним — старі речі без цього поля). Інакше — нулі й has_embedding = 0.
    /// </summary>
    public bool CanUseEmbedding(ItemNode node) =>
        node.FeatureVector.Length == EmbeddingDim
        && (string.IsNullOrEmpty(node.EmbeddingModel)
            || string.Equals(node.EmbeddingModel, EmbeddingModel, StringComparison.OrdinalIgnoreCase));

    private (DenseTensor<float> Features, DenseTensor<float> HasEmbedding) BuildNodeFeatureTensors(OutfitGraph graph)
    {
        var features = new DenseTensor<float>(new[] { graph.Nodes.Count, EmbeddingDim });
        var hasEmbedding = new DenseTensor<float>(new[] { graph.Nodes.Count });

        for (var i = 0; i < graph.Nodes.Count; i++)
        {
            var node = graph.Nodes[i];
            if (!CanUseEmbedding(node))
                continue;

            hasEmbedding[i] = 1f;
            for (var f = 0; f < EmbeddingDim; f++)
                features[i, f] = node.FeatureVector[f];
        }

        return (features, hasEmbedding);
    }

    private static DenseTensor<long> BuildNodeCategoryTensor(OutfitGraph graph)
    {
        var tensor = new DenseTensor<long>(new[] { graph.Nodes.Count });

        for (var i = 0; i < graph.Nodes.Count; i++)
        {
            // Невідомий слот (поза 0..6) → Unknown, щоб не вийти за межі embedding-таблиці моделі
            var slot = graph.Nodes[i].Slot;
            tensor[i] = slot is >= 0 and < CategoryVocabSize ? slot : 0;
        }

        return tensor;
    }

    private static DenseTensor<long> BuildEdgeIndexTensor(OutfitGraph graph)
    {
        // edge_index форми [2, E]: перший рядок — джерела, другий — цілі.
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
