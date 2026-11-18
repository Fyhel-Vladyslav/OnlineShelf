"""Тестовий гардероб з фото для перевірки 5.8: речі з образів Polyvore (disjoint/test, модель їх не бачила).

    python -m src.seed_polyvore_wardrobe --user-id <guid> --outfits 6 --dresses 2

Скрипт нічого не пише в БД. Він готує в ml/data/sanity/polyvore_seed/:
  images/{guid}_big.webp, {guid}_small.webp, {guid}_orig (оригінальний JPEG, з нього backfill рахує
                 ембединг) — для теки ImageService (/app/images = <repo>/images);
  seed.sql     — нова полиця «ТЕСТ Polyvore» і речі без ембедингів (їх дорахує backfill ShelfsService
                 через ParseImageService, тобто тим самим YOLO-кропом і CLIP, що й для фото користувача);
  cleanup.sql  — видалення цих речей і полиці;
  manifest.json — річ → образ Polyvore (еталон: оригінальні образи мають бути вище змішаних).

Беруться образи з рівно одним верхом, низом і взуттям (+ опційно верхній одяг) і сукні + взуття.
"""

from __future__ import annotations

import argparse
import csv
import json
import random
import sys
import uuid
from io import BytesIO
from pathlib import Path

import numpy as np
import pyarrow.parquet as pq
from PIL import Image

from src.data.polyvore import data_dir, load_config, load_outfits, polyvore_root

SHELF_NAME = "ТЕСТ Polyvore"
SMALL_SIZE = (100, 100)  # як у ImageService

# fine-категорія Polyvore → AttributeType з БД (shelf_service.AttributesValues, AttributeId = 3)
# і слот OutfitOfferService (через OutfitGeneration:Slots). Решта fine-категорій пропускається.
TYPE_RULES: dict[str, list[tuple[str, int]]] = {
    "tops": [("sweat", 3), ("sweater", 3), ("hoodie", 3), ("turtleneck", 3), ("t-shirt", 2), ("tshirt", 2),
             ("shirt", 1), ("blouse", 1), ("polo", 1), ("vest", 6), ("top", 2), ("tank", 2), ("tunic", 2)],
    "bottoms": [("skirt", 9), ("shorts", 8), ("jeans", 7), ("pant", 7)],
    "outerwear": [("trench", 10), ("coat", 10), ("parka", 10), ("jacket", 5), ("blazer", 5)],
    "all-body": [("dress", 11), ("gown", 11), ("jumpsuit", 12), ("romper", 12)],
    "shoes": [("", 23)],
}
EXCLUDE_WORDS = ("swim", "pyjama", "sports bra", "suit", "coverup", "slipper", "flip-flop", "bodie")
SLOT_OF = {"tops": "Top", "bottoms": "Bottom", "outerwear": "Outerwear", "all-body": "FullBody", "shoes": "Shoes"}


def attribute_type(semantic: str, fine: str) -> int | None:
    fine = fine.lower()
    if any(w in fine for w in EXCLUDE_WORDS):
        return None
    for word, type_key in TYPE_RULES.get(semantic, []):
        if word in fine:
            return type_key
    return None


def load_fine_names(root: Path) -> dict[str, str]:
    names: dict[str, str] = {}
    with open(root / "categories.csv", encoding="utf-8") as fh:
        for row in csv.reader(fh):
            if len(row) >= 2:
                names.setdefault(row[0], row[1])
    return names


def dominant_color(image: Image.Image) -> str:
    """Середній колір без білого тла продуктових фото → #RRGGBB."""
    pixels = np.asarray(image.convert("RGB").resize((64, 64))).reshape(-1, 3).astype(np.float32)
    foreground = pixels[pixels.min(axis=1) < 235]
    r, g, b = (foreground if len(foreground) else pixels).mean(axis=0)
    return f"#{int(r):02X}{int(g):02X}{int(b):02X}"


