"""Наскрізний smoke: синтетичний датасет → ембединги → навчання → оцінка → ONNX → паритет."""

import json
import os
import subprocess
import sys
from pathlib import Path

from tests.synthetic import make_polyvore, make_tiny_clip

SCORER_DIR = Path(__file__).resolve().parents[1]


def run(module: str, *args: str, env: dict) -> str:
    result = subprocess.run([sys.executable, "-m", module, *args], cwd=SCORER_DIR, env=env,
                            capture_output=True, text=True, timeout=600)
    assert result.returncode == 0, result.stdout + result.stderr
    return result.stdout


def test_end_to_end_smoke(tmp_path):
    root = make_polyvore(tmp_path / "polyvore_outfits", {"train": 60, "valid": 20, "test": 20})
    clip = make_tiny_clip(tmp_path / "tiny_clip")
    env = {**os.environ, "POLYVORE_DIR": str(root)}
    emb, runs, evals = tmp_path / "emb", tmp_path / "runs", tmp_path / "eval"

    run("src.data.embed", "--model", str(clip), "--out-dir", str(emb), "--precision", "fp32", env=env)
    run("src.train", "--emb-dir", str(emb), "--runs-dir", str(runs), "--epochs", "2", "--batch-size", "32", env=env)
    run("src.train", "--emb-dir", str(emb), "--runs-dir", str(runs), "--epochs", "1", "--batch-size", "32",
        "--gnn-rounds", "0", "--name", "pairwise", env=env)
    main_run = next(runs.glob("*_csa_ngnn"))
    pairwise_run = next(runs.glob("*_pairwise"))
    run("src.eval", "--checkpoint", str(main_run / "best.pt"), "--pairwise", str(pairwise_run / "best.pt"),
        "--emb-dir", str(emb), "--out-dir", str(evals), env=env)
    metrics = json.loads(next(evals.glob("*/metrics.json")).read_text(encoding="utf-8"))
    assert len(metrics["results"]) == 4

    onnx_path = tmp_path / "outfit_gnn.onnx"
    run("src.export_onnx", "--checkpoint", str(main_run / "best.pt"),
        "--metrics", str(next(evals.glob("*/metrics.json"))), "--out", str(onnx_path), env=env)
    assert "OK" in run("src.parity_check", "--checkpoint", str(main_run / "best.pt"), "--onnx", str(onnx_path),
                       "--graphs", "200", env=env)
