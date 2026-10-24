"""Метрики (Compat AUC, FITB) і скоринг наборів графів — спільне для train/eval/baselines."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Callable

import numpy as np
import torch

from src.data.polyvore import SplitData
from src.data.sampler import Graph, ItemTable, collate

GraphScorer = Callable[[list[Graph]], np.ndarray]


def roc_auc(labels: np.ndarray, scores: np.ndarray) -> float:
    """AUC через ранги (Mann–Whitney), однакові скори отримують середній ранг."""
    labels = np.asarray(labels, dtype=bool)
    scores = np.asarray(scores, dtype=np.float64)
    n_pos, n_neg = labels.sum(), (~labels).sum()
    if n_pos == 0 or n_neg == 0:
        return float("nan")
    order = np.argsort(scores, kind="mergesort")
    ranks = np.empty(len(scores), dtype=np.float64)
    sorted_scores = scores[order]
    i = 0
    while i < len(scores):
        j = i
        while j + 1 < len(scores) and sorted_scores[j + 1] == sorted_scores[i]:
            j += 1
        ranks[order[i:j + 1]] = 0.5 * (i + j) + 1.0
        i = j + 1
    return float((ranks[labels].sum() - n_pos * (n_pos + 1) / 2) / (n_pos * n_neg))


@dataclass
class EvalSet:
    """Compatibility- і FITB-графи спліту в рядках ItemTable."""
    compat_graphs: list[Graph]
    fitb_graphs: list[Graph]       # 4 кандидати на питання, підряд
    fitb_correct: np.ndarray       # індекс правильного кандидата 0..3
    skipped: int


def build_eval_set(split: SplitData, table: ItemTable) -> EvalSet:
    compat, skipped = [], 0
    for q in split.compatibility:
        rows = table.rows(q.item_ids)
        if len(rows) < 2:
            skipped += 1
            continue
        compat.append(Graph(rows, [True] * len(rows), q.label))
    fitb, correct = [], []
    for q in split.fitb:
        question = table.rows(q.question_ids)
        answers = table.rows(q.answer_ids)
        if len(answers) != len(q.answer_ids) or not question:
            skipped += 1
            continue
        fitb.extend(Graph(question + [a], [True] * (len(question) + 1), 0) for a in answers)
        correct.append(q.correct)
    return EvalSet(compat, fitb, np.array(correct, dtype=np.int64), skipped)


def fitb_accuracy(scores: np.ndarray, correct: np.ndarray) -> float:
    if len(correct) == 0:
        return float("nan")
    return float((scores.reshape(-1, 4).argmax(axis=1) == correct).mean())


def evaluate(scorer: GraphScorer, eval_set: EvalSet) -> dict[str, float]:
    compat_scores = scorer(eval_set.compat_graphs)
    labels = np.array([g.label for g in eval_set.compat_graphs])
    return {
        "compat_auc": roc_auc(labels, compat_scores),
        "fitb_acc": fitb_accuracy(scorer(eval_set.fitb_graphs), eval_set.fitb_correct),
    }


def model_scorer(model: torch.nn.Module, table: ItemTable, device: torch.device,
                 batch_size: int = 512) -> GraphScorer:
    """graph_score моделі для списку графів (батчі групуються за розміром, щоб менше доповнювати)."""

    @torch.no_grad()
    def score(graphs: list[Graph]) -> np.ndarray:
        model.eval()
        result = np.zeros(len(graphs), dtype=np.float32)
        order = sorted(range(len(graphs)), key=lambda i: len(graphs[i].rows))
        for start in range(0, len(order), batch_size):
            idx = order[start:start + batch_size]
            batch = collate([graphs[i] for i in idx], table).to(device)
            graph_logit, _ = model(batch.x, batch.category, batch.has_embedding, batch.mask)
            result[idx] = torch.sigmoid(graph_logit).float().cpu().numpy()
        return result

    return score


def histogram(scores: np.ndarray, bins: int = 10) -> list[dict]:
    counts, edges = np.histogram(scores, bins=bins, range=(0.0, 1.0))
    return [{"from": round(float(edges[i]), 2), "to": round(float(edges[i + 1]), 2), "count": int(c)}
            for i, c in enumerate(counts)]
