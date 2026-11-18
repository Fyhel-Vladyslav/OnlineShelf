"""Синтетичний Polyvore у форматі HF-копії + крихітна CLIP-модель з випадковими вагами.

Потрібен для тестів і smoke без справжнього датасету та без доступу до Hugging Face.
У кожного образу свій «стиль» (колір), картинки його айтемів — цей колір із шумом, тож
сумісність можна вивчити. Негативи compatibility/FITB — айтеми того самого слота з інших
образів, як у Polyvore Outfits.
"""

from __future__ import annotations

import io
import json
import random
from pathlib import Path

import numpy as np
import pyarrow as pa
import pyarrow.parquet as pq
from PIL import Image

CORE_TEMPLATES = (("tops", "bottoms", "shoes"), ("all-body", "shoes"))
EXTRAS = ("outerwear", "bags", "jewellery", "hats", "sunglasses")
PARQUET_SPLIT = {"train": "train", "valid": "validation", "test": "test"}


def _jpeg(color: np.ndarray, rng: np.random.Generator) -> bytes:
    pixels = np.clip(color + rng.normal(0, 25, (40, 32, 3)), 0, 255).astype(np.uint8)
    buf = io.BytesIO()
    Image.fromarray(pixels).save(buf, "JPEG", quality=90)
    return buf.getvalue()


def make_polyvore(root: Path, outfits_per_split: dict[str, int] | None = None, seed: int = 0) -> Path:
    outfits_per_split = outfits_per_split or {"train": 300, "valid": 80, "test": 80}
    rng = random.Random(seed)
    np_rng = np.random.default_rng(seed)
    (root / "data" / "disjoint").mkdir(parents=True, exist_ok=True)
    (root / "disjoint").mkdir(exist_ok=True)
    metadata: dict[str, dict] = {}
    next_item, next_set = 10_000, 100_000

    for split, n_outfits in outfits_per_split.items():
        outfits, images = [], {}
        for _ in range(n_outfits):
            next_set += 1
            style = np_rng.uniform(0, 255, 3)
            cats = list(rng.choice(CORE_TEMPLATES)) + rng.sample(EXTRAS, rng.randint(0, 2))
            items = []
            for index, cat in enumerate(cats, start=1):
                next_item += 1
                item_id = str(next_item)
                metadata[item_id] = {"semantic_category": cat}
                images[item_id] = _jpeg(style, np_rng)
                items.append({"item_id": item_id, "index": index})
            outfits.append({"set_id": str(next_set), "items": items})

        token_by_item = {it["item_id"]: f"{o['set_id']}_{it['index']}" for o in outfits for it in o["items"]}
        by_cat: dict[str, list[str]] = {}
        for item_id in token_by_item:
            by_cat.setdefault(metadata[item_id]["semantic_category"], []).append(item_id)

        def same_slot_other(item_id: str, outfit: dict) -> str:
            own = {it["item_id"] for it in outfit["items"]}
            pool = [i for i in by_cat[metadata[item_id]["semantic_category"]] if i not in own]
            # у дуже малих наборах слота може не бути в інших образах — тоді будь-який чужий айтем
            return rng.choice(pool or [i for i in token_by_item if i not in own])

        compat, fitb = [], []
        for o in outfits:
            ids = [it["item_id"] for it in o["items"]]
            compat.append("1 " + " ".join(token_by_item[i] for i in ids))
            negative = [same_slot_other(i, o) for i in ids]
            compat.append("0 " + " ".join(token_by_item[i] for i in negative))
            blank = rng.randrange(len(ids))
            answers = [token_by_item[ids[blank]]] + [token_by_item[same_slot_other(ids[blank], o)] for _ in range(3)]
            rng.shuffle(answers)
            fitb.append({"question": [token_by_item[i] for k, i in enumerate(ids) if k != blank],
                         "blank_position": blank + 1, "answers": answers})

        (root / "disjoint" / f"{split}.json").write_text(json.dumps(outfits), encoding="utf-8")
        (root / "disjoint" / f"compatibility_{split}.txt").write_text("\n".join(compat) + "\n", encoding="utf-8")
        (root / "disjoint" / f"fill_in_blank_{split}.json").write_text(json.dumps(fitb), encoding="utf-8")
        ids = sorted(images)
        table = pa.table({"item_id": ids, "image": [{"bytes": images[i], "path": f"{i}.jpg"} for i in ids]})
        pq.write_table(table, root / "data" / "disjoint" / f"{PARQUET_SPLIT[split]}.parquet")

    (root / "polyvore_item_metadata.json").write_text(json.dumps(metadata), encoding="utf-8")
    return root


def make_tiny_clip(path: Path) -> Path:
    """CLIP з випадковими вагами: та сама архітектура й препроцесинг, projection_dim = 512."""
    import torch
    from transformers import CLIPConfig, CLIPImageProcessor, CLIPModel

    torch.manual_seed(0)
    config = CLIPConfig(
        text_config=dict(hidden_size=32, intermediate_size=64, num_hidden_layers=1,
                         num_attention_heads=2, vocab_size=100),
        vision_config=dict(hidden_size=64, intermediate_size=128, num_hidden_layers=2,
                           num_attention_heads=2, image_size=224, patch_size=32),
        projection_dim=512,
    )
    CLIPModel(config).save_pretrained(path)
    CLIPImageProcessor().save_pretrained(path)
    return path


if __name__ == "__main__":
    import sys

    out = Path(sys.argv[1])
    make_polyvore(out / "polyvore_outfits")
    make_tiny_clip(out / "tiny_clip")
    print(f"synthetic dataset in {out}")
