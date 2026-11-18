"""Скільки речей образу знаходить детектор на одному фото: порівняння кандидатів для ParseImageService.

    python -m src.eval_detection --outfits 300
    python -m src.eval_detection --dataset fashionpedia --split test
    python -m src.eval_detection --dataset atr --detector detr --outfits 0

collages: з речей образів Polyvore test збираються колажі на білому тлі з відомими рамками (умови flat-lay).
fashionpedia: фото людей із розміткою, беруться лише цілі образи (є взуття і сукня або верх із низом), а два
черевики в розмітці зливаються в одну пару. Це найближче до фото, які завантажуватиме дизайнер.
atr: фото людей з іншого джерела, рамки беруться з масок. Куртку й верх ATR не розрізняє, тому Outerwear
рахується як Top.
Кожен результат подається двічі: усі рамки та лише найкраща на слот (так працюватиме Parse).
Річ вважається знайденою, якщо рамки того самого слота з центром усередині неї разом дають IoU ≥ 0.5
(одну пару взуття детектор часто бачить як два черевики).
Звіт → ml/data/eval/detection/<час>/report.md.
"""

from __future__ import annotations

import argparse
import json
import random
import sys
import time
from dataclasses import dataclass, replace
from datetime import datetime
from io import BytesIO
from pathlib import Path

import cv2
import numpy as np
import pyarrow.parquet as pq
import torch
import yaml
from PIL import Image

from src.data.polyvore import REPO_ROOT, data_dir, load_config, load_outfits, polyvore_root

CANVAS = 1024
FASHIONPEDIA_ROOT = REPO_ROOT / "backend/Microservices/ParseImageService/training_sandbox/data"
PARSE_DETECTOR_DIR = REPO_ROOT / "backend/Microservices/ParseImageService/app/models/weights/outfit_detector"
SLOTS = {"tops": "Top", "bottoms": "Bottom", "all-body": "FullBody", "shoes": "Shoes", "outerwear": "Outerwear"}

# Класи YOLO (Fashionpedia) → слот, як OutfitGeneration:Slots в OutfitOfferService
YOLO_SLOTS = {
    "shirt": "Top", "top, t-shirt, sweatshirt": "Top", "sweater": "Top", "cardigan": "Top", "vest": "Top",
    "jacket": "Outerwear", "coat": "Outerwear", "cape": "Outerwear",
    "pants": "Bottom", "shorts": "Bottom", "skirt": "Bottom",
    "dress": "FullBody", "jumpsuit": "FullBody",
    "shoe": "Shoes",
}
DETR_SLOTS = {"top": "Top", "outer": "Outerwear", "bottom": "Bottom", "dress": "FullBody", "shoes": "Shoes"}
# Мітки масок ATR: 4 Upper-clothes, 5 Skirt, 6 Pants, 7 Dress, 9/10 ліва і права взуттєва
ATR_SLOTS = {4: "Top", 5: "Bottom", 6: "Bottom", 7: "FullBody", 9: "Shoes", 10: "Shoes"}
MIN_SLOT_AREA = 0.001  # частка фото: менше — шум масок, а не річ
MIN_COMPONENT_SHARE = 0.05  # окремі дрібні плями тієї самої мітки не розширюють рамку


@dataclass
class Box:
    slot: str
    x1: float
    y1: float
    x2: float
    y2: float
    confidence: float = 1.0


def iou(a: Box, b: Box) -> float:
    w = max(0.0, min(a.x2, b.x2) - max(a.x1, b.x1))
    h = max(0.0, min(a.y2, b.y2) - max(a.y1, b.y1))
    inter = w * h
    union = (a.x2 - a.x1) * (a.y2 - a.y1) + (b.x2 - b.x1) * (b.y2 - b.y1) - inter
    return inter / union if union > 0 else 0.0


