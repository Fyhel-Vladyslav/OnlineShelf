"""Готує ваги детектора образів для ParseImageService: звичайний формат transformers, без timm і мережі.

    python -m src.convert_outfit_detector

Вхід: файли yainage90/fashion-object-detection (config.json, preprocessor_config.json, model.safetensors).
Їхній config.json просить timm і завантаження resnet50, тому backbone задається тут, а імена його ваг
перекладаються на ResNet із transformers. Після збереження прогін на контрольному зображенні має дати ті самі
виходи. Результат не комітиться (174 МБ): він потрібен лише для збірки образу Parse.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

import numpy as np
import torch
from PIL import Image
from safetensors.torch import load_file
from transformers import (AutoImageProcessor, AutoModelForObjectDetection, ConditionalDetrConfig,
                          ConditionalDetrForObjectDetection, ResNetConfig)

from src.data.polyvore import data_dir, load_config
from src.eval_detection import PARSE_DETECTOR_DIR


def timm_to_resnet(key: str) -> str:
    """Назва ваги timm ResNet → ResNet із transformers: шари ті самі, різняться лише імена."""
    prefix = "model.backbone.conv_encoder.model."
    if not key.startswith(prefix):
        return key
    name = key[len(prefix):]
    if name.startswith(("conv1.", "bn1.")):
        return prefix + name.replace("conv1", "embedder.embedder.convolution").replace("bn1", "embedder.embedder.normalization")
    name = re.sub(r"^layer(\d)\.(\d+)\.", lambda m: f"encoder.stages.{int(m[1]) - 1}.layers.{m[2]}.", name)
    name = re.sub(r"\.conv(\d)\.", lambda m: f".layer.{int(m[1]) - 1}.convolution.", name)
    name = re.sub(r"\.bn(\d)\.", lambda m: f".layer.{int(m[1]) - 1}.normalization.", name)
    return prefix + name.replace(".downsample.0.", ".shortcut.convolution.").replace(".downsample.1.", ".shortcut.normalization.")


def load_source(path: Path) -> ConditionalDetrForObjectDetection:
    settings = json.loads((path / "config.json").read_text(encoding="utf-8"))
    for key in ("_name_or_path", "backbone", "backbone_config", "backbone_kwargs", "use_pretrained_backbone", "use_timm_backbone"):
        settings.pop(key, None)
    backbone = ResNetConfig(depths=[3, 4, 6, 3], hidden_sizes=[256, 512, 1024, 2048], embedding_size=64,
                            layer_type="bottleneck", out_features=["stage1", "stage2", "stage3", "stage4"])
    config = ConditionalDetrConfig(**settings, backbone_config=backbone, use_timm_backbone=False)
    weights = {timm_to_resnet(k): v for k, v in load_file(path / "model.safetensors").items()}
    # from_pretrained, а не load_state_dict: він перекладає старі імена шарів DETR на імена transformers 5
    model, info = ConditionalDetrForObjectDetection.from_pretrained(
        None, config=config, state_dict=weights, output_loading_info=True)
    if info["missing_keys"] or info["unexpected_keys"] or info["mismatched_keys"]:
        raise RuntimeError(f"Ваги не збіглися з моделлю: {info}")
    return model.eval()


@torch.no_grad()
def control_outputs(model, processor) -> tuple[torch.Tensor, torch.Tensor]:
    pixels = np.random.default_rng(0).integers(0, 255, (640, 480, 3), dtype=np.uint8)
    outputs = model(**processor(images=[Image.fromarray(pixels)], return_tensors="pt"))
    return outputs.logits, outputs.pred_boxes


def main() -> int:
    config = load_config()
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--source", type=Path, default=data_dir(config) / "detectors" / "yainage90-fashion-object-detection")
    parser.add_argument("--out", type=Path, default=PARSE_DETECTOR_DIR)
    args = parser.parse_args()

    processor = AutoImageProcessor.from_pretrained(args.source, local_files_only=True)
    model = load_source(args.source)
    expected = control_outputs(model, processor)

    model.save_pretrained(args.out)
    processor.save_pretrained(args.out)

    reloaded = AutoModelForObjectDetection.from_pretrained(args.out, local_files_only=True).eval()
    actual = control_outputs(reloaded, AutoImageProcessor.from_pretrained(args.out, local_files_only=True))
    difference = max(float((a - e).abs().max()) for a, e in zip(actual, expected))
    if difference > 1e-4:
        raise RuntimeError(f"Після збереження виходи розійшлися: {difference}")
    print(f"saved: {args.out}, max output difference: {difference:.2e}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
