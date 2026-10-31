"""Ембединги: L2-норма, кеш з продовженням і паритет з ParseImageService.VisualEmbedder.

Паритет за замовчуванням перевіряється на крихітній CLIP з випадковими вагами (перевіряє, що
шлях картинка → препроцесинг → get_image_features → нормалізація однаковий). Зі справжньою моделлю:
    CLIP_MODEL=openai/clip-vit-base-patch32 PARITY_IMAGES_DIR=<тека з 3 фото-кропами> pytest tests/test_embed.py
"""

import io
import os
import types
from pathlib import Path

import numpy as np
import pyarrow.parquet as pq
import pytest
import torch
from PIL import Image

from src.data.embed import (Encoder, ShardWriter, YoloCropper, decode, embed_split, load_clip, load_embeddings,
                            preprocess)
from src.data.polyvore import load_config
from tests.synthetic import make_polyvore, make_tiny_clip

REPO_ROOT = Path(__file__).resolve().parents[3]
PARSE_EMBEDDER = REPO_ROOT / "backend/Microservices/ParseImageService/app/models/visual_embedder/visual_embedder.py"
PARSE_DETECTOR = REPO_ROOT / "backend/Microservices/ParseImageService/app/models/clothing_detector/clothing_detector.py"


@pytest.fixture(scope="module")
def clip_dir(tmp_path_factory):
    name = os.environ.get("CLIP_MODEL")
    return name if name else str(make_tiny_clip(tmp_path_factory.mktemp("clip") / "tiny_clip"))


@pytest.fixture(scope="module")
def synthetic_root(tmp_path_factory):
    return make_polyvore(tmp_path_factory.mktemp("polyvore") / "polyvore_outfits",
                         {"train": 20, "valid": 5, "test": 5})


def photo_crops() -> list[bytes]:
    folder = os.environ.get("PARITY_IMAGES_DIR")
    if folder:
        files = sorted(p for p in Path(folder).iterdir() if p.suffix.lower() in {".jpg", ".jpeg", ".png", ".webp"})
        return [p.read_bytes() for p in files[:3]]
    rng = np.random.default_rng(0)
    crops = []
    for w, h in ((180, 240), (300, 220), (96, 128)):
        gradient = np.linspace(0, 255, w)[None, :, None] * np.ones((h, 1, 3)) * rng.uniform(0.3, 1, 3)
        pixels = np.clip(gradient + rng.normal(0, 20, (h, w, 3)), 0, 255).astype(np.uint8)
        buf = io.BytesIO()
        Image.fromarray(pixels).save(buf, "JPEG", quality=92)
        crops.append(buf.getvalue())
    return crops


def test_parity_with_parse_visual_embedder(clip_dir):
    cv2 = pytest.importorskip("cv2")
    if not PARSE_EMBEDDER.exists():
        pytest.skip(f"ParseImageService VisualEmbedder not found: {PARSE_EMBEDDER} (гілка без EmbedClothing)")
    # виконуємо вихідний код без імпорту, щоб не лишати __pycache__ у теці ParseImageService
    module = types.ModuleType("parse_visual_embedder")
    exec(compile(PARSE_EMBEDDER.read_text(encoding="utf-8"), str(PARSE_EMBEDDER), "exec"), module.__dict__)

    model, processor = load_clip(clip_dir, torch.device("cpu"))
    if os.environ.get("CLIP_MODEL"):
        from transformers import CLIPProcessor  # у Parse саме CLIPProcessor
        parse_processor = CLIPProcessor.from_pretrained(clip_dir)
    else:
        parse_processor = processor
    parse = module.VisualEmbedder(model, parse_processor, "test")
    encoder = Encoder(model, torch.device("cpu"), fp16=False)

    for jpeg in photo_crops():
        # Parse: PIL → RGB ndarray (_decode_image) → BGR (детектор, кроп) → VisualEmbedder.embed
        rgb = np.array(Image.open(io.BytesIO(jpeg)).convert("RGB"))
        expected = np.array(parse.embed(cv2.cvtColor(rgb, cv2.COLOR_RGB2BGR)), dtype=np.float32)
        actual = encoder(torch.from_numpy(preprocess(processor, jpeg)[None]))[0].numpy()
        assert float(expected @ actual) >= 0.999


