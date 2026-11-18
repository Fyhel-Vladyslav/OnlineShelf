"""Графи для навчання й оцінки: позитиви, негативи, підобрази під шаблони OutfitOfferService.

Позитив — реальний образ або його підобраз за шаблоном `Top+Bottom+Shoes(+Outerwear)` чи
`FullBody+Shoes(+Outerwear)` (саме такі графи приходять з OutfitOffer). Негатив — той самий граф,
де 1–2 айтеми замінено випадковими айтемами **того самого слота**, щоб модель вчилась сумісності,
а не розрізненню категорій. Мітки ребер: пара двох оригінальних айтемів = 1, пара із заміненим = 0.
"""

from __future__ import annotations

import random
from dataclasses import dataclass

import numpy as np
import torch

from src.data.polyvore import Outfit, SlotMapper

TOP, BOTTOM, FULL_BODY, SHOES, OUTERWEAR = 1, 2, 3, 4, 5


class ItemTable:
    """Айтеми спліту: вектор, слот і прапорець наявності ембединга за рядком."""

    def __init__(self, item_ids: list[str], categories: dict[str, str], slot_mapper: SlotMapper,
                 emb_ids: list[str], vectors: np.ndarray, dim: int = 512) -> None:
        emb_row = {item_id: row for row, item_id in enumerate(emb_ids)}
        kept = [i for i in item_ids if slot_mapper(categories.get(i, "")) is not None]
        self.index = {item_id: row for row, item_id in enumerate(kept)}
        self.ids = kept
        self.slots = np.array([slot_mapper(categories[i]) for i in kept], dtype=np.int64)
        self.vectors = np.zeros((len(kept), dim), dtype=np.float32)
        self.has_embedding = np.zeros(len(kept), dtype=np.float32)
        for row, item_id in enumerate(kept):
            if item_id in emb_row:
                self.vectors[row] = vectors[emb_row[item_id]]
                self.has_embedding[row] = 1.0
        self.dropped = len(set(item_ids)) - len(kept)
        self.by_slot: dict[int, np.ndarray] = {
            int(s): np.flatnonzero(self.slots == s) for s in np.unique(self.slots)
        }

    def rows(self, item_ids: list[str]) -> list[int]:
        """Рядки айтемів; айтеми з відкинутою категорією пропускаються."""
        return [self.index[i] for i in item_ids if i in self.index]


@dataclass
class Graph:
    rows: list[int]          # рядки ItemTable
    original: list[bool]     # False — айтем підставлено в негативі
    label: int


def template_subgraph(rows: list[int], slots: np.ndarray, rng: random.Random) -> list[int] | None:
    """Випадковий підобраз образу за шаблонами OfferService або None, якщо жоден не складається."""
    by_slot: dict[int, list[int]] = {}
    for r in rows:
        by_slot.setdefault(int(slots[r]), []).append(r)
    options = []
    if by_slot.get(TOP) and by_slot.get(BOTTOM) and by_slot.get(SHOES):
        options.append((TOP, BOTTOM, SHOES))
    if by_slot.get(FULL_BODY) and by_slot.get(SHOES):
        options.append((FULL_BODY, SHOES))
    if not options:
        return None
    chosen = [rng.choice(by_slot[s]) for s in rng.choice(options)]
    if by_slot.get(OUTERWEAR) and rng.random() < 0.5:
        chosen.append(rng.choice(by_slot[OUTERWEAR]))
    return chosen


def make_negative(positive: list[int], table: ItemTable, rng: random.Random,
                  replace_min: int, replace_max: int) -> Graph | None:
    """Заміна 1–2 айтемів на випадкові того самого слота (не з цього образу)."""
    n = len(positive)
    k = rng.randint(replace_min, max(replace_min, min(replace_max, n - 1)))
    positions = rng.sample(range(n), k)
    rows = list(positive)
    original = [True] * n
    used = set(positive)
    for pos in positions:
        pool = table.by_slot.get(int(table.slots[positive[pos]]))
        if pool is None or len(pool) < 2:
            return None
        for _ in range(20):
            candidate = int(pool[rng.randrange(len(pool))])
            if candidate not in used:
                break
        else:
            return None
        rows[pos] = candidate
        original[pos] = False
        used.add(candidate)
    return Graph(rows, original, 0)


