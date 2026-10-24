"""Експорт скорера в ONNX під контракт OutfitNetworkService (opset 17, динамічні N і E).

    python -m src.export_onnx --checkpoint ml/data/runs/<run>/best.pt --metrics ml/data/eval/<run>/metrics.json
    python -m src.export_onnx --random --out tests/fixtures/tiny_scorer.onnx    # випадкові ваги для .NET-тестів

За замовчуванням пише backend/Microservices/OutfitNetworkService/Models/outfit_gnn.onnx.
Метадані (metadata_props): embedding_model, embedding_dim, category_vocab, model_version, trained_on, metrics_json.
"""

from __future__ import annotations

import argparse
import json
import sys
from datetime import datetime
from pathlib import Path

import onnx
import torch

from src.data.polyvore import REPO_ROOT, load_config
from src.model.scorer import OnnxOutfitScorer, OutfitScorer, build_model

DEFAULT_OUT = REPO_ROOT / "backend" / "Microservices" / "OutfitNetworkService" / "Models" / "outfit_gnn.onnx"
INPUT_NAMES = ["node_features", "node_category", "node_has_embedding", "edge_index"]
OUTPUT_NAMES = ["graph_score", "edge_scores"]
TINY_MODEL_CFG = {"hidden_dim": 32, "dim": 16, "num_masks": 5, "category_vocab": 7,
                  "category_emb_dim": 8, "gnn_rounds": 2, "dropout": 0.0}


def full_edge_index(n: int) -> torch.Tensor:
    """Повний орієнтований граф без петель — як OutfitGraph.CreateComplete у .NET."""
    pairs = [(i, j) for i in range(n) for j in range(n) if i != j]
    return torch.tensor(pairs, dtype=torch.int64).T.contiguous()


def example_inputs(n: int, emb_dim: int) -> tuple[torch.Tensor, ...]:
    x = torch.nn.functional.normalize(torch.randn(n, emb_dim), dim=-1)
    category = torch.arange(n, dtype=torch.int64) % 7
    has = torch.ones(n)
    return x, category, has, full_edge_index(n)


def export(model: OutfitScorer, emb_dim: int, out: Path, metadata: dict[str, str]) -> Path:
    wrapper = OnnxOutfitScorer(model.eval()).eval()
    out.parent.mkdir(parents=True, exist_ok=True)
    torch.onnx.export(
        wrapper, example_inputs(4, emb_dim), str(out),
        input_names=INPUT_NAMES, output_names=OUTPUT_NAMES, opset_version=17,
        dynamic_axes={"node_features": {0: "N"}, "node_category": {0: "N"}, "node_has_embedding": {0: "N"},
                      "edge_index": {1: "E"}, "edge_scores": {0: "E"}},
        dynamo=False,
    )
    proto = onnx.load(str(out))
    for key, value in metadata.items():
        entry = proto.metadata_props.add()
        entry.key, entry.value = key, value
    onnx.checker.check_model(proto)
    onnx.save(proto, str(out))
    return out


def build_metadata(config: dict, model_version: str, trained_on: str, metrics: dict) -> dict[str, str]:
    return {
        "embedding_model": config["embedding"]["model"],
        "embedding_dim": str(config["embedding"]["dim"]),
        "category_vocab": "OutfitSlot",
        "model_version": model_version,
        "trained_on": trained_on,
        "metrics_json": json.dumps(metrics, ensure_ascii=False),
    }


def main() -> int:
    config = load_config()
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--checkpoint", type=Path, help="best.pt from src.train")
    parser.add_argument("--random", action="store_true", help="tiny model with random weights (tests)")
    parser.add_argument("--metrics", type=Path, help="metrics.json from src.eval (embedded into metadata)")
    parser.add_argument("--model-version", default=f"{datetime.now():%Y.%m.%d}")
    parser.add_argument("--out", type=Path, default=DEFAULT_OUT)
    args = parser.parse_args()
    emb_dim = config["embedding"]["dim"]

    if args.random:
        torch.manual_seed(0)
        model = build_model(TINY_MODEL_CFG, emb_dim)
        metadata = build_metadata(config, "tiny-random", "none (random weights, contract tests only)", {})
    elif args.checkpoint:
        checkpoint = torch.load(args.checkpoint, map_location="cpu", weights_only=False)
        model = build_model(checkpoint["model_cfg"], checkpoint["emb_dim"])
        model.load_state_dict(checkpoint["model_state"])
        metrics: dict = {"valid": checkpoint.get("valid")}
        if args.metrics:
            evaluation = json.loads(args.metrics.read_text(encoding="utf-8"))
            metrics[evaluation["split"]] = evaluation["results"]
        trained_on = f"Polyvore Outfits ({checkpoint.get('variant', 'disjoint')}), epoch {checkpoint.get('epoch')}"
        metadata = build_metadata(config, args.model_version, trained_on, metrics)
    else:
        parser.error("pass --checkpoint or --random")

    out = export(model, emb_dim, args.out, metadata)
    print(f"saved {out} ({out.stat().st_size / 1024:.0f} KB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
