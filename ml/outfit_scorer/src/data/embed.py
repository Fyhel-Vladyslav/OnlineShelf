"""CLIP-ембединги айтемів Polyvore → кеш `ml/data/embeddings/{variant}_{split}.npy` + `.ids.txt`.

Функція ембедингу повторює ParseImageService (`app/models/visual_embedder/visual_embedder.py`):
RGB-картинка → CLIP image processor → `get_image_features` → L2-нормалізація, `openai/clip-vit-base-patch32`.
Код Parse не імпортується: логіка скопійована, паритет перевіряє tests/test_embed.py.
Parse викликає `CLIPProcessor(images=...)`, який делегує в той самий `CLIPImageProcessor`, що й тут.

Перед CLIP Parse вирізає річ YOLO-детектором (`app/models/clothing_detector/clothing_detector.py`):
`embedding.crop: yolo` повторює цей кроп (ті самі ваги, conf, дозволені класи, найкращий бокс, а якщо
нічого не знайдено — усе фото), щоб модель навчалась на тих самих векторах, що прийдуть у сервіс.
Кеш з кропом пишеться з суфіксом `_yolo` (`{variant}_{split}_yolo.npy`), іншої моделі ембедингів — ще й
з її назвою (`--model patrickjohncyh/fashion-clip` → `{variant}_{split}_fashion-clip_yolo.npy`).

Картинки беруться з `data/{variant}/{train|validation|test}.parquet` (рядок = item_id + JPEG-байти).
Декодування й препроцесинг ідуть у пулі потоків (PIL і numpy відпускають GIL). DataLoader з процесами
тут не підходить: на Windows кожен процес-воркер копіював би весь parquet (один row group на файл).

Прогрес зберігається частинами (`{name}.parts/part_00000.npy` + `.ids.txt`); повторний запуск
пропускає вже пораховані айтеми. Після останньої частини вони зливаються в один файл.

    python -m src.data.embed --split train                 # з ml/outfit_scorer
    python -m src.data.embed --split all
    python -m src.data.embed --split valid --limit 500     # бенчмарк швидкості
    python -m src.data.embed --limit-outfits 200           # smoke: лише айтеми перших 200 образів/питань
    python -m src.data.embed --check-fp16 256              # паритет fp16 ↔ fp32
    python -m src.data.embed --crop none                   # без YOLO-кропу (кеш без суфікса)
"""

from __future__ import annotations

import argparse
import io
import re
import sys
import time
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

import numpy as np
import pyarrow.parquet as pq
import torch
from PIL import Image

from src.data.polyvore import PARQUET_SPLIT, REPO_ROOT, SPLITS, data_dir, load_config, load_split, polyvore_root

CROP_MODES = ("yolo", "none")
DEFAULT_MODEL = "openai/clip-vit-base-patch32"


def crop_suffix(crop: str) -> str:
    """Суфікс кешу ембедингів для режиму кропу."""
    return "_yolo" if crop == "yolo" else ""


def model_suffix(model: str) -> str:
    """Суфікс кешу для моделі ембедингів: DEFAULT_MODEL — без суфікса (наявні кеші), інші — _<назва>,
    напр. patrickjohncyh/fashion-clip → _fashion-clip."""
    if model == DEFAULT_MODEL:
        return ""
    return "_" + re.sub(r"[^a-z0-9]+", "-", Path(model).name.lower()).strip("-")


def embedding_suffix(model: str, crop: str) -> str:
    """Повний суфікс кешу `{variant}_{split}{suffix}`; train/eval за замовчуванням беруть його з config."""
    return model_suffix(model) + crop_suffix(crop)


# ----------------------------------------------------------------------------- YOLO-кроп


