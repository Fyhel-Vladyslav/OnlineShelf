"""Скорер сумісності образу: CSA-Net (попарно) + NGNN-lite (агрегація), dense-форма без PyG.

Граф образу повнозв'язний, N = 2..8, тож усі пари рахуються матрицею N×N, а ONNX-експорт
зводиться до звичайних matmul/softmax/gather.

1. Вузол: h_i = MLP((x_i − μ)·σ) + emb(c_i), де μ, σ — середній ембединг і масштаб з train
   (буфери моделі: прибирають спільний компонент CLIP-векторів, навчання стартує в 2–3 рази швидше); якщо ембединга немає, замість MLP(x_i) — навчений вектор `missing`.
2. Пара (CSA-Net, Lin et al. 2020): K масок m_k, ω = softmax(MLP(c_i, c_j)), p_i = Σ ω_k (m_k ⊙ h_i),
   s_ij = σ(MLP([p_i; p_j; |p_i − p_j|; p_i ⊙ p_j])). Логіти усереднюються з транспонованими,
   тож s_ij = s_ji точно (вхід ω симетричний за побудовою).
3. Агрегація (NGNN-lite, Cui et al. 2019): `gnn_rounds` раундів message passing з вагами s_ij,
   attention-pooling, голова на [pool; mean s; min s] → graph_score.
   gnn_rounds = 0 — baseline «pairwise без GNN»: graph_score = середнє s_ij.
   residual = True — голова дає лише поправку: graph_logit = logit(mean s) + head(...), останній шар
   голови ініціалізовано нулями, тож на старті модель = pairwise і GNN вчиться лише покращувати її.
"""

from __future__ import annotations

import torch
from torch import nn


def mlp(sizes: list[int], dropout: float = 0.0) -> nn.Sequential:
    layers: list[nn.Module] = []
    for i in range(len(sizes) - 1):
        layers.append(nn.Linear(sizes[i], sizes[i + 1]))
        if i < len(sizes) - 2:
            layers.append(nn.GELU())
            if dropout:
                layers.append(nn.Dropout(dropout))
    return nn.Sequential(*layers)


