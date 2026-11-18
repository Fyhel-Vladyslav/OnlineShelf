"""Чи розрізняє скорер сумісності образи за кропами з фото людей (фаза 0 плану еталонних образів дизайнера).

    python -m src.eval_reference --outfits 20          # smoke
    python -m src.eval_reference --split val --outfits 0

Скорер навчено на предметних фото Polyvore, а еталони дизайнера — це кропи з фото людей (детектор образів
ParseImageService + FashionCLIP). Якщо модель на таких кропах не відрізняє справжній образ від образу із заміненою
річчю, приріст оцінки в порадах Gap Analysis буде шумом.

Експерименти (усі на підобразах за шаблонами OfferService: Top+Bottom+Shoes(+Outerwear) або FullBody+Shoes(+Outerwear)):
  A  кропи людей: справжній образ з фото проти образу, де 1 (A1) або 2 (A2) речі замінено кропами того самого слота
     з інших фото. Головна цифра для рішення про фазу 4.
  B1 Polyvore test: справжній образ проти заміни на випадкову річ Polyvore (еталон, збігається з AUC підобразів
     у src.eval).
  B2 Polyvore test: справжній образ проти заміни однієї речі на кроп людини. Якщо AUC B2 помітно вищий за B1, модель
     впізнає «чужий» домен, а не несумісність.
  B3 кропи людей: справжній образ проти заміни однієї речі на предметне фото Polyvore, те саме застереження.
Кропи одного фото мають спільне освітлення й тло, тому A може завищувати AUC; саме для цього є B2 і B3.
Звіт → ml/data/eval/reference/<час>/report.md.
"""

from __future__ import annotations

import argparse
import json
import random
import sys
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path

import numpy as np
import torch
from PIL import Image

from src.data.embed import embedding_suffix
from src.data.polyvore import REPO_ROOT, SlotMapper, data_dir, load_config, load_item_categories, polyvore_root
from src.data.sampler import Graph, template_subgraph
from src.eval import load_checkpoint
from src.eval_detection import FASHIONPEDIA_ROOT, PARSE_DETECTOR_DIR, load_fashionpedia
from src.evaluation import model_scorer, roc_auc
from src.train import load_split_table

PARSE_ROOT = REPO_ROOT / "backend/Microservices/ParseImageService"
sys.path.insert(0, str(PARSE_ROOT))

# Чекпоінт із MODEL_CARD.md: саме він лежить в OutfitNetworkService/Models/outfit_gnn.onnx
DEFAULT_CHECKPOINT = REPO_ROOT / "ml/data/runs/20261025_213839_pairwise_fashion-clip_yolo/best.pt"
BOOTSTRAP_ROUNDS = 200


@dataclass
class Pool:
    """Вектори, слоти і походження речей: рядки Polyvore (origin = -1) і кропи людей (origin = номер фото)."""
    vectors: np.ndarray
    slots: np.ndarray
    has_embedding: np.ndarray
    origin: np.ndarray

    def by_slot(self, mask: np.ndarray) -> dict[int, np.ndarray]:
        return {int(s): np.flatnonzero(mask & (self.slots == s)) for s in np.unique(self.slots[mask])}


def extract_crops(photos: list[Path], slot_ids: dict[str, int], cache: Path, device: str):
    """Рамки детектора образів → кроп → FashionCLIP, як AnalyzeOutfit у Parse. Результат кешується."""
    if cache.exists():
        stored = np.load(cache)
        print(f"crops from cache {cache.name}: {len(stored['slots'])} items")
        return stored["vectors"], stored["slots"], stored["photo"]

    from app.models.material_detector.material_detector import MaterialDetector
    from app.models.outfit_detector.outfit_detector import OutfitDetector
    from app.models.visual_embedder.visual_embedder import VisualEmbedder

    detector = OutfitDetector(PARSE_DETECTOR_DIR)
    detector.model.to(device)
    clip = MaterialDetector()
    clip.model.to(device)
    embedder = VisualEmbedder(clip.model, clip.processor, "eval")

    vectors, slots, photo_of = [], [], []
    for k, path in enumerate(photos):
        image = np.array(Image.open(path).convert("RGB"))
        for item in detector.detect(image):
            vectors.append(embedder.embed(item["crop_bgr"]))
            slots.append(slot_ids[item["slot"]])
            photo_of.append(k)
        if (k + 1) % 50 == 0:
            print(f"  {k + 1}/{len(photos)} photos", flush=True)
    result = (np.array(vectors, dtype=np.float32), np.array(slots, dtype=np.int64), np.array(photo_of, dtype=np.int64))
    cache.parent.mkdir(parents=True, exist_ok=True)
    np.savez(cache, vectors=result[0], slots=result[1], photo=result[2])
    return result


