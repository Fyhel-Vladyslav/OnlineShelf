from google.protobuf.internal import containers as _containers
from google.protobuf import descriptor as _descriptor
from google.protobuf import message as _message
from collections.abc import Iterable as _Iterable, Mapping as _Mapping
from typing import ClassVar as _ClassVar, Optional as _Optional, Union as _Union

DESCRIPTOR: _descriptor.FileDescriptor

class AnalyzeClothingRequest(_message.Message):
    __slots__ = ("image_data",)
    IMAGE_DATA_FIELD_NUMBER: _ClassVar[int]
    image_data: bytes
    def __init__(self, image_data: _Optional[bytes] = ...) -> None: ...

class AnalyzeClothingResponse(_message.Message):
    __slots__ = ("success", "attributes", "error_message")
    SUCCESS_FIELD_NUMBER: _ClassVar[int]
    ATTRIBUTES_FIELD_NUMBER: _ClassVar[int]
    ERROR_MESSAGE_FIELD_NUMBER: _ClassVar[int]
    success: bool
    attributes: ClothingAttributes
    error_message: str
    def __init__(self, success: bool = ..., attributes: _Optional[_Union[ClothingAttributes, _Mapping]] = ..., error_message: _Optional[str] = ...) -> None: ...

class ClothingAttributes(_message.Message):
    __slots__ = ("attribute_color_main", "attribute_color_second", "attribute_type", "attribute_season", "attribute_pattern", "attribute_material", "confidence")
    ATTRIBUTE_COLOR_MAIN_FIELD_NUMBER: _ClassVar[int]
    ATTRIBUTE_COLOR_SECOND_FIELD_NUMBER: _ClassVar[int]
    ATTRIBUTE_TYPE_FIELD_NUMBER: _ClassVar[int]
    ATTRIBUTE_SEASON_FIELD_NUMBER: _ClassVar[int]
    ATTRIBUTE_PATTERN_FIELD_NUMBER: _ClassVar[int]
    ATTRIBUTE_MATERIAL_FIELD_NUMBER: _ClassVar[int]
    CONFIDENCE_FIELD_NUMBER: _ClassVar[int]
    attribute_color_main: str
    attribute_color_second: str
    attribute_type: int
    attribute_season: int
    attribute_pattern: int
    attribute_material: int
    confidence: float
    def __init__(self, attribute_color_main: _Optional[str] = ..., attribute_color_second: _Optional[str] = ..., attribute_type: _Optional[int] = ..., attribute_season: _Optional[int] = ..., attribute_pattern: _Optional[int] = ..., attribute_material: _Optional[int] = ..., confidence: _Optional[float] = ...) -> None: ...

class EmbedClothingRequest(_message.Message):
    __slots__ = ("image_data",)
    IMAGE_DATA_FIELD_NUMBER: _ClassVar[int]
    image_data: bytes
    def __init__(self, image_data: _Optional[bytes] = ...) -> None: ...

class EmbedClothingResponse(_message.Message):
    __slots__ = ("success", "embedding", "embedding_model", "error_message")
    SUCCESS_FIELD_NUMBER: _ClassVar[int]
    EMBEDDING_FIELD_NUMBER: _ClassVar[int]
    EMBEDDING_MODEL_FIELD_NUMBER: _ClassVar[int]
    ERROR_MESSAGE_FIELD_NUMBER: _ClassVar[int]
    success: bool
    embedding: _containers.RepeatedScalarFieldContainer[float]
    embedding_model: str
    error_message: str
    def __init__(self, success: bool = ..., embedding: _Optional[_Iterable[float]] = ..., embedding_model: _Optional[str] = ..., error_message: _Optional[str] = ...) -> None: ...

class AnalyzeOutfitRequest(_message.Message):
    __slots__ = ("image_data",)
    IMAGE_DATA_FIELD_NUMBER: _ClassVar[int]
    image_data: bytes
    def __init__(self, image_data: _Optional[bytes] = ...) -> None: ...

class AnalyzeOutfitResponse(_message.Message):
    __slots__ = ("success", "items", "embedding_model", "error_message")
    SUCCESS_FIELD_NUMBER: _ClassVar[int]
    ITEMS_FIELD_NUMBER: _ClassVar[int]
    EMBEDDING_MODEL_FIELD_NUMBER: _ClassVar[int]
    ERROR_MESSAGE_FIELD_NUMBER: _ClassVar[int]
    success: bool
    items: _containers.RepeatedCompositeFieldContainer[OutfitItem]
    embedding_model: str
    error_message: str
    def __init__(self, success: bool = ..., items: _Optional[_Iterable[_Union[OutfitItem, _Mapping]]] = ..., embedding_model: _Optional[str] = ..., error_message: _Optional[str] = ...) -> None: ...

class OutfitItem(_message.Message):
    __slots__ = ("slot", "attributes", "box", "embedding")
    SLOT_FIELD_NUMBER: _ClassVar[int]
    ATTRIBUTES_FIELD_NUMBER: _ClassVar[int]
    BOX_FIELD_NUMBER: _ClassVar[int]
    EMBEDDING_FIELD_NUMBER: _ClassVar[int]
    slot: str
    attributes: ClothingAttributes
    box: BoundingBox
    embedding: _containers.RepeatedScalarFieldContainer[float]
    def __init__(self, slot: _Optional[str] = ..., attributes: _Optional[_Union[ClothingAttributes, _Mapping]] = ..., box: _Optional[_Union[BoundingBox, _Mapping]] = ..., embedding: _Optional[_Iterable[float]] = ...) -> None: ...

class BoundingBox(_message.Message):
    __slots__ = ("x1", "y1", "x2", "y2")
    X1_FIELD_NUMBER: _ClassVar[int]
    Y1_FIELD_NUMBER: _ClassVar[int]
    X2_FIELD_NUMBER: _ClassVar[int]
    Y2_FIELD_NUMBER: _ClassVar[int]
    x1: int
    y1: int
    x2: int
    y2: int
    def __init__(self, x1: _Optional[int] = ..., y1: _Optional[int] = ..., x2: _Optional[int] = ..., y2: _Optional[int] = ...) -> None: ...
