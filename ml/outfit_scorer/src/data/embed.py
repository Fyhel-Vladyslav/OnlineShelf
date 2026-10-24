"""CLIP-ембединги айтемів Polyvore → кеш `ml/data/embeddings/{variant}_{split}.npy` + `.ids.txt`.

Функція ембедингу повторює ParseImageService (`app/models/visual_embedder/visual_embedder.py`):
RGB-картинка → CLIP image processor → `get_image_features` → L2-нормалізація, `openai/clip-vit-base-patch32`.
Код Parse не імпортується: логіка скопійована, паритет перевіряє tests/test_embed_parity.py.
Parse викликає `CLIPProcessor(images=...)`, який делегує в той самий `CLIPImageProcessor`, що й тут.

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
"""

from __future__ import annotations

import argparse
import io
import sys
import time
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

import numpy as np
import pyarrow.parquet as pq
import torch
from PIL import Image

from src.data.polyvore import PARQUET_SPLIT, SPLITS, data_dir, load_config, load_split, polyvore_root


# ----------------------------------------------------------------------------- CLIP


def load_clip(model_name: str, device: torch.device):
    from transformers import CLIPImageProcessor, CLIPModel

    model = CLIPModel.from_pretrained(model_name).eval().to(device)
    processor = CLIPImageProcessor.from_pretrained(model_name)
    return model, processor


def preprocess(processor, image_bytes: bytes) -> np.ndarray:
    """JPEG-байти → pixel_values[3, H, W] (як у Parse: RGB PIL-картинка в processor)."""
    image = Image.open(io.BytesIO(image_bytes)).convert("RGB")
    return processor(images=image, return_tensors="np")["pixel_values"][0]


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
                only: set[str] | None = None) -> None:
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
          f"device {encoder.device}, fp16 {encoder.fp16}, batch {batch_size}, workers {num_workers}", flush=True)

    pin = encoder.device.type == "cuda"
    failed: list[str] = []
    processed = len(writer.done)
    started = time.perf_counter()
    computed = 0

    def prepare(batch):
        ids, images = batch
        arrays, ok_ids = [], []
        for item_id, result in zip(ids, pool.map(lambda b: _safe_preprocess(processor, b), images)):
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
        print(f"{name}: {len(failed)} images failed to decode (stored without embedding): {failed[:10]}")
    count = writer.merge(emb_dir, name)
    print(f"{name}: saved {count} vectors to {final}")


def _safe_preprocess(processor, image_bytes: bytes) -> np.ndarray | None:
    try:
        return preprocess(processor, image_bytes)
    except Exception as exc:  # битий JPEG не має зупиняти багатогодинний прогін
        print(f"\ndecode error: {exc}", file=sys.stderr)
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
    for split in splits:
        name = cache_name(args.variant, split)
        only = None
        if args.limit is not None:
            name += f"_n{args.limit}"
        if args.limit_outfits is not None:
            name += f"_o{args.limit_outfits}"
            only = load_split(root, args.variant, split).limited(args.limit_outfits).referenced_item_ids()
        embed_split(parquet_for(split), args.out_dir, name, model, processor, encoder,
                    args.batch_size, args.num_workers, emb_cfg["shard_size"], args.limit, only)
    return 0


if __name__ == "__main__":
    sys.exit(main())
