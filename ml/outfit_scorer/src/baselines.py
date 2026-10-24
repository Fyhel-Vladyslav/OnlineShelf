"""Baselines без навчання для таблиці 5.5.

Random — випадковий скор (очікувано AUC 0.5, FITB 25%).
Mean-cosine CLIP — середній косинус між CLIP-ембедингами всіх пар айтемів образу.
Pairwise CSA без GNN — це навчена модель з `gnn_rounds: 0` (python -m src.train --gnn-rounds 0).
"""

from __future__ import annotations

import numpy as np

from src.data.sampler import Graph, ItemTable
from src.evaluation import GraphScorer


def random_scorer(seed: int = 0) -> GraphScorer:
    rng = np.random.default_rng(seed)
    return lambda graphs: rng.random(len(graphs)).astype(np.float32)


def mean_cosine_scorer(table: ItemTable) -> GraphScorer:
    """Ембединги L2-нормовані, тож косинус = скалярний добуток. Айтеми без ембединга пропускаються."""

    def score(graphs: list[Graph]) -> np.ndarray:
        result = np.zeros(len(graphs), dtype=np.float32)
        for i, g in enumerate(graphs):
            rows = [r for r in g.rows if table.has_embedding[r] > 0]
            if len(rows) < 2:
                continue
            x = table.vectors[rows]
            sims = x @ x.T
            n = len(rows)
            result[i] = (sims.sum() - np.trace(sims)) / (n * (n - 1))
        return result

    return score
