"""Порівняння моделей на zero-shot визначенні матеріалу (ParseImageService MaterialDetector).

    python -m src.eval_material --per-class 200

Розмітки матеріалу в Polyvore немає, тому слабка мітка береться з назви речі: одяг (не взуття й не аксесуари)
зі спліту test, у title якого згадано рівно один матеріал. Шлях як у Parse: YOLO-кроп → промти MaterialDetector →
матеріал з найбільшою схожістю картинки й тексту. Звіт → ml/data/eval/material/<час>/report.md.
"""

from __future__ import annotations

import argparse
import json
import random
import re
import sys
from datetime import datetime
from pathlib import Path

import numpy as np
import pyarrow.parquet as pq
import torch

from src.data.embed import build_cropper, decode, load_clip
from src.data.polyvore import data_dir, load_config, load_outfits, polyvore_root

CLOTHING = {"tops", "bottoms", "outerwear", "all-body"}

# Ті самі промти, що й у MaterialDetector
PROMPTS = {
    "denim": "a photo of denim fabric jeans material",
    "leather": "a photo of leather jacket smooth leather material",
    "cotton": "a photo of cotton t-shirt fabric soft textile",
    "wool": "a photo of knitted wool sweater warm fabric",
    "silk": "a photo of shiny silk or satin fabric smooth material",
    "linen": "a photo of linen fabric texture",
    "polyester": "a photo of synthetic polyester sportswear fabric",
    "corduroy": "a photo of ribbed corduroy fabric material",
}
KEYWORDS = {
    "denim": ["denim", "jean", "jeans"],
    "leather": ["leather"],
    "cotton": ["cotton"],
    "wool": ["wool", "woolen", "woollen", "merino", "cashmere"],
    "silk": ["silk", "satin"],
    "linen": ["linen"],
    "polyester": ["polyester"],
    "corduroy": ["corduroy", "cord"],
}


def weak_label(title: str) -> str | None:
    """Матеріал з назви речі; None, якщо не згадано жодного або кілька (назва неоднозначна)."""
    words = set(re.findall(r"[a-z]+", title.lower()))
    found = [m for m, keys in KEYWORDS.items() if words & set(keys)]
    return found[0] if len(found) == 1 else None


def features(output) -> torch.Tensor:
    # transformers 5.x повертає BaseModelOutputWithPooling (проєктований вектор у pooler_output), 4.x — тензор
    tensor = output if isinstance(output, torch.Tensor) else output.pooler_output
    return tensor / tensor.norm(dim=-1, keepdim=True)


def predict(model_name: str, crops: list[np.ndarray], device: torch.device, batch_size: int = 64) -> np.ndarray:
    from transformers import CLIPTokenizerFast

    model, processor = load_clip(model_name, device)
    tokenizer = CLIPTokenizerFast.from_pretrained(model_name)
    with torch.no_grad():
        text = tokenizer(list(PROMPTS.values()), padding=True, return_tensors="pt").to(device)
        text_features = features(model.get_text_features(**text))
        result = []
        for start in range(0, len(crops), batch_size):
            pixels = processor(images=crops[start:start + batch_size], return_tensors="pt")["pixel_values"].to(device)
            result.append((features(model.get_image_features(pixel_values=pixels)) @ text_features.T).argmax(1).cpu())
    return torch.cat(result).numpy()


def report_row(name: str, y_true: np.ndarray, y_pred: np.ndarray, labels: list[str]) -> dict:
    per_class = {label: float((y_pred[y_true == k] == k).mean()) for k, label in enumerate(labels)}
    predicted = {label: float((y_pred == k).mean()) for k, label in enumerate(labels)}
    precision = {label: float((y_true[y_pred == k] == k).mean()) if (y_pred == k).any() else 0.0
                 for k, label in enumerate(labels)}
    return {"model": name, "accuracy": float((y_true == y_pred).mean()),
            "balanced_accuracy": float(np.mean(list(per_class.values()))), "recall": per_class,
            "precision": precision, "predicted_share": predicted}


def main() -> int:
    config = load_config()
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--models", nargs="+", default=["openai/clip-vit-base-patch32", "patrickjohncyh/fashion-clip"])
    parser.add_argument("--per-class", type=int, default=200, help="at most N items per material")
    parser.add_argument("--variant", default=config["dataset"]["variant"])
    parser.add_argument("--seed", type=int, default=config["train"]["seed"])
    parser.add_argument("--out-dir", type=Path, default=data_dir(config) / "eval" / "material")
    parser.add_argument("--device", default="cuda" if torch.cuda.is_available() else "cpu")
    args = parser.parse_args()

    root = polyvore_root(config)
    device = torch.device(args.device)
    with open(root / "polyvore_item_metadata.json", encoding="utf-8") as fh:
        meta = json.load(fh)
    outfits, _ = load_outfits(root, args.variant, "test")

    by_material: dict[str, list[str]] = {m: [] for m in PROMPTS}
    for item_id in sorted({i for o in outfits for i in o.item_ids}):
        m = meta.get(item_id, {})
        if m.get("semantic_category") in CLOTHING and (label := weak_label(str(m.get("title") or ""))):
            by_material[label].append(item_id)

    rng = random.Random(args.seed)
    wanted: dict[str, str] = {}
    for material, ids in by_material.items():
        rng.shuffle(ids)
        wanted.update({i: material for i in ids[:args.per_class]})
    print({m: f"{min(len(i), args.per_class)}/{len(i)}" for m, i in by_material.items()})

    cropper = build_cropper(config["embedding"], "yolo", device)
    crops, truth = [], []
    parquet = root / "data" / args.variant / "test.parquet"
    for batch in pq.ParquetFile(parquet).iter_batches(batch_size=2048, columns=["item_id", "image"]):
        ids, blobs = batch.column(0).to_pylist(), batch.column(1).field("bytes").to_pylist()
        for item_id, blob in zip(ids, blobs):
            if item_id in wanted and (crop := cropper(decode(blob))) is not None:
                crops.append(crop)
                truth.append(wanted[item_id])

    labels = list(PROMPTS)
    y_true = np.array([labels.index(t) for t in truth])
    rows = [report_row(name, y_true, predict(name, crops, device), labels) for name in args.models]

    counts = {m: int((y_true == k).sum()) for k, m in enumerate(labels)}
    lines = ["# Визначення матеріалу: слабка розмітка з назв Polyvore test", "",
             f"Речей: {len(y_true)} ({counts}). Випадковий вибір: {1 / len(labels):.1%}.", "",
             "| Модель | Метрика | " + " | ".join(labels) + " |",
             "|---|---|" + "---|" * len(labels)]
    for r in rows:
        lines.append(f"| {r['model']} (accuracy {r['accuracy']:.1%}, balanced {r['balanced_accuracy']:.1%}) | recall | "
                     + " | ".join(f"{r['recall'][m]:.0%}" for m in labels) + " |")
        lines.append("| | precision | " + " | ".join(f"{r['precision'][m]:.0%}" for m in labels) + " |")
        lines.append("| | частка передбачень | " + " | ".join(f"{r['predicted_share'][m]:.0%}" for m in labels) + " |")
    out_dir = args.out_dir / f"{datetime.now():%Y%m%d_%H%M%S}"
    out_dir.mkdir(parents=True, exist_ok=True)
    (out_dir / "report.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    (out_dir / "metrics.json").write_text(json.dumps(rows, indent=2), encoding="utf-8")
    print("\n".join(lines))
    return 0


if __name__ == "__main__":
    sys.exit(main())