def build_collage(images: list[Image.Image], slots: list[str], rng: random.Random) -> tuple[Image.Image, list[Box]]:
    """Речі в сітці 2×N з випадковим масштабом і зсувом усередині клітинки."""
    columns = 2 if len(images) <= 4 else 3
    rows = (len(images) + columns - 1) // columns
    cell_w, cell_h = CANVAS // columns, CANVAS // rows
    canvas = Image.new("RGB", (CANVAS, CANVAS), "white")
    cells = list(range(columns * rows))
    rng.shuffle(cells)
    boxes = []
    for image, slot, cell in zip(images, slots, cells):
        scale = rng.uniform(0.75, 0.95) * min(cell_w / image.width, cell_h / image.height)
        w, h = int(image.width * scale), int(image.height * scale)
        x = (cell % columns) * cell_w + rng.randint(0, max(cell_w - w, 0))
        y = (cell // columns) * cell_h + rng.randint(0, max(cell_h - h, 0))
        canvas.paste(image.resize((w, h)), (x, y))
        boxes.append(Box(slot, x, y, x + w, y + h))
    return canvas, boxes


def load_collages(config: dict, variant: str, count: int, seed: int) -> list[tuple[Image.Image, list[Box]]]:
    root = polyvore_root(config)
    with open(root / "polyvore_item_metadata.json", encoding="utf-8") as fh:
        meta = json.load(fh)
    outfits, _ = load_outfits(root, variant, "test")
    rng = random.Random(seed)
    rng.shuffle(outfits)

    chosen = []
    for outfit in outfits:
        items = [(i, SLOTS[meta[i]["semantic_category"]]) for i in outfit.item_ids
                 if meta.get(i, {}).get("semantic_category") in SLOTS]
        if len(items) >= 3:
            chosen.append(items[:5])
        if len(chosen) == count:
            break

    wanted = {i for items in chosen for i, _ in items}
    blobs: dict[str, bytes] = {}
    parquet = root / "data" / variant / "test.parquet"
    for batch in pq.ParquetFile(parquet).iter_batches(batch_size=2048, columns=["item_id", "image"]):
        for item_id, blob in zip(batch.column(0).to_pylist(), batch.column(1).field("bytes").to_pylist()):
            if item_id in wanted:
                blobs[item_id] = blob
    return [build_collage([Image.open(BytesIO(blobs[i])).convert("RGB") for i, _ in items],
                          [s for _, s in items], rng) for items in chosen]


def merge_boxes(slot: str, boxes: list[Box]) -> Box:
    return Box(slot, min(b.x1 for b in boxes), min(b.y1 for b in boxes),
               max(b.x2 for b in boxes), max(b.y2 for b in boxes))


def is_whole_outfit(boxes: list[Box]) -> bool:
    slots = {b.slot for b in boxes}
    return "Shoes" in slots and ("FullBody" in slots or {"Top", "Bottom"} <= slots)


def load_fashionpedia(root: Path, split: str, count: int, seed: int) -> list[tuple[Path, list[Box]]]:
    """Фото цілих образів із розмітки у форматі YOLO; count ≤ 0 бере всі."""
    with open(root / "data.yaml", encoding="utf-8") as fh:
        names = yaml.safe_load(fh)["names"]

    photos = []
    for label in sorted((root / "labels" / split).glob("*.txt")):
        rows = [line.split() for line in label.read_text(encoding="utf-8").splitlines() if line.strip()]
        raw = [(YOLO_SLOTS[names[int(r[0])]], *map(float, r[1:5])) for r in rows if names[int(r[0])] in YOLO_SLOTS]
        if not is_whole_outfit([Box(row[0], 0, 0, 0, 0) for row in raw]):
            continue
        path = root / "images" / split / f"{label.stem}.jpg"
        with Image.open(path) as image:
            width, height = image.size
        boxes = [Box(slot, (x - w / 2) * width, (y - h / 2) * height, (x + w / 2) * width, (y + h / 2) * height)
                 for slot, x, y, w, h in raw]
        shoes = [b for b in boxes if b.slot == "Shoes"]
        photos.append((path, [b for b in boxes if b.slot != "Shoes"] + [merge_boxes("Shoes", shoes)]))

    random.Random(seed).shuffle(photos)
    return photos[:count] if count > 0 else photos


def mask_boxes(mask: np.ndarray) -> list[Box]:
    boxes = []
    for slot in dict.fromkeys(ATR_SLOTS.values()):
        region = np.isin(mask, [label for label, s in ATR_SLOTS.items() if s == slot]).astype(np.uint8)
        total = int(region.sum())
        if total < MIN_SLOT_AREA * mask.size:
            continue
        _, _, stats, _ = cv2.connectedComponentsWithStats(region)
        parts = stats[1:][stats[1:, cv2.CC_STAT_AREA] >= MIN_COMPONENT_SHARE * total]
        x, y = parts[:, cv2.CC_STAT_LEFT], parts[:, cv2.CC_STAT_TOP]
        boxes.append(Box(slot, float(x.min()), float(y.min()), float((x + parts[:, cv2.CC_STAT_WIDTH]).max()),
                         float((y + parts[:, cv2.CC_STAT_HEIGHT]).max())))
    return boxes


def load_atr(parquet: Path, count: int, seed: int) -> list[tuple[bytes, list[Box]]]:
    photos = []
    for batch in pq.ParquetFile(parquet).iter_batches(batch_size=256, columns=["image", "mask"]):
        for image, mask in zip(batch.column(0).field("bytes").to_pylist(), batch.column(1).field("bytes").to_pylist()):
            boxes = mask_boxes(np.array(Image.open(BytesIO(mask))))
            if is_whole_outfit(boxes):
                photos.append((image, boxes))
    random.Random(seed).shuffle(photos)
    return photos[:count] if count > 0 else photos


def open_image(source: Image.Image | Path | bytes) -> Image.Image:
    if isinstance(source, bytes):
        source = BytesIO(source)
    return source if isinstance(source, Image.Image) else Image.open(source).convert("RGB")


def yolo_detector(weights: Path, device: str):
    from ultralytics import YOLO

    model = YOLO(str(weights))

    def detect(image: Image.Image) -> list[Box]:
        result = model(np.array(image)[:, :, ::-1], verbose=False, conf=0.05, device=device)[0]
        boxes = []
        for box in result.boxes:
            slot = YOLO_SLOTS.get(result.names[int(box.cls[0].item())])
            if slot:
                boxes.append(Box(slot, *box.xyxy[0].tolist(), float(box.conf[0].item())))
        return boxes

    return detect


def detr_detector(path: Path, device: str):
    """Conditional DETR із ваг, підготовлених src.convert_outfit_detector."""
    from transformers import AutoImageProcessor, AutoModelForObjectDetection

    model = AutoModelForObjectDetection.from_pretrained(path, local_files_only=True).to(device).eval()
    processor = AutoImageProcessor.from_pretrained(path, local_files_only=True)

    @torch.no_grad()
    def detect(image: Image.Image) -> list[Box]:
        inputs = processor(images=[image], return_tensors="pt").to(device)
        result = processor.post_process_object_detection(
            model(**inputs), threshold=0.05, target_sizes=[(image.height, image.width)])[0]
        boxes = []
        for score, label, box in zip(result["scores"], result["labels"], result["boxes"]):
            slot = DETR_SLOTS.get(model.config.id2label[int(label)])
            if slot:
                boxes.append(Box(slot, *box.tolist(), float(score)))
        return boxes

    return detect


def outerwear_as_top(detect):
    return lambda image: [replace(b, slot="Top") if b.slot == "Outerwear" else b for b in detect(image)]


def best_per_slot(boxes: list[Box]) -> list[Box]:
    """По одній рамці на слот, для взуття дві: кожен черевик може бути окремою рамкою."""
    kept = []
    for slot in SLOTS.values():
        ranked = sorted((b for b in boxes if b.slot == slot), key=lambda b: b.confidence, reverse=True)
        kept += ranked[:2 if slot == "Shoes" else 1]
    return kept


def matched(truth: Box, predictions: list[Box]) -> bool:
    inside = [p for p in predictions if p.slot == truth.slot
              and truth.x1 <= (p.x1 + p.x2) / 2 <= truth.x2 and truth.y1 <= (p.y1 + p.y2) / 2 <= truth.y2]
    return bool(inside) and iou(truth, merge_boxes(truth.slot, inside)) >= 0.5


def run_detector(detect, collages) -> tuple[list[list[Box]], float]:
    """Рамки на кожне фото і середній час детекції в мс (без декодування файлу)."""
    detections, spent = [], 0.0
    for source, _ in collages:
        image = open_image(source)
        started = time.perf_counter()
        detections.append(detect(image))
        spent += time.perf_counter() - started
    return detections, 1000 * spent / len(collages)


def evaluate(detections: list[list[Box]], collages, thresholds: list[float], single: bool = False) -> list[dict]:
    rows = []
    for threshold in thresholds:
        found: dict[str, list[bool]] = {s: [] for s in SLOTS.values()}
        complete, false_boxes, total_boxes = 0, 0, 0
        for (_, truth), predicted in zip(collages, detections):
            kept = [p for p in predicted if p.confidence >= threshold]
            if single:
                kept = best_per_slot(kept)
            hits = [matched(t, kept) for t in truth]
            for t, hit in zip(truth, hits):
                found[t.slot].append(hit)
            complete += all(hits)
            total_boxes += len(kept)
            false_boxes += sum(not any(p.slot == t.slot and iou(p, t) > 0.1 for t in truth) for p in kept)
        everything = [h for values in found.values() for h in values]
        rows.append({"threshold": threshold, "item_recall": float(np.mean(everything)),
                     "complete_outfits": complete / len(collages),
                     "false_box_share": false_boxes / max(total_boxes, 1),
                     "recall_by_slot": {s: float(np.mean(v)) if v else None for s, v in found.items()}})
    return rows


def report_table(rows: list[dict]) -> list[str]:
    slots = list(SLOTS.values())
    lines = ["| Поріг conf | Знайдено речей | Образів знайдено повністю | Хибних рамок | " + " | ".join(slots) + " |",
             "|---|---|---|---|" + "---|" * len(slots)]
    for r in rows:
        by_slot = [r["recall_by_slot"][s] for s in slots]
        lines.append(f"| {r['threshold']:.2f} | {r['item_recall']:.1%} | {r['complete_outfits']:.1%} | "
                     f"{r['false_box_share']:.1%} | " + " | ".join("—" if v is None else f"{v:.0%}" for v in by_slot) + " |")
    return lines


def main() -> int:
    config = load_config()
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--dataset", choices=["collages", "fashionpedia", "atr"], default="collages")
    parser.add_argument("--detector", choices=["yolo", "detr"], default="yolo")
    parser.add_argument("--outfits", type=int, default=300, help="fashionpedia, atr: 0 = all whole-outfit photos")
    parser.add_argument("--split", default="test", help="fashionpedia split: val or test")
    parser.add_argument("--fashionpedia-root", type=Path, default=FASHIONPEDIA_ROOT)
    parser.add_argument("--atr-parquet", type=Path, default=data_dir(config) / "datasets" / "atr" / "train-00000-of-00002.parquet")
    parser.add_argument("--detr-path", type=Path, default=PARSE_DETECTOR_DIR)
    parser.add_argument("--variant", default=config["dataset"]["variant"])
    parser.add_argument("--seed", type=int, default=config["train"]["seed"])
    parser.add_argument("--out-dir", type=Path, default=data_dir(config) / "eval" / "detection")
    parser.add_argument("--device", default="cuda" if torch.cuda.is_available() else "cpu")
    parser.add_argument("--samples", type=int, default=6, help="save N collages with detections for manual review")
    args = parser.parse_args()

    if args.dataset == "fashionpedia":
        collages = load_fashionpedia(args.fashionpedia_root, args.split, args.outfits, args.seed)
        title = f"# Детекція речей на фото людей (Fashionpedia {args.split}, цілі образи)"
    elif args.dataset == "atr":
        collages = load_atr(args.atr_parquet, args.outfits, args.seed)
        title = "# Детекція речей на фото людей (ATR, цілі образи, Outerwear рахується як Top)"
    else:
        collages = load_collages(config, args.variant, args.outfits, args.seed)
        title = "# Детекція речей на фото цілого образу (колажі Polyvore test)"
    device = str(args.device if args.device == "cpu" else "cuda:0")
    weights = args.detr_path if args.detector == "detr" else REPO_ROOT / config["embedding"]["crop_weights"]
    detect = detr_detector(weights, device) if args.detector == "detr" else yolo_detector(weights, device)
    if args.dataset == "atr":
        detect = outerwear_as_top(detect)

    thresholds = [0.05, 0.15, 0.25, 0.4, 0.6]
    detections, ms_per_photo = run_detector(detect, collages)
    rows = evaluate(detections, collages, thresholds)
    single = evaluate(detections, collages, thresholds, single=True)

    lines = [title, "",
             f"Фото: {len(collages)}, речей: {sum(len(t) for _, t in collages)}. Детектор: `{weights.name}`. "
             f"Час: {ms_per_photo:.0f} мс на фото ({args.device}).", "",
             "## Усі рамки", "", *report_table(rows), "", "## Найкраща рамка на слот", "", *report_table(single)]
    out_dir = args.out_dir / f"{datetime.now():%Y%m%d_%H%M%S}"
    out_dir.mkdir(parents=True, exist_ok=True)
    (out_dir / "report.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    metrics = {"ms_per_photo": ms_per_photo, "all_boxes": rows, "best_per_slot": single}
    (out_dir / "metrics.json").write_text(json.dumps(metrics, indent=2), encoding="utf-8")
    for k, (image, _) in enumerate(collages[:args.samples]):
        open_image(image).save(out_dir / f"sample_{k}.jpg", quality=92)
    print("\n".join(lines))
    print(f"report: {out_dir}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