class YoloCropper:
    """Кроп речі як у ParseImageService.ClothingDetector.detect_and_crop.

    RGB → BGR → YOLO(conf) → бокс з найвищою впевненістю серед allowed_classes (None — будь-який клас)
    → кроп `image_bgr[y1:y2, x1:x2]`; без боксу — усе фото. Повертає RGB-кроп або None для порожнього
    кропу (Parse у такому разі не рахує ембединг). Не потокобезпечний: викликати з одного потоку.
    """

    def __init__(self, weights: Path, conf: float, allowed_classes: list[str] | None, device: str) -> None:
        from ultralytics import YOLO

        self.model = YOLO(str(weights))
        self.conf = conf
        self.allowed = set(allowed_classes) if allowed_classes else None
        self.device = device

    def __call__(self, image_rgb: np.ndarray) -> np.ndarray | None:
        import cv2

        image_bgr = cv2.cvtColor(image_rgb, cv2.COLOR_RGB2BGR)
        result = self.model(image_bgr, verbose=False, conf=self.conf, device=self.device)[0]
        best_box, highest_conf = None, 0.0
        for box in result.boxes:
            name = result.names[int(box.cls[0].item())]
            conf = float(box.conf[0].item())
            if (self.allowed is None or name in self.allowed) and conf > highest_conf:
                highest_conf = conf
                best_box = box.xyxy[0].cpu().numpy().astype(int)
        if best_box is None:
            crop = image_bgr
        else:
            x1, y1, x2, y2 = best_box
            crop = image_bgr[y1:y2, x1:x2]
        if crop.size == 0:
            return None
        return cv2.cvtColor(crop, cv2.COLOR_BGR2RGB)


def build_cropper(emb_cfg: dict, crop: str, device: torch.device) -> YoloCropper | None:
    if crop == "none":
        return None
    weights = Path(emb_cfg["crop_weights"])
    return YoloCropper(weights if weights.is_absolute() else REPO_ROOT / weights, emb_cfg["crop_conf"],
                       emb_cfg.get("crop_allowed_classes"), device.type if device.type == "cpu" else str(device))


# ----------------------------------------------------------------------------- CLIP


def load_clip(model_name: str, device: torch.device):
    from transformers import CLIPImageProcessor, CLIPModel

    model = CLIPModel.from_pretrained(model_name).eval().to(device)
    processor = CLIPImageProcessor.from_pretrained(model_name)
    return model, processor


def decode(image_bytes: bytes) -> np.ndarray:
    """Байти картинки → RGB ndarray (як ParseImageService._decode_image)."""
    return np.array(Image.open(io.BytesIO(image_bytes)).convert("RGB"))


def preprocess_rgb(processor, image_rgb: np.ndarray) -> np.ndarray:
    """RGB ndarray → pixel_values[3, H, W] (як VisualEmbedder.embed: PIL-картинка в processor)."""
    return processor(images=Image.fromarray(image_rgb), return_tensors="np")["pixel_values"][0]


def preprocess(processor, image_bytes: bytes, cropper: YoloCropper | None = None) -> np.ndarray | None:
    """JPEG-байти → (YOLO-кроп) → pixel_values[3, H, W]; None, якщо кроп порожній."""
    image = decode(image_bytes)
    if cropper is not None:
        image = cropper(image)
        if image is None:
            return None
    return preprocess_rgb(processor, image)


def image_features(model, pixel_values: torch.Tensor) -> torch.Tensor:
    """get_image_features + L2-нормалізація, як у VisualEmbedder.embed."""
    features = model.get_image_features(pixel_values=pixel_values)
    # transformers 5.x повертає BaseModelOutputWithPooling (проєктований вектор у pooler_output), 4.x — тензор
    if not isinstance(features, torch.Tensor):
        features = features.pooler_output
    features = features.float()
    return features / features.norm(dim=-1, keepdim=True)


