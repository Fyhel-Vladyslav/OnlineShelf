import numpy as np

from src.evaluation import fitb_accuracy, roc_auc


def test_roc_auc_perfect_inverse_and_ties():
    labels = np.array([0, 0, 1, 1])
    assert roc_auc(labels, np.array([0.1, 0.2, 0.8, 0.9])) == 1.0
    assert roc_auc(labels, np.array([0.9, 0.8, 0.2, 0.1])) == 0.0
    assert roc_auc(labels, np.array([0.5, 0.5, 0.5, 0.5])) == 0.5
    # 0.4 (поз.) > 0.3 (нег.), 0.4 < 0.6 (нег.), 0.7 (поз.) > обох → 3 з 4 пар
    assert roc_auc(labels, np.array([0.3, 0.6, 0.4, 0.7])) == 0.75


def test_fitb_accuracy():
    scores = np.array([0.1, 0.9, 0.2, 0.3,   0.8, 0.1, 0.1, 0.1])
    assert fitb_accuracy(scores, np.array([1, 2])) == 0.5