class OutfitScorer(nn.Module):
    def __init__(self, emb_dim: int = 512, hidden_dim: int = 256, dim: int = 64, num_masks: int = 5,
                 category_vocab: int = 7, category_emb_dim: int = 32, gnn_rounds: int = 2,
                 dropout: float = 0.1, residual: bool = False) -> None:
        super().__init__()
        self.gnn_rounds = gnn_rounds
        self.residual = residual
        self.register_buffer("input_mean", torch.zeros(emb_dim))
        self.register_buffer("input_scale", torch.ones(()))
        self.node_mlp = mlp([emb_dim, hidden_dim, dim], dropout)
        self.missing = nn.Parameter(torch.zeros(dim))
        self.node_category = nn.Embedding(category_vocab, dim)
        self.pair_category = nn.Embedding(category_vocab, category_emb_dim)
        self.pair_attention = mlp([2 * category_emb_dim, dim, num_masks])
        self.masks = nn.Parameter(1.0 + 0.1 * torch.randn(num_masks, dim))
        self.edge_mlp = mlp([4 * dim, 2 * dim, 1], dropout)
        self.message = nn.ModuleList(nn.Linear(dim, dim) for _ in range(gnn_rounds))
        self.update = nn.ModuleList(mlp([2 * dim, dim, dim]) for _ in range(gnn_rounds))
        self.norm = nn.ModuleList(nn.LayerNorm(dim) for _ in range(gnn_rounds))
        self.pool_score = mlp([dim, dim, 1])
        self.head = mlp([dim + 2, dim, 1], dropout)
        if residual:
            nn.init.zeros_(self.head[-1].weight)
            nn.init.zeros_(self.head[-1].bias)

    @torch.no_grad()
    def set_input_stats(self, vectors) -> None:
        """μ і σ з ембедингів train (лише айтеми з ембедингом)."""
        v = torch.as_tensor(vectors, dtype=torch.float32)
        mean = v.mean(dim=0)
        self.input_mean.copy_(mean)
        self.input_scale.fill_(1.0 / (v - mean).norm(dim=-1).mean().clamp(min=1e-6).item())

    def forward(self, x: torch.Tensor, category: torch.Tensor, has_embedding: torch.Tensor,
                mask: torch.Tensor) -> tuple[torch.Tensor, torch.Tensor]:
        """x [B,N,D], category [B,N], has_embedding [B,N], mask [B,N] → (graph_logit [B], edge_logit [B,N,N])."""
        has = has_embedding.unsqueeze(-1)
        x = (x - self.input_mean) * self.input_scale
        h = has * self.node_mlp(x) + (1.0 - has) * self.missing + self.node_category(category)

        # --- попарна сумісність (CSA-Net)
        e = self.pair_category(category)                                   # [B,N,C]
        e_i, e_j = e.unsqueeze(2), e.unsqueeze(1)
        pair_in = torch.cat([e_i + e_j, e_i * e_j], dim=-1)                 # [B,N,N,2C], симетричний
        weights = torch.softmax(self.pair_attention(pair_in), dim=-1)      # [B,N,N,K]
        pair_mask_vec = torch.matmul(weights, self.masks)                  # [B,N,N,d] = Σ ω_k m_k
        p_i = h.unsqueeze(2) * pair_mask_vec
        p_j = h.unsqueeze(1) * pair_mask_vec
        edge_in = torch.cat([p_i, p_j, (p_i - p_j).abs(), p_i * p_j], dim=-1)
        raw = self.edge_mlp(edge_in).squeeze(-1)                           # [B,N,N]
        edge_logit = 0.5 * (raw + raw.transpose(1, 2))

        n = x.shape[1]
        idx = torch.arange(n, device=x.device)
        not_self = (idx.unsqueeze(0) != idx.unsqueeze(1)).to(x.dtype)       # [N,N]
        pair_mask = mask.unsqueeze(2) * mask.unsqueeze(1) * not_self        # [B,N,N]
        edge_prob = torch.sigmoid(edge_logit) * pair_mask
        pair_count = pair_mask.sum(dim=(1, 2))
        mean_s = edge_prob.sum(dim=(1, 2)) / pair_count.clamp(min=1.0)
        mean_s = torch.where(pair_count > 0, mean_s, torch.full_like(mean_s, 0.5))
        p = mean_s.clamp(1e-6, 1.0 - 1e-6)
        pairwise_logit = torch.log(p / (1.0 - p))  # не torch.logit: простіший ONNX-граф

        if self.gnn_rounds == 0:
            return pairwise_logit, edge_logit

        # --- message passing з вагами s_ij (NGNN-lite)
        adjacency = edge_prob / (edge_prob.sum(dim=-1, keepdim=True) + 1e-6)
        node_mask = mask.unsqueeze(-1)
        for message, update, norm in zip(self.message, self.update, self.norm):
            m = torch.matmul(adjacency, message(h))
            h = norm(h + update(torch.cat([h, m], dim=-1))) * node_mask

        # --- attention-pooling + «найслабша ланка»
        scores = self.pool_score(h).squeeze(-1).masked_fill(mask <= 0, -1e4)
        attention = torch.softmax(scores, dim=-1).unsqueeze(-1)
        pooled = (attention * h).sum(dim=1)
        min_s = (edge_prob + (1.0 - pair_mask) * 2.0).amin(dim=(1, 2)).clamp(max=1.0)
        graph_logit = self.head(torch.cat([pooled, mean_s.unsqueeze(-1), min_s.unsqueeze(-1)], dim=-1)).squeeze(-1)
        if self.residual:
            graph_logit = pairwise_logit + graph_logit
        return graph_logit, edge_logit


class OnnxOutfitScorer(nn.Module):
    """Обгортка під контракт OutfitNetworkService (один граф, без батча).

    входи:  node_features float32[N,512], node_category int64[N] (OutfitSlot 0..6),
            node_has_embedding float32[N], edge_index int64[2,E]
    виходи: graph_score float32[1], edge_scores float32[E] (обидва в [0,1])
    """

    def __init__(self, scorer: OutfitScorer) -> None:
        super().__init__()
        self.scorer = scorer

    def forward(self, node_features: torch.Tensor, node_category: torch.Tensor,
                node_has_embedding: torch.Tensor, edge_index: torch.Tensor) -> tuple[torch.Tensor, torch.Tensor]:
        mask = torch.ones_like(node_has_embedding).unsqueeze(0)
        graph_logit, edge_logit = self.scorer(
            node_features.unsqueeze(0), node_category.unsqueeze(0), node_has_embedding.unsqueeze(0), mask)
        graph_score = torch.sigmoid(graph_logit).reshape(1)
        n = node_features.shape[0]
        flat = torch.sigmoid(edge_logit[0]).reshape(-1)
        edge_scores = torch.gather(flat, 0, edge_index[0] * n + edge_index[1])
        return graph_score, edge_scores


def build_model(model_cfg: dict, emb_dim: int) -> OutfitScorer:
    return OutfitScorer(
        emb_dim=emb_dim,
        hidden_dim=model_cfg["hidden_dim"],
        dim=model_cfg["dim"],
        num_masks=model_cfg["num_masks"],
        category_vocab=model_cfg["category_vocab"],
        category_emb_dim=model_cfg["category_emb_dim"],
        gnn_rounds=model_cfg["gnn_rounds"],
        dropout=model_cfg["dropout"],
        residual=model_cfg.get("residual", False),  # старі чекпоінти — без residual
    )
