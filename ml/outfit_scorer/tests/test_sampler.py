import random

import numpy as np

from src.data.polyvore import Outfit, SlotMapper, load_config
from src.data.sampler import (BOTTOM, FULL_BODY, OUTERWEAR, SHOES, TOP, GraphSampler, ItemTable, collate,
                              make_negative, template_subgraph)

CATEGORIES = {
    "t1": "tops", "t2": "tops", "b1": "bottoms", "b2": "bottoms", "s1": "shoes", "s2": "shoes",
    "f1": "all-body", "o1": "outerwear", "o2": "outerwear", "g1": "bags", "g2": "bags",
}


def make_table(missing=()):
    ids = sorted(CATEGORIES)
    emb_ids = [i for i in ids if i not in missing]
    rng = np.random.default_rng(0)
    vectors = rng.normal(size=(len(emb_ids), 512)).astype(np.float32)
    vectors /= np.linalg.norm(vectors, axis=1, keepdims=True)
    return ItemTable(ids, CATEGORIES, SlotMapper(load_config()), emb_ids, vectors)


def test_table_marks_missing_embeddings():
    table = make_table(missing=("t2",))
    row = table.index["t2"]
    assert table.has_embedding[row] == 0 and not table.vectors[row].any()
    assert table.has_embedding.sum() == len(CATEGORIES) - 1


def test_template_subgraph_matches_offer_templates():
    table = make_table()
    rng = random.Random(0)
    outfit = table.rows(["t1", "b1", "s1", "o1", "g1", "f1"])
    seen = set()
    for _ in range(200):
        slots = tuple(sorted(int(table.slots[r]) for r in template_subgraph(outfit, table.slots, rng)))
        seen.add(slots)
    assert seen <= {(TOP, BOTTOM, SHOES), (TOP, BOTTOM, SHOES, OUTERWEAR),
                    (FULL_BODY, SHOES), (FULL_BODY, SHOES, OUTERWEAR)}
    assert len(seen) == 4
    assert template_subgraph(table.rows(["t1", "g1"]), table.slots, rng) is None


def test_negative_replaces_with_same_slot_from_outside_outfit():
    table = make_table()
    rng = random.Random(1)
    positive = table.rows(["t1", "b1", "s1", "g1"])
    for _ in range(100):
        neg = make_negative(positive, table, rng, 1, 2)
        assert neg.label == 0
        replaced = [i for i, orig in enumerate(neg.original) if not orig]
        assert 1 <= len(replaced) <= 2
        for i in replaced:
            assert table.slots[neg.rows[i]] == table.slots[positive[i]]
            assert neg.rows[i] not in positive


def test_sampler_epoch_is_balanced():
    table = make_table()
    cfg = load_config()["train"]
    outfits = [Outfit("1", ["t1", "b1", "s1"]), Outfit("2", ["f1", "s2", "o1"]), Outfit("3", ["t2", "b2", "s2", "g2"])]
    graphs = GraphSampler(outfits, table, cfg, seed=0).epoch()
    labels = [g.label for g in graphs]
    assert labels.count(1) == labels.count(0) == 3


def test_collate_pads_and_labels_edges():
    table = make_table()
    rng = random.Random(0)
    pos = table.rows(["t1", "b1", "s1"])
    neg = make_negative(pos, table, rng, 1, 1)
    small = table.rows(["f1", "s2"])
    from src.data.sampler import Graph
    batch = collate([Graph(pos, [True] * 3, 1), neg, Graph(small, [True, True], 1)], table)
    assert batch.x.shape == (3, 3, 512)
    assert batch.mask.tolist()[2] == [1.0, 1.0, 0.0]
    assert batch.label.tolist() == [1.0, 0.0, 1.0]
    assert batch.edge_label[0].min() == 1.0
    replaced = neg.original.index(False)
    assert batch.edge_label[1, replaced].sum() == 0 and batch.edge_label[1, :, replaced].sum() == 0


def test_collate_drop_augmentation_zeroes_vectors():
    table = make_table()
    from src.data.sampler import Graph
    graphs = [Graph(table.rows(["t1", "b1", "s1"]), [True] * 3, 1)] * 50
    batch = collate(graphs, table, np.random.default_rng(0), drop_embedding_prob=0.5, noise_std=0.01)
    dropped = batch.has_embedding == 0
    assert 0 < dropped.sum() < dropped.numel()
    assert batch.x[dropped].abs().sum() == 0
    kept_norms = batch.x[~dropped].norm(dim=-1)
    assert np.allclose(kept_norms.numpy(), 1.0, atol=1e-5)
