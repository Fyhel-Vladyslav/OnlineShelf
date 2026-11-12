import sys
from pathlib import Path

import numpy as np
import torch

PARSE_ROOT = Path(__file__).resolve().parents[3] / "backend/Microservices/ParseImageService"
sys.path.insert(0, str(PARSE_ROOT))

from app.models.outfit_detector.outfit_detector import CLASS_SLOTS, OutfitDetector, select_items  # noqa: E402
from app.models.type_classifier.type_classifier import TYPE_PROMPTS, TypeClassifier  # noqa: E402


def detection(name, confidence, box=(0, 0, 10, 10)):
    return {"raw_class_name": name, "confidence": confidence, "box": box}


def test_select_items_keeps_best_box_per_class_and_skips_bag():
    items = select_items([detection("top", 0.4), detection("top", 0.9), detection("bag", 0.99)])

    assert [(i["raw_class_name"], i["confidence"]) for i in items] == [("top", 0.9)]


def test_select_items_joins_two_shoes_into_one_item():
    items = select_items([detection("shoes", 0.9, (10, 80, 20, 90)), detection("shoes", 0.7, (30, 85, 40, 95)),
                          detection("shoes", 0.3, (0, 0, 5, 5))])

    assert len(items) == 1
    assert items[0]["box"] == (10, 80, 40, 95)
    assert abs(items[0]["confidence"] - 0.8) < 1e-9


def test_select_items_keeps_single_shoe_box():
    items = select_items([detection("shoes", 0.6, (1, 2, 3, 4))])

    assert [i["box"] for i in items] == [(1, 2, 3, 4)]


def test_detector_without_weights_is_not_ready(tmp_path):
    assert not OutfitDetector(tmp_path).is_ready


def test_every_class_with_slot_has_types():
    assert set(TYPE_PROMPTS) == set(CLASS_SLOTS)


PROMPTS = {"bottom": {"pants": ["a", "b"], "skirt": ["c"]}, "shoes": {"shoe": ["d"]}, "hat": {"hat": ["e"]}}


class FakeClip:
    """Текстові ознаки — базисні вектори: подібність показує, який тип ближчий до ембединга."""

    device = "cpu"

    def get_text_features(self, input_ids):
        return torch.eye(4)[:len(input_ids)]


class FakeProcessor:
    def __call__(self, text, return_tensors, padding):
        return FakeInputs(torch.arange(len(text)))


class FakeInputs(dict):
    def __init__(self, input_ids):
        super().__init__(input_ids=input_ids)

    def to(self, device):
        return self


def test_type_classifier_uses_every_prompt_of_a_type(monkeypatch):
    monkeypatch.setattr("app.models.type_classifier.type_classifier.TYPE_PROMPTS", PROMPTS)
    classifier = TypeClassifier(FakeClip(), FakeProcessor())

    assert classifier.predict_type([0.0, 0.0, 1.0, 0.0], "bottom") == "skirt"
    assert classifier.predict_type([1.0, 0.0, 0.0, 0.0], "bottom") == "pants"
    assert classifier.predict_type([0.0, 1.0, 0.0, 0.0], "bottom") == "pants"


def test_type_classifier_needs_no_model_for_single_type_group(monkeypatch):
    monkeypatch.setattr("app.models.type_classifier.type_classifier.TYPE_PROMPTS", PROMPTS)
    classifier = TypeClassifier(FakeClip(), FakeProcessor())

    assert classifier.predict_type(list(np.zeros(4)), "shoes") == "shoe"
    assert classifier.predict_type(list(np.zeros(4)), "hat") == "hat"
