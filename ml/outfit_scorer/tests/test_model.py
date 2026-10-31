import torch

from src.data.polyvore import load_config
from src.model.scorer import build_model


def make_model(gnn_rounds=2, residual=True):
    torch.manual_seed(0)
    cfg = dict(load_config()["model"], gnn_rounds=gnn_rounds, dropout=0.0, residual=residual)
    model = build_model(cfg, 512).eval()
    model.set_input_stats(torch.nn.functional.normalize(torch.randn(100, 512), dim=-1))
    return model


def random_batch(b=3, n=5):
    x = torch.nn.functional.normalize(torch.randn(b, n, 512), dim=-1)
    category = torch.randint(0, 7, (b, n))
    return x, category, torch.ones(b, n), torch.ones(b, n)


def test_output_shapes_and_range():
    graph_logit, edge_logit = make_model()(*random_batch())
    assert graph_logit.shape == (3,) and edge_logit.shape == (3, 5, 5)
    probs = torch.sigmoid(graph_logit)
    assert ((probs >= 0) & (probs <= 1)).all()


def test_pairwise_scores_are_symmetric():
    _, edge_logit = make_model()(*random_batch())
    assert torch.allclose(edge_logit, edge_logit.transpose(1, 2), atol=1e-6)


def test_padding_does_not_change_scores():
    model = make_model()
    x, category, has, mask = random_batch(b=1, n=3)
    graph, edges = model(x, category, has, mask)
    pad = lambda t, value: torch.cat([t, torch.full((1, 2, *t.shape[2:]), value, dtype=t.dtype)], dim=1)
    graph_p, edges_p = model(pad(x, 0.0), pad(category, 0), pad(has, 0.0), pad(mask, 0.0))
    assert torch.allclose(graph, graph_p, atol=1e-5)
    assert torch.allclose(edges, edges_p[:, :3, :3], atol=1e-5)


def test_node_order_does_not_change_graph_score():
    model = make_model()
    x, category, has, mask = random_batch(b=1, n=4)
    perm = torch.tensor([2, 0, 3, 1])
    graph, _ = model(x, category, has, mask)
    graph_p, _ = model(x[:, perm], category[:, perm], has[:, perm], mask[:, perm])
    assert torch.allclose(graph, graph_p, atol=1e-5)


def test_missing_embedding_ignores_vector():
    model = make_model()
    x, category, has, mask = random_batch(b=1, n=3)
    has[0, 1] = 0.0
    x2 = x.clone()
    x2[0, 1] = torch.randn(512)
    assert torch.allclose(model(x, category, has, mask)[0], model(x2, category, has, mask)[0], atol=1e-6)


def test_pairwise_baseline_graph_score_is_mean_edge_score():
    model = make_model(gnn_rounds=0)
    x, category, has, mask = random_batch(b=2, n=4)
    graph_logit, edge_logit = model(x, category, has, mask)
    off_diag = ~torch.eye(4, dtype=torch.bool)
    expected = torch.sigmoid(edge_logit)[:, off_diag].mean(dim=1)
    assert torch.allclose(torch.sigmoid(graph_logit), expected, atol=1e-5)


def test_residual_model_starts_as_pairwise():
    # нульова поправка голови → graph_score = середнє s_ij, як у pairwise
    model = make_model(residual=True)
    x, category, has, mask = random_batch(b=2, n=4)
    graph_logit, edge_logit = model(x, category, has, mask)
    off_diag = ~torch.eye(4, dtype=torch.bool)
    expected = torch.sigmoid(edge_logit)[:, off_diag].mean(dim=1)
    assert torch.allclose(torch.sigmoid(graph_logit), expected, atol=1e-5)


def test_residual_correction_is_trainable():
    model = make_model(residual=True).train()
    graph_logit, _ = model(*random_batch())
    graph_logit.sum().backward()
    assert model.head[-1].weight.grad.abs().sum() > 0


def test_old_checkpoint_config_without_residual_still_builds():
    cfg = {k: v for k, v in load_config()["model"].items() if k != "residual"}
    assert build_model(cfg, 512).residual is False
