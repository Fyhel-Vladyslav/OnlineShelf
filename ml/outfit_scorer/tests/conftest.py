import json

import pytest


@pytest.fixture
def tiny_polyvore(tmp_path):
    """Мінімальна копія структури Polyvore Outfits (disjoint, split test)."""
    root = tmp_path / "polyvore_outfits"
    (root / "disjoint").mkdir(parents=True)
    meta = {
        "1": {"semantic_category": "tops"}, "2": {"semantic_category": "bottoms"},
        "3": {"semantic_category": "shoes"}, "4": {"semantic_category": "all-body"},
        "5": {"semantic_category": "shoes"}, "6": {"semantic_category": "bags"},
    }
    (root / "polyvore_item_metadata.json").write_text(json.dumps(meta), encoding="utf-8")
    outfits = [
        # index навмисно не по порядку: читання має сортувати за index
        {"set_id": "100", "items": [{"item_id": "2", "index": 2}, {"item_id": "1", "index": 1},
                                    {"item_id": "3", "index": 3}]},
        {"set_id": "200", "items": [{"item_id": "4", "index": 1}, {"item_id": "5", "index": 2},
                                    {"item_id": "6", "index": 3}]},
    ]
    (root / "disjoint" / "test.json").write_text(json.dumps(outfits), encoding="utf-8")
    (root / "disjoint" / "compatibility_test.txt").write_text(
        "1 100_1 100_2 100_3\n0 100_1 200_2 200_3\n1 999_1 100_2\n", encoding="utf-8")
    fitb = [
        {"question": ["100_1", "100_2"], "blank_position": 3, "answers": ["200_2", "200_3", "100_3", "200_1"]},
        {"question": ["200_1", "200_2"], "blank_position": 3, "answers": ["200_3", "100_1", "100_2", "100_3"]},
        {"question": ["100_1"], "blank_position": 2, "answers": ["200_2", "200_3", "200_1", "100_3"]},
    ]
    (root / "disjoint" / "fill_in_blank_test.json").write_text(json.dumps(fitb), encoding="utf-8")
    return root
