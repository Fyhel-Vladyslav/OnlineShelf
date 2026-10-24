from pathlib import Path

import numpy as np
import onnx
import onnxruntime as ort
import torch

from src.data.polyvore import load_config
from src.export_onnx import TINY_MODEL_CFG, build_metadata, export, full_edge_index
from src.model.scorer import build_model
from src.parity_check import TOLERANCE, check_parity

FIXTURE = Path(__file__).parent / "fixtures" / "tiny_scorer.onnx"
EXPECTED_METADATA = {"embedding_model", "embedding_dim", "category_vocab", "model_version", "trained_on", "metrics_json"}


def test_torch_onnx_parity(tmp_path):
    torch.manual_seed(1)
    model = build_model(dict(load_config()["model"], dropout=0.0), 512)
    model.set_input_stats(torch.nn.functional.normalize(torch.randn(50, 512), dim=-1))
    path = export(model, 512, tmp_path / "model.onnx", build_metadata(load_config(), "test", "test", {}))
    result = check_parity(model, path, graphs=300)
    assert result["max_abs_diff_graph_score"] < TOLERANCE
    assert result["max_abs_diff_edge_scores"] < TOLERANCE


def test_tiny_scorer_fixture_contract():
    assert FIXTURE.exists(), "python -m src.export_onnx --random --out tests/fixtures/tiny_scorer.onnx"
    assert FIXTURE.stat().st_size < 1024 * 1024
    proto = onnx.load(str(FIXTURE))
    assert {p.key for p in proto.metadata_props} == EXPECTED_METADATA
    session = ort.InferenceSession(str(FIXTURE), providers=["CPUExecutionProvider"])
    assert [i.name for i in session.get_inputs()] == ["node_features", "node_category", "node_has_embedding", "edge_index"]
    assert [o.name for o in session.get_outputs()] == ["graph_score", "edge_scores"]
    for n in (2, 3, 5):
        x = np.zeros((n, 512), np.float32)  # усі вузли без ембединга — не має падати
        graph, edges = session.run(None, {
            "node_features": x, "node_category": np.arange(n, dtype=np.int64),
            "node_has_embedding": np.zeros(n, np.float32), "edge_index": full_edge_index(n).numpy()})
        assert graph.shape == (1,) and edges.shape == (n * (n - 1),)
        assert 0 <= graph[0] <= 1 and ((edges >= 0) & (edges <= 1)).all()


def test_tiny_scorer_matches_tiny_config():
    proto = onnx.load(str(FIXTURE))
    assert any(init.dims == [TINY_MODEL_CFG["hidden_dim"], 512] or list(init.dims) == [512, TINY_MODEL_CFG["hidden_dim"]]
               for init in proto.graph.initializer)
