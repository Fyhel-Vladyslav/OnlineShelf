import numpy as np
import pytest

from src.data.embed import embedding_suffix, model_suffix
from src.eval_material import report_row, weak_label


@pytest.mark.parametrize("title, expected", [
    ("Skinny Crop Tattoo Jeans", "denim"),
    ("Silk Satin Blouse", "silk"),
    ("Merino wool sweater", "wool"),
    ("Leather and denim jacket", None),  # кілька матеріалів — назва неоднозначна
    ("Striped long sleeveless dress", None),
    ("Silky top", None),  # лише цілі слова
])
def test_weak_label_from_title(title, expected):
    assert weak_label(title) == expected


def test_report_row_precision_ignores_empty_prediction_class():
    labels = ["a", "b", "c"]
    row = report_row("m", np.array([0, 0, 1, 2]), np.array([0, 1, 1, 1]), labels)
    assert row["accuracy"] == 0.5
    assert row["recall"] == {"a": 0.5, "b": 1.0, "c": 0.0}
    assert row["precision"]["c"] == 0.0  # клас "c" не передбачався ні разу


def test_cache_suffix_names_model_and_crop():
    assert model_suffix("openai/clip-vit-base-patch32") == ""
    assert embedding_suffix("patrickjohncyh/fashion-clip", "yolo") == "_fashion-clip_yolo"
    assert embedding_suffix("openai/clip-vit-base-patch32", "none") == ""