class GraphSampler:
    """Пари позитив/негатив на кожну епоху (нова випадкова вибірка щоразу)."""

    def __init__(self, outfits: list[Outfit], table: ItemTable, cfg: dict, seed: int) -> None:
        self.table = table
        self.cfg = cfg
        self.rng = random.Random(seed)
        self.outfit_rows = [r for r in (table.rows(o.item_ids) for o in outfits) if len(r) >= 2]

    def positive(self, rows: list[int]) -> list[int]:
        if self.rng.random() < self.cfg["template_prob"]:
            sub = template_subgraph(rows, self.table.slots, self.rng)
            if sub is not None:
                return sub
        if len(rows) > self.cfg["max_nodes"]:
            rows = self.rng.sample(rows, self.cfg["max_nodes"])
        return list(rows)

    def epoch(self) -> list[Graph]:
        graphs: list[Graph] = []
        for rows in self.outfit_rows:
            pos = self.positive(rows)
            neg = make_negative(pos, self.table, self.rng, self.cfg["replace_min"], self.cfg["replace_max"])
            if neg is None:
                continue
            graphs.append(Graph(pos, [True] * len(pos), 1))
            graphs.append(neg)
        self.rng.shuffle(graphs)
        return graphs


def eval_template_graphs(outfits: list[Outfit], table: ItemTable, seed: int) -> list[Graph]:
    """Фіксований набір підобразів 3–4 айтеми (реальний випадок OfferService) + негативи до кожного."""
    rng = random.Random(seed)
    graphs: list[Graph] = []
    for outfit in outfits:
        sub = template_subgraph(table.rows(outfit.item_ids), table.slots, rng)
        if sub is None or len(sub) < 3:
            continue
        neg = make_negative(sub, table, rng, 1, 1)
        if neg is not None:
            graphs.append(Graph(sub, [True] * len(sub), 1))
            graphs.append(neg)
    return graphs


@dataclass
class Batch:
    x: torch.Tensor           # [B, N, D]
    category: torch.Tensor    # [B, N] int64
    has_embedding: torch.Tensor  # [B, N]
    mask: torch.Tensor        # [B, N] 1 = справжній вузол, 0 = доповнення
    label: torch.Tensor       # [B]
    edge_label: torch.Tensor  # [B, N, N]

    def to(self, device: torch.device) -> "Batch":
        return Batch(*(t.to(device, non_blocking=True) for t in
                       (self.x, self.category, self.has_embedding, self.mask, self.label, self.edge_label)))


def collate(graphs: list[Graph], table: ItemTable, rng: np.random.Generator | None = None,
            drop_embedding_prob: float = 0.0, noise_std: float = 0.0) -> Batch:
    """Доповнює графи до спільного N. Аугментації (лише для навчання): обнулення ембединга і шум."""
    b = len(graphs)
    n = max(len(g.rows) for g in graphs)
    d = table.vectors.shape[1]
    x = np.zeros((b, n, d), dtype=np.float32)
    category = np.zeros((b, n), dtype=np.int64)
    has = np.zeros((b, n), dtype=np.float32)
    mask = np.zeros((b, n), dtype=np.float32)
    original = np.zeros((b, n), dtype=np.float32)
    for i, g in enumerate(graphs):
        k = len(g.rows)
        rows = np.asarray(g.rows)
        x[i, :k] = table.vectors[rows]
        category[i, :k] = table.slots[rows]
        has[i, :k] = table.has_embedding[rows]
        mask[i, :k] = 1.0
        original[i, :k] = g.original

    if rng is not None:
        if noise_std > 0:
            x += rng.normal(0.0, noise_std, size=x.shape).astype(np.float32)
            norms = np.linalg.norm(x, axis=-1, keepdims=True)
            x = np.where(norms > 0, x / np.maximum(norms, 1e-12), x)
        if drop_embedding_prob > 0:
            drop = (rng.random((b, n)) < drop_embedding_prob) & (mask > 0)
            has[drop] = 0.0
        x *= has[..., None]  # відсутній ембединг = нулі, як у .NET

    edge_label = original[:, :, None] * original[:, None, :]
    label = np.array([g.label for g in graphs], dtype=np.float32)
    return Batch(*(torch.from_numpy(a) for a in (x, category, has, mask, label, edge_label)))