def sql_text(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


def main() -> int:
    config = load_config()
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--user-id", required=True, help="UserId власника гардероба (user_service.Users)")
    parser.add_argument("--outfits", type=int, default=6, help="образів Top + Bottom + Shoes (+ Outerwear)")
    parser.add_argument("--with-outerwear", type=int, default=3, help="скільки з них з верхнім одягом")
    parser.add_argument("--dresses", type=int, default=2, help="образів FullBody + Shoes")
    parser.add_argument("--variant", default=config["dataset"]["variant"])
    parser.add_argument("--seed", type=int, default=config["train"]["seed"])
    parser.add_argument("--out-dir", type=Path, default=data_dir(config) / "sanity" / "polyvore_seed")
    args = parser.parse_args()

    root = polyvore_root(config)
    with open(root / "polyvore_item_metadata.json", encoding="utf-8") as fh:
        meta = json.load(fh)
    fine_names = load_fine_names(root)
    outfits, _ = load_outfits(root, args.variant, "test")

    def describe(item_id: str) -> tuple[str, int | None]:
        m = meta.get(item_id, {})
        semantic = str(m.get("semantic_category", ""))
        return semantic, attribute_type(semantic, fine_names.get(str(m.get("category_id", "")), ""))

    rng = random.Random(args.seed)
    rng.shuffle(outfits)
    chosen: list[tuple[str, list[tuple[str, str, int]]]] = []
    need = {"plain": args.outfits - args.with_outerwear, "outer": args.with_outerwear, "dress": args.dresses}
    for outfit in outfits:
        core = [(i, *describe(i)) for i in outfit.item_ids]
        core = [(i, s, t) for i, s, t in core if s in SLOT_OF]
        if any(t is None for _, _, t in core):
            continue
        counts = {s: sum(1 for _, cs, _ in core if cs == s) for s in SLOT_OF}
        if counts["all-body"] == 1 and counts["shoes"] == 1 and counts["tops"] + counts["bottoms"] + counts["outerwear"] == 0:
            kind = "dress"
        elif counts["tops"] == 1 and counts["bottoms"] == 1 and counts["shoes"] == 1 and counts["all-body"] == 0:
            kind = {0: "plain", 1: "outer"}.get(counts["outerwear"])
        else:
            kind = None
        if kind and need[kind] > 0:
            need[kind] -= 1
            chosen.append((outfit.set_id, core))
        if not any(need.values()):
            break
    if any(need.values()):
        print(f"not enough matching outfits, missing {need}", file=sys.stderr)
        return 1

    wanted = {i for _, core in chosen for i, _, _ in core}
    images: dict[str, bytes] = {}
    parquet = root / "data" / args.variant / "test.parquet"
    for batch in pq.ParquetFile(parquet).iter_batches(batch_size=2048, columns=["item_id", "image"]):
        ids = batch.column(0).to_pylist()
        blobs = batch.column(1).field("bytes").to_pylist()
        images.update({i: b for i, b in zip(ids, blobs) if i in wanted})
        if len(images) == len(wanted):
            break

    image_dir = args.out_dir / "images"
    image_dir.mkdir(parents=True, exist_ok=True)
    shelf_id = str(uuid.uuid4())
    manifest = {"user_id": args.user_id, "shelf_id": shelf_id, "shelf_name": SHELF_NAME,
                "source": f"Polyvore Outfits {args.variant}/test", "outfits": []}
    rows = []
    for number, (set_id, core) in enumerate(chosen, start=1):
        entry = {"set_id": set_id, "items": []}
        for item_id, semantic, type_key in core:
            m = meta[item_id]
            title = (m.get("title") or m.get("url_name") or item_id).strip()
            slot = SLOT_OF[semantic]
            name = f"PV{number} {slot}: {title}"[:100]
            guid = str(uuid.uuid4())
            image = Image.open(BytesIO(images[item_id])).convert("RGB")
            big, small = f"{guid}_big.webp", f"{guid}_small.webp"
            image.save(image_dir / big, "WEBP")
            thumb = image.copy()
            thumb.thumbnail(SMALL_SIZE)
            thumb.save(image_dir / small, "WEBP")
            (image_dir / f"{guid}_orig").write_bytes(images[item_id])
            color = dominant_color(image)
            rows.append(f"({sql_text(str(uuid.uuid4()))}::uuid, {sql_text(name)}, {sql_text(args.user_id)}::uuid, "
                        f"{sql_text(shelf_id)}::uuid, {sql_text(big)}, {sql_text(small)}, now(), now(), '', "
                        f"{sql_text(color)}, '', 0, 0, 0, {type_key}, false)")
            entry["items"].append({"name": name, "polyvore_item_id": item_id, "slot": slot,
                                   "attribute_type": type_key, "big_image": big, "color": color})
        manifest["outfits"].append(entry)

    seed = [
        "BEGIN;",
        f'INSERT INTO shelf_service."Shelfs" ("Id", "Name", "DateCreated", "UpdatedAt", "UserId", "positionKey") '
        f"VALUES ({sql_text(shelf_id)}::uuid, {sql_text(SHELF_NAME)}, now(), now(), {sql_text(args.user_id)}::uuid, '');",
        'INSERT INTO shelf_service."Items" ("Id", "Name", "UserId", "ShelfId", "BigImage", "SmallImage", "DateCreated", '
        '"UpdatedAt", "positionKey", "AttributeColorMain", "AttributeColorSecond", "AttributeMatterial", '
        '"AttributePattern", "AttributeSeason", "AttributeType", "isFavorite") VALUES',
        ",\n".join(rows) + ";",
        "COMMIT;",
    ]
    (args.out_dir / "seed.sql").write_text("\n".join(seed) + "\n", encoding="utf-8")
    cleanup = [
        "BEGIN;",
        f'DELETE FROM shelf_service."Items" WHERE "ShelfId" = {sql_text(shelf_id)}::uuid;',
        f'DELETE FROM shelf_service."Shelfs" WHERE "Id" = {sql_text(shelf_id)}::uuid;',
        "COMMIT;",
    ]
    (args.out_dir / "cleanup.sql").write_text("\n".join(cleanup) + "\n", encoding="utf-8")
    (args.out_dir / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")

    print(f"{len(rows)} items from {len(chosen)} outfits → {args.out_dir}")
    for entry in manifest["outfits"]:
        print("  " + " + ".join(f"{i['slot']}:{i['name'].split(': ', 1)[1][:40]}" for i in entry["items"]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
