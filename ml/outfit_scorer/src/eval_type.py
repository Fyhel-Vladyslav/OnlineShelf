"""Чи визначає FashionCLIP точний тип речі на кропах детектора образів ParseImageService.

    python -m src.eval_type --split val

На фото людей із Fashionpedia детектор образів знаходить речі, а TypeClassifier обирає тип. Правильна відповідь —
розмічена річ того самого широкого класу з IoU ≥ 0.5. Для порівняння на тих самих речах: клас YOLO з найкращим
перетином і найчастіший тип групи. Звіт → ml/data/eval/type/<час>/report.md.
"""

from __future__ import annotations

import argparse
import json
import random
import sys
from collections import Counter, defaultdict
from datetime import datetime
from pathlib import Path

import numpy as np
import torch
import yaml
from PIL import Image

from src.data.polyvore import REPO_ROOT, data_dir, load_config
from src.eval_detection import FASHIONPEDIA_ROOT, PARSE_DETECTOR_DIR, YOLO_SLOTS, Box, iou, is_whole_outfit

PARSE_ROOT = REPO_ROOT / "backend/Microservices/ParseImageService"
sys.path.insert(0, str(PARSE_ROOT))

from app.models.material_detector.material_detector import MaterialDetector  # noqa: E402
from app.models.outfit_detector.outfit_detector import OutfitDetector  # noqa: E402
from app.models.type_classifier.type_classifier import TYPE_PROMPTS, TypeClassifier  # noqa: E402
from app.models.visual_embedder.visual_embedder import VisualEmbedder  # noqa: E402

GROUPS = {name: group for group, prompts in TYPE_PROMPTS.items() if len(prompts) > 1 for name in prompts}


def load_photos(root: Path, split: str, count: int, seed: int) -> list[tuple[Path, list[tuple[str, Box]]]]:
    with open(root / "data.yaml", encoding="utf-8") as fh:
        names = yaml.safe_load(fh)["names"]
    photos = []
    for label in sorted((root / "labels" / split).glob("*.txt")):
        rows = [line.split() for line in label.read_text(encoding="utf-8").splitlines() if line.strip()]
        rows = [(names[int(r[0])], *map(float, r[1:5])) for r in rows]
        if not is_whole_outfit([Box(YOLO_SLOTS[r[0]], 0, 0, 0, 0) for r in rows if r[0] in YOLO_SLOTS]):
            continue
        path = root / "images" / split / f"{label.stem}.jpg"
        with Image.open(path) as image:
            width, height = image.size
        photos.append((path, [(name, Box(name, (x - w / 2) * width, (y - h / 2) * height, (x + w / 2) * width,
                                         (y + h / 2) * height)) for name, x, y, w, h in rows if name in GROUPS]))
    random.Random(seed).shuffle(photos)
    return photos[:count] if count > 0 else photos


def best_truth(box: Box, truth: list[tuple[str, Box]], group: str) -> str | None:
    scored = [(iou(box, b), name) for name, b in truth if GROUPS[name] == group]
    best = max(scored, default=(0.0, None))
    return best[1] if best[0] >= 0.5 else None


def yolo_class(box: Box, result) -> str | None:
    scored = [(iou(box, Box("", *b.xyxy[0].tolist())), result.names[int(b.cls[0].item())]) for b in result.boxes]
    best = max(scored, default=(0.0, None))
    return best[1] if best[0] >= 0.5 else None


def main() -> int:
    config = load_config()
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--split", default="val")
    parser.add_argument("--outfits", type=int, default=0, help="0 = all whole-outfit photos")
    parser.add_argument("--fashionpedia-root", type=Path, default=FASHIONPEDIA_ROOT)
    parser.add_argument("--seed", type=int, default=config["train"]["seed"])
    parser.add_argument("--out-dir", type=Path, default=data_dir(config) / "eval" / "type")
    args = parser.parse_args()

    from ultralytics import YOLO

    device = "cuda:0" if torch.cuda.is_available() else "cpu"
    yolo = YOLO(str(REPO_ROOT / config["embedding"]["crop_weights"]))
    detector = OutfitDetector(PARSE_DETECTOR_DIR)
    detector.model.to(device)
    clip = MaterialDetector()
    clip.model.to(device)
    embedder = VisualEmbedder(clip.model, clip.processor, "eval")
    classifier = TypeClassifier(clip.model, clip.processor)

    # (правильний тип, відповідь FashionCLIP, відповідь YOLO) по кожній зіставленій речі
    answers: dict[str, list[tuple[str, str, str | None]]] = defaultdict(list)
    photos = load_photos(args.fashionpedia_root, args.split, args.outfits, args.seed)
    for path, truth in photos:
        image = np.array(Image.open(path).convert("RGB"))
        result = yolo(image[:, :, ::-1], verbose=False, conf=0.05, device=device)[0]
        for item in detector.detect(image):
            group = item["raw_class_name"]
            box = Box("", *item["box"])
            expected = best_truth(box, truth, group) if group in classifier.text_features else None
            if expected:
                predicted = classifier.predict_type(embedder.embed(item["crop_bgr"]), group)
                answers[group].append((expected, predicted, yolo_class(box, result)))

    rows = []
    for group, group_answers in answers.items():
        expected = [a[0] for a in group_answers]
        rows.append({"group": group, "items": len(expected),
                     "fashion_clip": float(np.mean([a[0] == a[1] for a in group_answers])),
                     "yolo": float(np.mean([a[0] == a[2] for a in group_answers])),
                     "most_common": Counter(expected).most_common(1)[0][1] / len(expected),
                     "distribution": dict(Counter(expected).most_common()),
                     "recall": {name: float(np.mean([a[1] == name for a in group_answers if a[0] == name]))
                                for name in set(expected)}})

    lines = [f"# Точність Type на кропах детектора образів (Fashionpedia {args.split})", "",
             f"Фото: {len(photos)}.", "",
             "| Група | Речей | FashionCLIP | Клас YOLO | Найчастіший тип |", "|---|---|---|---|---|"]
    for r in rows:
        lines.append(f"| {r['group']} | {r['items']} | {r['fashion_clip']:.1%} | {r['yolo']:.1%} | {r['most_common']:.1%} |")
    lines += ["", "| Тип | Частка в групі | Знайдено FashionCLIP |", "|---|---|---|"]
    for r in rows:
        lines += [f"| {name} | {n / r['items']:.1%} | {r['recall'][name]:.1%} |" for name, n in r["distribution"].items()]

    out_dir = args.out_dir / f"{datetime.now():%Y%m%d_%H%M%S}"
    out_dir.mkdir(parents=True, exist_ok=True)
    (out_dir / "report.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    (out_dir / "metrics.json").write_text(json.dumps(rows, indent=2), encoding="utf-8")
    print("\n".join(lines))
    print(f"report: {out_dir}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
