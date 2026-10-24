"""Оцінка на test: таблиця 5.5 (Compat AUC, FITB), підобрази 3–4 айтеми, розподіл graph_score.

    python -m src.eval --checkpoint ml/data/runs/<run>/best.pt --pairwise ml/data/runs/<run_pairwise>/best.pt

Результат: ml/data/eval/<timestamp>/report.md + metrics.json.
"""

from __future__ import annotations

import argparse
import json
import sys
from datetime import datetime
from pathlib import Path

import numpy as np
import torch

from src.baselines import mean_cosine_scorer, random_scorer
from src.data.polyvore import SlotMapper, data_dir, load_config, load_item_categories, polyvore_root
from src.data.sampler import eval_template_graphs
from src.evaluation import build_eval_set, evaluate, histogram, model_scorer, roc_auc
from src.model.scorer import OutfitScorer, build_model
from src.train import load_split_table


def load_checkpoint(path: Path, device: torch.device) -> tuple[OutfitScorer, dict]:
    checkpoint = torch.load(path, map_location=device, weights_only=False)
    model = build_model(checkpoint["model_cfg"], checkpoint["emb_dim"])
    model.load_state_dict(checkpoint["model_state"])
    return model.to(device).eval(), checkpoint


def main() -> int:
    config = load_config()
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--checkpoint", type=Path, required=True, help="CSA + NGNN-lite (best.pt)")
    parser.add_argument("--pairwise", type=Path, default=None, help="pairwise CSA without GNN (best.pt)")
    parser.add_argument("--variant", default=config["dataset"]["variant"])
    parser.add_argument("--split", default="test")
    parser.add_argument("--emb-dir", type=Path, default=data_dir(config) / "embeddings")
    parser.add_argument("--emb-suffix", default="", help="e.g. _o200 for smoke caches")
    parser.add_argument("--limit-outfits", type=int, default=None)
    parser.add_argument("--out-dir", type=Path, default=data_dir(config) / "eval")
    parser.add_argument("--device", default="cuda" if torch.cuda.is_available() else "cpu")
    args = parser.parse_args()

    device = torch.device(args.device)
    root = polyvore_root(config)
    categories = load_item_categories(root)
    data, table = load_split_table(root, args.emb_dir, args.variant, args.split, categories, SlotMapper(config),
                                   config["embedding"]["dim"], args.limit_outfits, args.emb_suffix)
    eval_set = build_eval_set(data, table)
    templates = eval_template_graphs(data.outfits, table, seed=config["train"]["seed"])
    template_labels = np.array([g.label for g in templates])

    scorers = {"Random": random_scorer(), "Mean-cosine CLIP (без навчання)": mean_cosine_scorer(table)}
    if args.pairwise:
        scorers["Pairwise CSA без GNN"] = model_scorer(load_checkpoint(args.pairwise, device)[0], table, device)
    main_model, checkpoint = load_checkpoint(args.checkpoint, device)
    main_name = "CSA + NGNN-lite (наша)"
    scorers[main_name] = model_scorer(main_model, table, device)

    results: dict[str, dict] = {}
    for name, scorer in scorers.items():
        metrics = evaluate(scorer, eval_set)
        template_scores = scorer(templates)
        metrics["template_auc"] = roc_auc(template_labels, template_scores) if templates else float("nan")
        results[name] = metrics
        print(f"{name:<35} AUC {metrics['compat_auc']:.4f}  FITB {metrics['fitb_acc']:.4f}  "
              f"templates AUC {metrics['template_auc']:.4f}")

    # Розподіл graph_score нашої моделі на підобразах: скори не мають злипатися біля 0.5
    main_template_scores = scorers[main_name](templates) if templates else np.zeros(0)
    distribution = {
        "std": float(main_template_scores.std()) if len(main_template_scores) else float("nan"),
        "mean_positive": float(main_template_scores[template_labels == 1].mean()) if templates else float("nan"),
        "mean_negative": float(main_template_scores[template_labels == 0].mean()) if templates else float("nan"),
        "histogram": histogram(main_template_scores),
    }

    out_dir = args.out_dir / f"{datetime.now():%Y%m%d_%H%M%S}_{args.split}"
    out_dir.mkdir(parents=True, exist_ok=True)
    summary = {
        "variant": args.variant, "split": args.split, "checkpoint": str(args.checkpoint),
        "pairwise_checkpoint": str(args.pairwise) if args.pairwise else None,
        "compat_questions": len(eval_set.compat_graphs), "fitb_questions": len(eval_set.fitb_correct),
        "template_graphs": len(templates), "skipped": eval_set.skipped,
        "results": results, "score_distribution": distribution,
    }
    (out_dir / "metrics.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")

    lines = [
        f"# Оцінка скорера: {args.variant}/{args.split}", "",
        f"Compat-питань {len(eval_set.compat_graphs)}, FITB-питань {len(eval_set.fitb_correct)}, "
        f"підобразів 3–4 айтеми {len(templates)} (половина — негативи), пропущено {eval_set.skipped}.", "",
        "| Модель | Compat. AUC | FITB acc. | AUC на підобразах 3–4 |", "|---|---|---|---|",
    ]
    for name, m in results.items():
        bold = name == main_name
        cells = [f"{m['compat_auc']:.3f}", f"{m['fitb_acc']:.1%}", f"{m['template_auc']:.3f}"]
        if bold:
            name, cells = f"**{name}**", [f"**{c}**" for c in cells]
        lines.append(f"| {name} | " + " | ".join(cells) + " |")
    lines += ["", "## Розподіл graph_score (наша модель, підобрази)", "",
              f"std {distribution['std']:.3f}, середнє на позитивах {distribution['mean_positive']:.3f}, "
              f"на негативах {distribution['mean_negative']:.3f}", "", "| Інтервал | Кількість |", "|---|---|"]
    lines += [f"| {b['from']:.1f}–{b['to']:.1f} | {b['count']} |" for b in distribution["histogram"]]
    (out_dir / "report.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"report: {out_dir / 'report.md'}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
