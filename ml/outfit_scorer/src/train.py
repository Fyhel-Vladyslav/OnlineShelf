"""Навчання скорера на кешованих CLIP-ембедингах Polyvore.

    python -m src.train                              # CSA + NGNN-lite (config.yaml)
    python -m src.train --gnn-rounds 0 --name pairwise   # baseline: pairwise CSA без GNN
    python -m src.train --limit-outfits 400 --epochs 2   # smoke

Позитиви/негативи — src/data/sampler.py. Loss = BCE(graph) + edge_loss_weight · BCE(edges).
Early stopping за valid Compat AUC. Лог і найкращий чекпоінт: ml/data/runs/<timestamp>_<name>/.
"""

from __future__ import annotations

import argparse
import json
import random
import sys
import time
from datetime import datetime
from pathlib import Path

import numpy as np
import torch
import torch.nn.functional as F

from src.data.embed import cache_name, load_embeddings
from src.data.polyvore import SlotMapper, data_dir, load_config, load_item_categories, load_split, polyvore_root
from src.data.sampler import GraphSampler, ItemTable, collate
from src.evaluation import build_eval_set, evaluate, model_scorer
from src.model.scorer import build_model


def set_seed(seed: int) -> None:
    random.seed(seed)
    np.random.seed(seed)
    torch.manual_seed(seed)


def load_split_table(root: Path, emb_dir: Path, variant: str, split: str, categories: dict[str, str],
                     mapper: SlotMapper, dim: int, limit_outfits: int | None, emb_suffix: str = ""):
    data = load_split(root, variant, split).limited(limit_outfits)
    emb_ids, vectors = load_embeddings(emb_dir, cache_name(variant, split) + emb_suffix)
    item_ids = sorted(data.referenced_item_ids())
    table = ItemTable(item_ids, categories, mapper, emb_ids, vectors, dim)
    missing = int((table.has_embedding == 0).sum())
    print(f"{split}: {len(data.outfits)} outfits, {len(table.ids)} items "
          f"({missing} without embedding, {table.dropped} dropped by category)")
    return data, table


def edge_loss(edge_logit: torch.Tensor, edge_label: torch.Tensor, mask: torch.Tensor) -> torch.Tensor:
    n = mask.shape[1]
    not_self = 1.0 - torch.eye(n, device=mask.device)
    pair_mask = mask.unsqueeze(2) * mask.unsqueeze(1) * not_self
    loss = F.binary_cross_entropy_with_logits(edge_logit, edge_label, reduction="none")
    return (loss * pair_mask).sum() / pair_mask.sum().clamp(min=1.0)


