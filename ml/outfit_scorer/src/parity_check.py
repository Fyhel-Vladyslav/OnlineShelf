"""Паритет torch ↔ onnxruntime: 1 000 випадкових графів (N = 2..6), |різниця| < 1e-4.

    python -m src.parity_check --checkpoint ml/data/runs/<run>/best.pt --onnx <шлях до outfit_gnn.onnx>
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np
import onnxruntime as ort
import torch

from src.export_onnx import DEFAULT_OUT, full_edge_index
from src.model.scorer import OnnxOutfitScorer, OutfitScorer, build_model

TOLERANCE = 1e-4


def random_graph(rng: np.random.Generator, emb_dim: int) -> dict[str, np.ndarray]:
    n = int(rng.integers(2, 7))
    x = rng.normal(size=(n, emb_dim)).astype(np.float32)
    x /= np.linalg.norm(x, axis=-1, keepdims=True)
    has = (rng.random(n) < 0.8).astype(np.float32)
    x *= has[:, None]
    edges = full_edge_index(n).numpy()
    edges = edges[:, rng.permutation(edges.shape[1])]  # порядок ребер довільний — gather має це витримати
    return {"node_features": x, "node_category": rng.integers(0, 7, n).astype(np.int64),
            "node_has_embedding": has, "edge_index": edges}


def check_parity(model: OutfitScorer, onnx_path: Path, graphs: int = 1000, seed: int = 0) -> dict[str, float]:
    wrapper = OnnxOutfitScorer(model.eval()).eval()
    session = ort.InferenceSession(str(onnx_path), providers=["CPUExecutionProvider"])
    emb_dim = session.get_inputs()[0].shape[1]
    rng = np.random.default_rng(seed)
    max_graph = max_edge = 0.0
    for _ in range(graphs):
        inputs = random_graph(rng, emb_dim)
        onnx_graph, onnx_edges = session.run(None, inputs)
        with torch.no_grad():
            torch_graph, torch_edges = wrapper(*(torch.from_numpy(inputs[k]) for k in
                                                 ("node_features", "node_category", "node_has_embedding", "edge_index")))
        max_graph = max(max_graph, float(np.abs(onnx_graph - torch_graph.numpy()).max()))
        max_edge = max(max_edge, float(np.abs(onnx_edges - torch_edges.numpy()).max()))
    return {"graphs": graphs, "max_abs_diff_graph_score": max_graph, "max_abs_diff_edge_scores": max_edge}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--checkpoint", type=Path, required=True)
    parser.add_argument("--onnx", type=Path, default=DEFAULT_OUT)
    parser.add_argument("--graphs", type=int, default=1000)
    args = parser.parse_args()

    checkpoint = torch.load(args.checkpoint, map_location="cpu", weights_only=False)
    model = build_model(checkpoint["model_cfg"], checkpoint["emb_dim"])
    model.load_state_dict(checkpoint["model_state"])
    result = check_parity(model, args.onnx, args.graphs)
    ok = max(result["max_abs_diff_graph_score"], result["max_abs_diff_edge_scores"]) < TOLERANCE
    print(f"{result}\n{'OK' if ok else 'FAIL'}: tolerance {TOLERANCE}")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