def swap(rows: list[int], pool: Pool, candidates: dict[int, np.ndarray], count: int, rng: random.Random,
         forbidden_origin: int | None = None) -> list[int] | None:
    """Заміна `count` речей на випадкові того самого слота з `candidates` (не з тих самих фото, не вже в образі)."""
    count = min(count, len(rows) - 1)
    rows_new = list(rows)
    for position in rng.sample(range(len(rows)), count):
        options = candidates.get(int(pool.slots[rows[position]]))
        if options is None:
            return None
        for _ in range(30):
            candidate = int(options[rng.randrange(len(options))])
            if candidate not in rows_new and (forbidden_origin is None or pool.origin[candidate] != forbidden_origin):
                rows_new[position] = candidate
                break
        else:
            return None
    return rows_new


def template_graphs(groups: list[list[int]], pool: Pool, rng: random.Random) -> list[list[int]]:
    out = []
    for rows in groups:
        sub = template_subgraph(rows, pool.slots, rng)
        if sub is not None and len(sub) >= 3:
            out.append(sub)
    return out


def paired_auc(scorer, pool_table, positives: list[list[int]], negatives: list[list[int] | None]) -> dict:
    """AUC позитивів проти негативів і довірчий інтервал бутстрепом по парах."""
    pairs = [(p, n) for p, n in zip(positives, negatives) if n is not None]
    pos = scorer([Graph(p, [True] * len(p), 1) for p, _ in pairs])
    neg = scorer([Graph(n, [True] * len(n), 0) for _, n in pairs])
    labels = np.r_[np.ones(len(pos)), np.zeros(len(neg))]
    scores = np.r_[pos, neg]
    rng = np.random.default_rng(0)
    boot = []
    for _ in range(BOOTSTRAP_ROUNDS):
        idx = rng.integers(0, len(pos), len(pos))
        boot.append(roc_auc(np.r_[np.ones(len(idx)), np.zeros(len(idx))], np.r_[pos[idx], neg[idx]]))
    return {"auc": roc_auc(labels, scores), "ci_low": float(np.percentile(boot, 2.5)),
            "ci_high": float(np.percentile(boot, 97.5)), "pairs": len(pairs),
            "mean_positive": float(pos.mean()), "mean_negative": float(neg.mean()),
            "std_positive": float(pos.std()), "std_negative": float(neg.std())}


def verdict(auc: float) -> str:
    if auc >= 0.70:
        return "≥ 0.70: модель оцінює поради, як описано в плані"
    if auc >= 0.60:
        return "0.60–0.70: модель лише допомагає сортувати, поради базуються на схожості й частоті поєднань"
    return "< 0.60: поради без моделі, донавчання стає пріоритетом"


