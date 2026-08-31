namespace OutfitNetworkService.src.OutfitNetworkService.Repository.EfCore.Entities;

/// <summary>
/// Граф кандидата-образу: вузли — айтеми, ребра — пари, для яких рахується сумісність.
/// За замовчуванням граф повнозв'язний — GNN сама навчається через message-passing
/// та ваги уваги, які пари важливіші, тож ручне обмеження ребер тут не потрібне.
/// </summary>
public sealed class OutfitGraph
{
    public IReadOnlyList<ItemNode> Nodes { get; }

    /// <summary>Ребра як пари 0-based індексів вузлів, що відповідають масиву Nodes.</summary>
    public IReadOnlyList<(int Source, int Target)> Edges { get; }

    private OutfitGraph(IReadOnlyList<ItemNode> nodes, IReadOnlyList<(int, int)> edges)
    {
        Nodes = nodes;
        Edges = edges;
    }

    /// <summary>
    /// Будує повнозв'язний орієнтований граф (кожна впорядкована пара вузлів — окреме ребро),
    /// що відповідає типовому контракту PyTorch Geometric edge_index.
    /// </summary>
    public static OutfitGraph CreateComplete(IReadOnlyList<ItemNode> nodes)
    {
        if (nodes.Count < 2)
            throw new ArgumentException("Граф сумісності потребує щонайменше 2 айтеми.", nameof(nodes));

        var edges = new List<(int, int)>();
        for (var i = 0; i < nodes.Count; i++)
        {
            for (var j = 0; j < nodes.Count; j++)
            {
                if (i != j)
                    edges.Add((i, j));
            }
        }

        return new OutfitGraph(nodes, edges);
    }

    /// <summary>Пара вузлів по індексу ребра — зручно для зіставлення з попарними виходами моделі.</summary>
    public (ItemNode Source, ItemNode Target) GetEdgeNodes(int edgeIndex)
    {
        var (s, t) = Edges[edgeIndex];
        return (Nodes[s], Nodes[t]);
    }
}