class Encoder:
    """Батчевий енкодер з fp16 (autocast) і автоматичним зменшенням батча при CUDA OOM."""

    def __init__(self, model, device: torch.device, fp16: bool) -> None:
        self.model = model
        self.device = device
        self.fp16 = fp16 and device.type == "cuda"
        self.chunk: int | None = None  # максимальний розмір батча, що влазить у пам'ять

    @torch.no_grad()
    def _run(self, pixel_values: torch.Tensor) -> torch.Tensor:
        pixel_values = pixel_values.to(self.device, non_blocking=True)
        if self.fp16:
            with torch.autocast("cuda", dtype=torch.float16):
                return image_features(self.model, pixel_values).cpu()
        return image_features(self.model, pixel_values).cpu()

    def __call__(self, pixel_values: torch.Tensor) -> torch.Tensor:
        n = pixel_values.shape[0]
        step = self.chunk or n
        outputs = []
        start = 0
        while start < n:
            part = pixel_values[start:start + step]
            try:
                outputs.append(self._run(part))
                start += step
            except torch.cuda.OutOfMemoryError:
                if step == 1:
                    raise
                torch.cuda.empty_cache()
                step = max(1, step // 2)
                self.chunk = step
                print(f"CUDA OOM: batch size reduced to {step}", flush=True)
        return torch.cat(outputs)


# ----------------------------------------------------------------------------- cache


def cache_name(variant: str, split: str) -> str:
    return f"{variant}_{split}"


def load_embeddings(emb_dir: Path, name: str) -> tuple[list[str], np.ndarray]:
    ids = (emb_dir / f"{name}.ids.txt").read_text(encoding="utf-8").split()
    vectors = np.load(emb_dir / f"{name}.npy")
    if len(ids) != vectors.shape[0]:
        raise ValueError(f"{name}: {len(ids)} ids but {vectors.shape[0]} vectors")
    return ids, vectors


class ShardWriter:
    """Пише частини кешу і пам'ятає вже пораховані id для продовження після зупинки."""

    def __init__(self, parts_dir: Path, shard_size: int) -> None:
        self.parts_dir = parts_dir
        self.shard_size = shard_size
        parts_dir.mkdir(parents=True, exist_ok=True)
        self.done: set[str] = set()
        self.next_part = 0
        for ids_file in sorted(parts_dir.glob("part_*.ids.txt")):
            if (ids_file.with_name(ids_file.name.replace(".ids.txt", ".npy"))).exists():
                self.done.update(ids_file.read_text(encoding="utf-8").split())
                self.next_part += 1
        self.ids: list[str] = []
        self.vectors: list[np.ndarray] = []

    def add(self, ids: list[str], vectors: np.ndarray) -> None:
        self.ids.extend(ids)
        self.vectors.append(vectors)
        if len(self.ids) >= self.shard_size:
            self.flush()

    def flush(self) -> None:
        if not self.ids:
            return
        stem = self.parts_dir / f"part_{self.next_part:05d}"
        # Спершу вектори, потім ids: частина без ids.txt вважається недописаною і рахується заново.
        np.save(stem.with_suffix(".npy"), np.concatenate(self.vectors).astype(np.float32))
        stem.with_suffix(".ids.txt").write_text("\n".join(self.ids) + "\n", encoding="utf-8")
        self.done.update(self.ids)
        self.next_part += 1
        self.ids, self.vectors = [], []

    def merge(self, emb_dir: Path, name: str) -> int:
        all_ids: list[str] = []
        arrays: list[np.ndarray] = []
        for ids_file in sorted(self.parts_dir.glob("part_*.ids.txt")):
            all_ids.extend(ids_file.read_text(encoding="utf-8").split())
            arrays.append(np.load(ids_file.with_name(ids_file.name.replace(".ids.txt", ".npy"))))
        vectors = np.concatenate(arrays) if arrays else np.zeros((0, 0), np.float32)
        np.save(emb_dir / f"{name}.npy", vectors)
        (emb_dir / f"{name}.ids.txt").write_text("\n".join(all_ids) + "\n", encoding="utf-8")
        for path in self.parts_dir.iterdir():
            path.unlink()
        self.parts_dir.rmdir()
        return len(all_ids)


# ----------------------------------------------------------------------------- run


def iter_parquet(path: Path, batch_size: int, skip: set[str], limit: int | None,
                 only: set[str] | None = None):
    """(ids, jpeg_bytes) батчами; пропускає вже пораховані id і, якщо задано only, усі інші."""
    taken = 0
    for record_batch in pq.ParquetFile(path).iter_batches(batch_size=batch_size, columns=["item_id", "image"]):
        ids = record_batch.column(0).to_pylist()
        images = record_batch.column(1).field("bytes").to_pylist()
        if limit is not None:
            ids, images = ids[:limit - taken], images[:limit - taken]
        taken += len(ids)
        keep = [(i, b) for i, b in zip(ids, images) if i not in skip and (only is None or i in only)]
        if keep:
            yield [k[0] for k in keep], [k[1] for k in keep]
        if limit is not None and taken >= limit:
            return


def embed_split(parquet_path: Path, emb_dir: Path, name: str, model, processor, encoder: Encoder,
                batch_size: int, num_workers: int, shard_size: int, limit: int | None,
                only: set[str] | None = None, cropper: YoloCropper | None = None) -> None:
    final = emb_dir / f"{name}.npy"
    if final.exists():
        print(f"{name}: already cached in {final}, skipping (delete the file to recompute)")
        return

    total = pq.ParquetFile(parquet_path).metadata.num_rows
    if limit is not None:
        total = min(total, limit)
    if only is not None:
        total = min(total, len(only))
    writer = ShardWriter(emb_dir / f"{name}.parts", shard_size)
    print(f"{name}: {total} items, {len(writer.done)} already done, "
          f"device {encoder.device}, fp16 {encoder.fp16}, batch {batch_size}, workers {num_workers}, "
          f"crop {'yolo' if cropper else 'none'}", flush=True)

    pin = encoder.device.type == "cuda"
    failed: list[str] = []
    processed = len(writer.done)
    started = time.perf_counter()
    computed = 0

    def prepare(batch):
        ids, images = batch
        if cropper is None:
            results = pool.map(lambda b: _safe(preprocess, processor, b), images)
        else:
            # декодування й препроцесинг — у пулі, YOLO — послідовно в цьому потоці (predictor не потокобезпечний)
            decoded = list(pool.map(lambda b: _safe(decode, b), images))
            crops = [None if d is None else _safe(cropper, d) for d in decoded]
            results = pool.map(lambda c: None if c is None else _safe(preprocess_rgb, processor, c), crops)
        arrays, ok_ids = [], []
        for item_id, result in zip(ids, results):
            if result is None:
                failed.append(item_id)
            else:
                ok_ids.append(item_id)
                arrays.append(result)
        tensor = torch.from_numpy(np.stack(arrays)) if arrays else None
        if tensor is not None and pin:
            tensor = tensor.pin_memory()
        return ok_ids, tensor

    with ThreadPoolExecutor(max_workers=max(1, num_workers)) as pool, \
            ThreadPoolExecutor(max_workers=1) as prefetch:
        batches = iter_parquet(parquet_path, batch_size, writer.done, limit, only)
        pending = prefetch.submit(prepare, next(batches, ([], [])))
        while True:
            ok_ids, tensor = pending.result()
            nxt = next(batches, None)
            if nxt is not None:
                pending = prefetch.submit(prepare, nxt)  # препроцесинг наступного батча паралельно з GPU
            if tensor is not None:
                writer.add(ok_ids, encoder(tensor).numpy())
                computed += len(ok_ids)
                processed += len(ok_ids)
                elapsed = time.perf_counter() - started
                rate = computed / elapsed if elapsed else 0.0
                eta = (total - processed) / rate if rate else 0.0
                print(f"\r{name}: {processed}/{total}  {rate:.0f} img/s  ETA {eta / 60:.1f} min",
                      end="", flush=True)
            if nxt is None:
                break
    writer.flush()
    print()
    elapsed = time.perf_counter() - started
    if computed:
        print(f"{name}: {computed} images in {elapsed:.1f} s ({computed / elapsed:.1f} img/s)")
    if failed:
        print(f"{name}: {len(failed)} images failed to decode or crop (stored without embedding): {failed[:10]}")
    count = writer.merge(emb_dir, name)
    print(f"{name}: saved {count} vectors to {final}")


def _safe(fn, *args):
    try:
        return fn(*args)
    except Exception as exc:  # битий JPEG не має зупиняти багатогодинний прогін
        print(f"\n{getattr(fn, '__name__', type(fn).__name__)} error: {exc}", file=sys.stderr)
        return None


def check_fp16(parquet_path: Path, model, processor, device: torch.device, n: int) -> bool:
    if device.type != "cuda":
        print("fp16 check needs CUDA; on CPU embeddings are computed in fp32")
        return False
    ids, images = next(iter_parquet(parquet_path, n, set(), n))
    pixel_values = torch.from_numpy(np.stack([preprocess(processor, b) for b in images]))
    fp32 = Encoder(model, device, fp16=False)(pixel_values)
    fp16 = Encoder(model, device, fp16=True)(pixel_values)
    cos = (fp32 * fp16).sum(dim=-1)
    print(f"fp16 vs fp32 on {len(ids)} images: cosine min {cos.min():.6f}, mean {cos.mean():.6f}")
    ok = bool(cos.min() >= 0.999)
    print("OK: fp16 can be used" if ok else "FAIL: use --precision fp32")
    return ok


def main() -> int:
    config = load_config()
    emb_cfg = config["embedding"]
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--variant", default=config["dataset"]["variant"])
    parser.add_argument("--split", default="all", choices=[*SPLITS, "all"])
    parser.add_argument("--limit", type=int, default=None, help="only the first N items (benchmark / smoke)")
    parser.add_argument("--limit-outfits", type=int, default=None,
                        help="smoke: only items referenced by the first N outfits/questions of each split")
    parser.add_argument("--out-dir", type=Path, default=data_dir(config) / "embeddings")
    parser.add_argument("--model", default=emb_cfg["model"])
    parser.add_argument("--precision", choices=["fp16", "fp32"], default=emb_cfg["precision"])
    parser.add_argument("--batch-size", type=int, default=emb_cfg["batch_size"])
    parser.add_argument("--num-workers", type=int, default=emb_cfg["num_workers"])
    parser.add_argument("--device", default="cuda" if torch.cuda.is_available() else "cpu")
    parser.add_argument("--check-fp16", type=int, metavar="N", default=0, help="compare fp16 and fp32 on N images")
    parser.add_argument("--crop", choices=CROP_MODES, default=emb_cfg.get("crop", "none"),
                        help="yolo: crop the item like ParseImageService before CLIP")
    args = parser.parse_args()

    root = polyvore_root(config)
    device = torch.device(args.device)
    model, processor = load_clip(args.model, device)
    splits = SPLITS if args.split == "all" else (args.split,)

    def parquet_for(split: str) -> Path:
        return root / "data" / args.variant / f"{PARQUET_SPLIT[split]}.parquet"

    if args.check_fp16:
        return 0 if check_fp16(parquet_for(splits[0]), model, processor, device, args.check_fp16) else 1

    args.out_dir.mkdir(parents=True, exist_ok=True)
    encoder = Encoder(model, device, fp16=args.precision == "fp16")
    cropper = build_cropper(emb_cfg, args.crop, device)
    for split in splits:
        name = cache_name(args.variant, split) + embedding_suffix(args.model, args.crop)
        only = None
        if args.limit is not None:
            name += f"_n{args.limit}"
        if args.limit_outfits is not None:
            name += f"_o{args.limit_outfits}"
            only = load_split(root, args.variant, split).limited(args.limit_outfits).referenced_item_ids()
        embed_split(parquet_for(split), args.out_dir, name, model, processor, encoder,
                    args.batch_size, args.num_workers, emb_cfg["shard_size"], args.limit, only, cropper)
    return 0


if __name__ == "__main__":
    sys.exit(main())