def main() -> int:
    config = load_config()
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--checkpoint", type=Path, default=DEFAULT_CHECKPOINT)
    parser.add_argument("--split", default="val", help="Fashionpedia: val або test (YOLO їх не бачила)")
    parser.add_argument("--outfits", type=int, default=0, help="фото людей, 0 = усі цілі образи")
    parser.add_argument("--fashionpedia-root", type=Path, default=FASHIONPEDIA_ROOT)
    parser.add_argument("--polyvore-split", default="test")
    parser.add_argument("--variant", default=config["dataset"]["variant"])
    parser.add_argument("--seed", type=int, default=config["train"]["seed"])
    parser.add_argument("--emb-dir", type=Path, default=data_dir(config) / "embeddings")
    parser.add_argument("--out-dir", type=Path, default=data_dir(config) / "eval" / "reference")
    parser.add_argument("--device", default="cuda" if torch.cuda.is_available() else "cpu")
    args = parser.parse_args()

    device = torch.device(args.device)
    model, checkpoint = load_checkpoint(args.checkpoint, device)
    suffix = checkpoint.get("emb_suffix", embedding_suffix(config["embedding"]["model"],
                                                           config["embedding"].get("crop", "none")))
    print(f"checkpoint {args.checkpoint.parent.name}, embedding suffix '{suffix}'")

    root = polyvore_root(config)
    data, table = load_split_table(root, args.emb_dir, args.variant, args.polyvore_split,
                                   load_item_categories(root), SlotMapper(config), config["embedding"]["dim"],
                                   None, suffix)

    photos = [path for path, _ in load_fashionpedia(args.fashionpedia_root, args.split, args.outfits, args.seed)]
    cache = args.out_dir / f"crops_{args.split}_{len(photos)}.npz"
    crop_vectors, crop_slots, crop_photo = extract_crops(photos, config["outfit_slots"], cache,
                                                         args.device if args.device == "cpu" else "cuda:0")

    # Спільна таблиця: спершу Polyvore, потім кропи людей
    polyvore_count = len(table.ids)
    pool = Pool(np.vstack([table.vectors, crop_vectors]), np.r_[table.slots, crop_slots],
                np.r_[table.has_embedding, np.ones(len(crop_slots), dtype=np.float32)],
                np.r_[np.full(polyvore_count, -1, dtype=np.int64), crop_photo])
    is_person = pool.origin >= 0
    person_by_slot, polyvore_by_slot = pool.by_slot(is_person), pool.by_slot(~is_person)

    class PoolTable:
        vectors, slots, has_embedding = pool.vectors, pool.slots, pool.has_embedding

    scorer = model_scorer(model, PoolTable, device)
    rng = random.Random(args.seed)

    person_groups = [list(np.flatnonzero(pool.origin == k)) for k in range(len(photos))]
    person_pos = template_graphs(person_groups, pool, rng)
    person_photo = [int(pool.origin[rows[0]]) for rows in person_pos]
    polyvore_groups = [list(table.rows(o.item_ids)) for o in data.outfits]
    polyvore_pos = template_graphs(polyvore_groups, pool, rng)
    print(f"template outfits: {len(person_pos)} of {len(photos)} people photos, {len(polyvore_pos)} Polyvore")

    results = {
        "A1 люди: заміна 1 речі на кроп з іншого фото": paired_auc(
            scorer, PoolTable, person_pos,
            [swap(r, pool, person_by_slot, 1, rng, p) for r, p in zip(person_pos, person_photo)]),
        "A2 люди: заміна 2 речей на кропи з інших фото": paired_auc(
            scorer, PoolTable, person_pos,
            [swap(r, pool, person_by_slot, 2, rng, p) for r, p in zip(person_pos, person_photo)]),
        "B1 Polyvore: заміна 1 речі на річ Polyvore (еталон)": paired_auc(
            scorer, PoolTable, polyvore_pos, [swap(r, pool, polyvore_by_slot, 1, rng) for r in polyvore_pos]),
        "B2 Polyvore: заміна 1 речі на кроп людини": paired_auc(
            scorer, PoolTable, polyvore_pos, [swap(r, pool, person_by_slot, 1, rng) for r in polyvore_pos]),
        "B3 люди: заміна 1 речі на річ Polyvore": paired_auc(
            scorer, PoolTable, person_pos, [swap(r, pool, polyvore_by_slot, 1, rng) for r in person_pos]),
    }

    a1 = results["A1 люди: заміна 1 речі на кроп з іншого фото"]
    leak_polyvore = (results["B2 Polyvore: заміна 1 речі на кроп людини"]["auc"]
                     - results["B1 Polyvore: заміна 1 речі на річ Polyvore (еталон)"]["auc"])
    leak_person = results["B3 люди: заміна 1 речі на річ Polyvore"]["auc"] - a1["auc"]
    absolute_shift = a1["mean_positive"] - results["B1 Polyvore: заміна 1 речі на річ Polyvore (еталон)"]["mean_positive"]

    lines = [f"# Скорер на кропах із фото людей (Fashionpedia {args.split}, Polyvore {args.polyvore_split})", "",
             f"Чекпоінт `{args.checkpoint.parent.name}`, ембединги `{suffix}`. Фото людей: {len(photos)}, з підобразом за "
             f"шаблоном: {len(person_pos)}, кропів: {len(crop_slots)}. Підобразів Polyvore: {len(polyvore_pos)}.", "",
             "| Експеримент | Пар | AUC | 95% ІД | Середній скор позитиву | Середній скор негативу |",
             "|---|---|---|---|---|---|"]
    for name, r in results.items():
        lines.append(f"| {name} | {r['pairs']} | **{r['auc']:.3f}** | {r['ci_low']:.3f}–{r['ci_high']:.3f} | "
                     f"{r['mean_positive']:.3f} | {r['mean_negative']:.3f} |")
    lines += ["", "## Висновок", "",
              f"- AUC A1 = {a1['auc']:.3f}: {verdict(a1['auc'])}.",
              f"- Зсув скору позитивів «люди» проти Polyvore: {absolute_shift:+.3f}. Якщо він сильно від'ємний, абсолютні "
              f"оцінки порад не можна порівнювати з оцінками образів з гардероба, лише відносний приріст.",
              f"- Ознака впізнавання домену: B2 − B1 = {leak_polyvore:+.3f}, B3 − A1 = {leak_person:+.3f}. Чим більше "
              f"додатні значення, тим більше AUC обумовлений різницею фото, а не сумісністю.", ""]

    out_dir = args.out_dir / f"{datetime.now():%Y%m%d_%H%M%S}"
    out_dir.mkdir(parents=True, exist_ok=True)
    (out_dir / "report.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    (out_dir / "metrics.json").write_text(json.dumps(
        {"checkpoint": str(args.checkpoint), "emb_suffix": suffix, "photos": len(photos), "results": results,
         "leak_polyvore": leak_polyvore, "leak_person": leak_person, "absolute_shift": absolute_shift},
        ensure_ascii=False, indent=2), encoding="utf-8")
    print("\n".join(lines))
    print(f"report: {out_dir}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