def main() -> int:
    config = load_config()
    train_cfg, model_cfg = config["train"], dict(config["model"])
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--variant", default=config["dataset"]["variant"])
    parser.add_argument("--emb-dir", type=Path, default=data_dir(config) / "embeddings")
    parser.add_argument("--emb-suffix", default="", help="suffix of embedding cache, e.g. _o200 for smoke caches")
    parser.add_argument("--runs-dir", type=Path, default=data_dir(config) / "runs")
    parser.add_argument("--name", default="csa_ngnn")
    parser.add_argument("--epochs", type=int, default=train_cfg["epochs"])
    parser.add_argument("--batch-size", type=int, default=train_cfg["batch_size"])
    parser.add_argument("--gnn-rounds", type=int, default=model_cfg["gnn_rounds"])
    parser.add_argument("--limit-outfits", type=int, default=None, help="smoke: first N outfits per split")
    parser.add_argument("--device", default="cuda" if torch.cuda.is_available() else "cpu")
    args = parser.parse_args()

    model_cfg["gnn_rounds"] = args.gnn_rounds
    train_cfg = {**train_cfg, "batch_size": args.batch_size}
    seed = train_cfg["seed"]
    set_seed(seed)
    device = torch.device(args.device)
    dim = config["embedding"]["dim"]

    root = polyvore_root(config)
    mapper = SlotMapper(config)
    categories = load_item_categories(root)
    train_data, train_table = load_split_table(root, args.emb_dir, args.variant, "train", categories, mapper,
                                               dim, args.limit_outfits, args.emb_suffix)
    valid_data, valid_table = load_split_table(root, args.emb_dir, args.variant, "valid", categories, mapper,
                                               dim, args.limit_outfits, args.emb_suffix)
    valid_set = build_eval_set(valid_data, valid_table)

    sampler = GraphSampler(train_data.outfits, train_table, train_cfg, seed)
    model = build_model(model_cfg, dim)
    model.set_input_stats(train_table.vectors[train_table.has_embedding > 0])
    model = model.to(device)
    optimizer = torch.optim.AdamW(model.parameters(), lr=train_cfg["lr"], weight_decay=train_cfg["weight_decay"])
    aug_rng = np.random.default_rng(seed)

    run_dir = args.runs_dir / f"{datetime.now():%Y%m%d_%H%M%S}_{args.name}"
    run_dir.mkdir(parents=True, exist_ok=True)
    (run_dir / "config.json").write_text(json.dumps(
        {"args": {k: str(v) for k, v in vars(args).items()}, "model": model_cfg, "train": train_cfg,
         "embedding": config["embedding"], "category_map": config["category_map"]},
        ensure_ascii=False, indent=2), encoding="utf-8")
    log = open(run_dir / "metrics.jsonl", "w", encoding="utf-8")
    print(f"run dir: {run_dir}, device {device}, params {sum(p.numel() for p in model.parameters()):,}")

    best_auc, best_epoch, bad_epochs = -1.0, 0, 0
    for epoch in range(1, args.epochs + 1):
        model.train()
        started = time.perf_counter()
        graphs = sampler.epoch()
        total_loss = total_graph = total_edge = 0.0
        steps = 0
        for start in range(0, len(graphs), train_cfg["batch_size"]):
            batch = collate(graphs[start:start + train_cfg["batch_size"]], train_table, aug_rng,
                            train_cfg["drop_embedding_prob"], train_cfg["noise_std"]).to(device)
            graph_logit, edge_logit = model(batch.x, batch.category, batch.has_embedding, batch.mask)
            graph_loss = F.binary_cross_entropy_with_logits(graph_logit, batch.label)
            e_loss = edge_loss(edge_logit, batch.edge_label, batch.mask)
            loss = graph_loss + train_cfg["edge_loss_weight"] * e_loss
            optimizer.zero_grad(set_to_none=True)
            loss.backward()
            torch.nn.utils.clip_grad_norm_(model.parameters(), 5.0)
            optimizer.step()
            total_loss += loss.item()
            total_graph += graph_loss.item()
            total_edge += e_loss.item()
            steps += 1

        metrics = evaluate(model_scorer(model, valid_table, device), valid_set)
        record = {"epoch": epoch, "loss": total_loss / max(steps, 1), "graph_loss": total_graph / max(steps, 1),
                  "edge_loss": total_edge / max(steps, 1), "train_graphs": len(graphs),
                  "valid_compat_auc": metrics["compat_auc"], "valid_fitb_acc": metrics["fitb_acc"],
                  "seconds": round(time.perf_counter() - started, 1)}
        log.write(json.dumps(record) + "\n")
        log.flush()
        print(f"epoch {epoch:3d}  loss {record['loss']:.4f}  valid AUC {metrics['compat_auc']:.4f}  "
              f"FITB {metrics['fitb_acc']:.4f}  ({record['seconds']} s)", flush=True)

        if metrics["compat_auc"] > best_auc:
            best_auc, best_epoch, bad_epochs = metrics["compat_auc"], epoch, 0
            torch.save({"model_state": model.state_dict(), "model_cfg": model_cfg, "emb_dim": dim,
                        "epoch": epoch, "valid": metrics, "variant": args.variant}, run_dir / "best.pt")
        else:
            bad_epochs += 1
            if bad_epochs >= train_cfg["patience"]:
                print(f"early stopping: no improvement for {bad_epochs} epochs")
                break

    log.close()
    summary = {"best_epoch": best_epoch, "best_valid_compat_auc": best_auc, "checkpoint": str(run_dir / "best.pt")}
    (run_dir / "summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(json.dumps(summary, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
