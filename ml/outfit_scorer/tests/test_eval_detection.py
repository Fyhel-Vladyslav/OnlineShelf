import numpy as np
from PIL import Image

from src.convert_outfit_detector import timm_to_resnet
from src.eval_detection import Box, best_per_slot, is_whole_outfit, load_fashionpedia, mask_boxes, matched


def write_photo(root, split, name, labels):
    (root / "images" / split).mkdir(parents=True, exist_ok=True)
    (root / "labels" / split).mkdir(parents=True, exist_ok=True)
    Image.new("RGB", (100, 200), "white").save(root / "images" / split / f"{name}.jpg")
    (root / "labels" / split / f"{name}.txt").write_text("\n".join(labels), encoding="utf-8")


def test_whole_outfit_needs_shoes_and_dress_or_top_with_bottom():
    box = lambda slot: Box(slot, 0, 0, 1, 1)
    assert is_whole_outfit([box("Top"), box("Bottom"), box("Shoes")])
    assert is_whole_outfit([box("FullBody"), box("Shoes")])
    assert not is_whole_outfit([box("Top"), box("Bottom")])
    assert not is_whole_outfit([box("Top"), box("Shoes")])


def test_load_fashionpedia_keeps_outfits_and_merges_shoes_into_pair(tmp_path):
    (tmp_path / "data.yaml").write_text("names:\n  0: dress\n  1: shoe\n  2: glasses\n", encoding="utf-8")
    write_photo(tmp_path, "test", "outfit", ["0 0.5 0.4 0.4 0.6", "1 0.3 0.9 0.2 0.1", "1 0.7 0.9 0.2 0.1", "2 0.5 0.1 0.2 0.1"])
    write_photo(tmp_path, "test", "only_dress", ["0 0.5 0.4 0.4 0.6"])

    photos = load_fashionpedia(tmp_path, "test", 0, seed=1)

    assert [path.stem for path, _ in photos] == ["outfit"]
    boxes = {b.slot: b for b in photos[0][1]}
    assert set(boxes) == {"FullBody", "Shoes"}
    assert (boxes["FullBody"].x1, boxes["FullBody"].y2) == (30, 140)
    assert (boxes["Shoes"].x1, boxes["Shoes"].x2) == (20, 80)


def test_matched_joins_two_shoes_of_one_pair():
    pair = Box("Shoes", 20, 170, 80, 190)
    assert matched(pair, [Box("Shoes", 20, 170, 40, 190), Box("Shoes", 60, 170, 80, 190)])
    assert not matched(pair, [Box("Top", 20, 170, 80, 190)])


def test_mask_boxes_joins_shoes_and_skips_stray_pixels():
    mask = np.zeros((200, 100), dtype=np.uint8)
    mask[20:80, 30:70] = 4
    mask[150:160, 40:44] = 4  # пляма тієї самої мітки далеко внизу
    mask[180:190, 10:30] = 9
    mask[180:190, 60:80] = 10
    mask[0:1, 0:1] = 6  # надто мало, щоб бути штанами

    boxes = {b.slot: b for b in mask_boxes(mask)}

    assert set(boxes) == {"Top", "Shoes"}
    assert (boxes["Top"].x1, boxes["Top"].y1, boxes["Top"].x2, boxes["Top"].y2) == (30, 20, 70, 80)
    assert (boxes["Shoes"].x1, boxes["Shoes"].x2) == (10, 80)


def test_best_per_slot_keeps_one_box_and_two_shoes():
    boxes = [Box("Top", 0, 0, 1, 1, 0.3), Box("Top", 0, 0, 1, 1, 0.9),
             Box("Shoes", 0, 0, 1, 1, 0.8), Box("Shoes", 0, 0, 1, 1, 0.7), Box("Shoes", 0, 0, 1, 1, 0.1)]

    kept = best_per_slot(boxes)

    assert [(b.slot, b.confidence) for b in kept] == [("Top", 0.9), ("Shoes", 0.8), ("Shoes", 0.7)]


def test_timm_to_resnet_maps_backbone_names_only():
    prefix = "model.backbone.conv_encoder.model."
    assert timm_to_resnet(prefix + "conv1.weight") == prefix + "embedder.embedder.convolution.weight"
    assert timm_to_resnet(prefix + "layer2.3.bn2.bias") == prefix + "encoder.stages.1.layers.3.layer.1.normalization.bias"
    assert timm_to_resnet(prefix + "layer4.0.downsample.1.weight") == prefix + "encoder.stages.3.layers.0.shortcut.normalization.weight"
    assert timm_to_resnet("bbox_predictor.layers.0.bias") == "bbox_predictor.layers.0.bias"
