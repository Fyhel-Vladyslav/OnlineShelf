"""Перевірка завантаженого Polyvore Outfits (структура HF `mvasil/polyvore-outfits`, див. polyvore.py).

Рахує айтеми, образи, compatibility/FITB, наявність картинок у parquet, розподіл `semantic_category`
з мапінгом на OutfitSlot (скільки відкинуто) і скільки образів дають підобрази під шаблони OfferService.

    python -m src.data.check_dataset              # з ml/outfit_scorer; шлях: POLYVORE_DIR або config.yaml

Звіт друкується і зберігається у ml/data/check_dataset_report.txt.
"""

from __future__ import annotations

import sys
from collections import Counter

import pyarrow.parquet as pq

from src.data.polyvore import (PARQUET_SPLIT, SPLITS, SlotMapper, data_dir, load_config, load_item_categories,
                               load_split, polyvore_root)
from src.data.sampler import BOTTOM, FULL_BODY, SHOES, TOP

SLOT_NAMES = {0: "Unknown", 1: "Top", 2: "Bottom", 3: "FullBody", 4: "Shoes", 5: "Outerwear", 6: "Accessory"}


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    config = load_config()
    root = polyvore_root(config)
    lines: list[str] = []

    def say(text: str = "") -> None:
        print(text, flush=True)
        lines.append(text)

    say(f"Polyvore Outfits: {root}")
    if not (root / "polyvore_item_metadata.json").exists():
        say("polyvore_item_metadata.json not found: dataset is not downloaded (see README)")
        return 2

    mapper = SlotMapper(config)
    categories = load_item_categories(root)
    counts = Counter(categories.values())
    kept = sum(n for c, n in counts.items() if mapper(c) is not None)
    say(f"\nitems in metadata: {len(categories)}")
    say("semantic_category → OutfitSlot:")
    for cat, n in counts.most_common():
        slot = mapper(cat)
        say(f"  {cat:<15} {n:>7}  → {SLOT_NAMES[slot] if slot is not None else 'DROPPED'}")
    say(f"kept {kept} ({kept / len(categories):.1%}), dropped {len(categories) - kept}")

    for variant in ("disjoint", "nondisjoint"):
        if not (root / variant).is_dir():
            say(f"\n[{variant}] missing")
            continue
        say(f"\n[{variant}]")
        for split in SPLITS:
            data = load_split(root, variant, split)
            items = data.item_ids()
            sizes = [len(o.item_ids) for o in data.outfits]
            parquet = root / "data" / variant / f"{PARQUET_SPLIT[split]}.parquet"
            if parquet.exists():
                with_image = set(pq.read_table(parquet, columns=["item_id"]).column(0).to_pylist())
                image_info = f"with image {len(items & with_image)}/{len(items)}"
            else:
                image_info = f"parquet missing: {parquet.name}"
            unknown = sum(1 for i in items if mapper(categories.get(i, "")) is None)

            templates = Counter()
            any_template = 0
            for o in data.outfits:
                slots = {mapper(categories.get(i, "")) for i in o.item_ids}
                tbs, fs = {TOP, BOTTOM, SHOES} <= slots, {FULL_BODY, SHOES} <= slots
                templates["Top+Bottom+Shoes"] += tbs
                templates["FullBody+Shoes"] += fs
                any_template += tbs or fs
            labels = Counter(q.label for q in data.compatibility)
            say(f"  {split:<5} outfits {len(data.outfits):>6} | items {len(items):>6}, {image_info}, "
                f"dropped category {unknown}")
            say(f"        items/outfit min {min(sizes)} mean {sum(sizes) / len(sizes):.2f} max {max(sizes)} | "
                f"outfits with template {any_template} ({any_template / len(data.outfits):.0%}): {dict(templates)}")
            say(f"        compatibility {len(data.compatibility)} (pos {labels[1]}, neg {labels[0]}, "
                f"skipped {data.skipped['compatibility']}) | FITB {len(data.fitb)} "
                f"(skipped {data.skipped['fitb']})")

    out = data_dir(config) / "check_dataset_report.txt"
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"\nreport saved to {out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
