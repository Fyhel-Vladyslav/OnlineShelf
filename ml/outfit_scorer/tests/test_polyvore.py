from src.data.polyvore import SlotMapper, load_config, load_item_categories, load_split

# 11 semantic_category з polyvore_item_metadata.json завантаженої копії (dataset_dump 2026-10-10)
POLYVORE_CATEGORIES = {
    "shoes", "jewellery", "bags", "tops", "bottoms", "all-body",
    "outerwear", "sunglasses", "accessories", "hats", "scarves",
}


def test_slot_mapping_covers_all_polyvore_categories():
    mapper = SlotMapper(load_config())
    assert {c for c in POLYVORE_CATEGORIES if mapper(c) is None} == set()
    assert mapper("tops") == 1
    assert mapper("bottoms") == 2
    assert mapper("all-body") == 3
    assert mapper("shoes") == 4
    assert mapper("outerwear") == 5
    for accessory in ("bags", "jewellery", "hats", "sunglasses", "scarves", "accessories"):
        assert mapper(accessory) == 6
    assert mapper("beauty") is None


def test_outfits_are_ordered_by_index(tiny_polyvore):
    split = load_split(tiny_polyvore, "disjoint", "test")
    assert [o.item_ids for o in split.outfits] == [["1", "2", "3"], ["4", "5", "6"]]
    assert split.item_ids() == {"1", "2", "3", "4", "5", "6"}


def test_compatibility_resolves_tokens_and_skips_unknown(tiny_polyvore):
    split = load_split(tiny_polyvore, "disjoint", "test")
    assert [(q.label, q.item_ids) for q in split.compatibility] == [
        (1, ["1", "2", "3"]),
        (0, ["1", "5", "6"]),
    ]
    assert split.skipped["compatibility"] == 1


def test_fitb_correct_answer_is_found_by_set_id_not_position(tiny_polyvore):
    split = load_split(tiny_polyvore, "disjoint", "test")
    first, second = split.fitb
    assert first.correct == 2 and first.answer_ids[first.correct] == "3"
    assert second.correct == 0 and second.answer_ids[second.correct] == "6"
    # третє питання: правильної відповіді (100_2) серед варіантів немає
    assert split.skipped["fitb"] == 1


def test_item_categories(tiny_polyvore):
    assert load_item_categories(tiny_polyvore)["4"] == "all-body"