def crop_test_images() -> list[bytes]:
    """Фото-кропи з PARITY_IMAGES_DIR, інакше 3 продуктові фото з Polyvore (якщо є), плюс синтетика (без одягу)."""
    images = photo_crops()
    if not os.environ.get("PARITY_IMAGES_DIR") and os.environ.get("POLYVORE_DIR"):
        parquet = Path(os.environ["POLYVORE_DIR"]) / "data/disjoint/validation.parquet"
        if parquet.exists():
            batch = next(pq.ParquetFile(parquet).iter_batches(batch_size=3, columns=["image"]))
            images = batch.column(0).field("bytes").to_pylist() + images
    return images


@pytest.fixture
def restore_cuda_visible_devices():
    # ultralytics.select_device("cpu") пише CUDA_VISIBLE_DEVICES=-1 в os.environ, і підпроцеси інших тестів
    # (test_pipeline) втрачають GPU — після тесту повертаємо змінну як була
    saved = os.environ.get("CUDA_VISIBLE_DEVICES")
    yield
    if saved is None:
        os.environ.pop("CUDA_VISIBLE_DEVICES", None)
    else:
        os.environ["CUDA_VISIBLE_DEVICES"] = saved


def test_yolo_crop_matches_parse_clothing_detector(restore_cuda_visible_devices):
    cv2 = pytest.importorskip("cv2")
    pytest.importorskip("ultralytics")
    emb_cfg = load_config()["embedding"]
    weights = REPO_ROOT / emb_cfg["crop_weights"]
    if not PARSE_DETECTOR.exists() or not weights.exists():
        pytest.skip(f"ParseImageService ClothingDetector or weights not found: {PARSE_DETECTOR}, {weights}")
    module = types.ModuleType("parse_clothing_detector")
    module.__dict__["__file__"] = str(PARSE_DETECTOR)  # ClothingDetector шукає ваги відносно свого файлу
    exec(compile(PARSE_DETECTOR.read_text(encoding="utf-8"), str(PARSE_DETECTOR), "exec"), module.__dict__)

    allowed = emb_cfg["crop_allowed_classes"]
    parse = module.ClothingDetector()
    ours = YoloCropper(weights, emb_cfg["crop_conf"], allowed, "cpu")
    for image_bytes in crop_test_images():
        rgb = decode(image_bytes)
        expected = cv2.cvtColor(parse.detect_and_crop(rgb, allowed_classes=allowed)["crop_bgr"], cv2.COLOR_BGR2RGB)
        assert np.array_equal(ours(rgb), expected)


def test_embed_split_normalized_and_matches_parquet(tmp_path, clip_dir, synthetic_root):
    model, processor = load_clip(clip_dir, torch.device("cpu"))
    parquet = synthetic_root / "data/disjoint/validation.parquet"
    encoder = Encoder(model, torch.device("cpu"), fp16=False)
    embed_split(parquet, tmp_path, "v", model, processor, encoder, batch_size=8, num_workers=2,
                shard_size=16, limit=None)
    ids, vectors = load_embeddings(tmp_path, "v")
    assert ids == pq.read_table(parquet, columns=["item_id"]).column(0).to_pylist()
    assert vectors.shape == (len(ids), 512)
    assert np.allclose(np.linalg.norm(vectors, axis=1), 1.0, atol=1e-5)
    assert not (tmp_path / "v.parts").exists()


def test_embed_split_resumes_after_interruption(tmp_path, clip_dir, synthetic_root):
    model, processor = load_clip(clip_dir, torch.device("cpu"))
    parquet = synthetic_root / "data/disjoint/validation.parquet"
    all_ids = pq.read_table(parquet, columns=["item_id"]).column(0).to_pylist()
    encoder = Encoder(model, torch.device("cpu"), fp16=False)

    # «перервана» частина: перші 5 айтемів уже пораховано (позначка-вектор, щоб відрізнити)
    writer = ShardWriter(tmp_path / "v.parts", shard_size=100)
    marker = np.full((5, 512), 0.5, dtype=np.float32)
    writer.add(all_ids[:5], marker)
    writer.flush()
    # недописана частина без ids.txt має ігноруватися
    np.save(tmp_path / "v.parts" / "part_00001.npy", np.zeros((3, 512), np.float32))

    embed_split(parquet, tmp_path, "v", model, processor, encoder, batch_size=8, num_workers=2,
                shard_size=16, limit=None)
    ids, vectors = load_embeddings(tmp_path, "v")
    assert sorted(ids) == sorted(all_ids) and len(ids) == len(all_ids)
    assert np.allclose(vectors[:5], 0.5)  # уже пораховане не перераховувалося
