"""Читання Polyvore Outfits (Vasileva et al., 2018) у форматі HF `mvasil/polyvore-outfits`.

Структура (перевірено на завантаженій копії, 2026-10-10):

    polyvore_item_metadata.json      {item_id: {semantic_category, category_id, title, ...}}, 251 008 айтемів
    categories.csv                   category_id,fine_name,semantic_category
    polyvore_outfit_titles.json      {set_id: {url_name, title}}
    {variant}/{split}.json           [{set_id, items: [{item_id, index}]}], split = train|valid|test
    {variant}/compatibility_{split}.txt   "label set_index set_index ..."
    {variant}/fill_in_blank_{split}.json  [{question: [set_index], answers: [4 x set_index], blank_position}]
    data/{variant}/{train|validation|test}.parquet   картинки (див. images.py)

`set_index` = "<set_id>_<index>" посилається на айтем образу зі сплітів того самого варіанту.
`maryland_polyvore_hardneg/` посилається на образи Maryland Polyvore, яких у датасеті немає, тому не читається.
"""

from __future__ import annotations

import json
import os
from dataclasses import dataclass, field
from pathlib import Path

import yaml

REPO_ROOT = Path(__file__).resolve().parents[4]
CONFIG_PATH = REPO_ROOT / "ml" / "outfit_scorer" / "config.yaml"

SPLITS = ("train", "valid", "test")
# Назви сплітів у parquet-файлах HF відрізняються від JSON.
PARQUET_SPLIT = {"train": "train", "valid": "validation", "test": "test"}


def load_config(path: Path = CONFIG_PATH) -> dict:
    with open(path, encoding="utf-8") as fh:
        return yaml.safe_load(fh)


def polyvore_root(config: dict) -> Path:
    env = os.environ.get("POLYVORE_DIR")
    return Path(env) if env else REPO_ROOT / config["paths"]["polyvore_dir"]


def data_dir(config: dict) -> Path:
    return REPO_ROOT / config["paths"]["data_dir"]


class SlotMapper:
    """semantic_category → значення OutfitSlot (int) або None, якщо категорію відкинуто."""

    def __init__(self, config: dict) -> None:
        slots = config["outfit_slots"]
        self._map = {cat: int(slots[slot]) for cat, slot in config["category_map"].items()}

    def __call__(self, semantic_category: str) -> int | None:
        return self._map.get(semantic_category)


@dataclass
class Outfit:
    set_id: str
    item_ids: list[str]


@dataclass
class CompatQuestion:
    label: int
    item_ids: list[str]


@dataclass
class FitbQuestion:
    question_ids: list[str]
    answer_ids: list[str]
    correct: int  # індекс правильної відповіді в answer_ids


@dataclass
class SplitData:
    variant: str
    split: str
    outfits: list[Outfit]
    compatibility: list[CompatQuestion]
    fitb: list[FitbQuestion]
    skipped: dict[str, int] = field(default_factory=dict)

    def item_ids(self) -> set[str]:
        return {i for o in self.outfits for i in o.item_ids}

    def referenced_item_ids(self) -> set[str]:
        """Айтеми образів, compatibility- і FITB-питань (негативи посилаються на чужі образи)."""
        return (self.item_ids()
                | {i for q in self.compatibility for i in q.item_ids}
                | {i for q in self.fitb for i in q.question_ids + q.answer_ids})

    def limited(self, n: int | None) -> "SplitData":
        """Перші n образів і n питань кожного типу (smoke-прогони)."""
        if n is None:
            return self
        return SplitData(self.variant, self.split, self.outfits[:n], self.compatibility[:n], self.fitb[:n],
                         dict(self.skipped))


def load_item_categories(root: Path) -> dict[str, str]:
    """item_id → semantic_category. Решту метаданих не тримаємо в пам'яті."""
    with open(root / "polyvore_item_metadata.json", encoding="utf-8") as fh:
        meta = json.load(fh)
    return {item_id: str(m.get("semantic_category", "")) for item_id, m in meta.items()}


def load_outfits(root: Path, variant: str, split: str) -> tuple[list[Outfit], dict[str, str]]:
    """Образи спліту і індекс "<set_id>_<index>" → item_id."""
    with open(root / variant / f"{split}.json", encoding="utf-8") as fh:
        raw = json.load(fh)
    outfits: list[Outfit] = []
    index: dict[str, str] = {}
    for entry in raw:
        set_id = str(entry["set_id"])
        items = sorted(entry["items"], key=lambda it: int(it["index"]))
        outfits.append(Outfit(set_id, [str(it["item_id"]) for it in items]))
        for it in items:
            index[f"{set_id}_{it['index']}"] = str(it["item_id"])
    return outfits, index


def load_compatibility(root: Path, variant: str, split: str,
                       index: dict[str, str]) -> tuple[list[CompatQuestion], int]:
    questions: list[CompatQuestion] = []
    skipped = 0
    with open(root / variant / f"compatibility_{split}.txt", encoding="utf-8") as fh:
        for line in fh:
            parts = line.split()
            if not parts:
                continue
            tokens = parts[1:]
            if any(t not in index for t in tokens):
                skipped += 1
                continue
            questions.append(CompatQuestion(int(parts[0]), [index[t] for t in tokens]))
    return questions, skipped


def load_fitb(root: Path, variant: str, split: str,
              index: dict[str, str]) -> tuple[list[FitbQuestion], int]:
    """Правильна відповідь = варіант з тим самим set_id, що й питання (позиція в answers довільна)."""
    with open(root / variant / f"fill_in_blank_{split}.json", encoding="utf-8") as fh:
        raw = json.load(fh)
    questions: list[FitbQuestion] = []
    skipped = 0
    for q in raw:
        tokens = list(q["question"]) + list(q["answers"])
        set_id = str(q["question"][0]).rsplit("_", 1)[0]
        expected = f"{set_id}_{q['blank_position']}"
        correct = [i for i, a in enumerate(q["answers"]) if a == expected]
        if len(correct) != 1 or any(t not in index for t in tokens):
            skipped += 1
            continue
        questions.append(FitbQuestion(
            question_ids=[index[t] for t in q["question"]],
            answer_ids=[index[a] for a in q["answers"]],
            correct=correct[0],
        ))
    return questions, skipped


def load_split(root: Path, variant: str, split: str) -> SplitData:
    if split not in SPLITS:
        raise ValueError(f"unknown split {split!r}, expected one of {SPLITS}")
    outfits, index = load_outfits(root, variant, split)
    compat, compat_skipped = load_compatibility(root, variant, split, index)
    fitb, fitb_skipped = load_fitb(root, variant, split, index)
    return SplitData(variant, split, outfits, compat, fitb,
                     skipped={"compatibility": compat_skipped, "fitb": fitb_skipped})
