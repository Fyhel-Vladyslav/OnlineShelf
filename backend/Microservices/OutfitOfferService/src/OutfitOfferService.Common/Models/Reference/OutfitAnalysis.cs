namespace OutfitOfferService.src.OutfitOfferService.Common.Models.Reference;

/// <summary>Рамка речі в пікселях фото: (X1, Y1) лівий верхній кут, (X2, Y2) правий нижній.</summary>
public sealed record AnalyzedBox(int X1, int Y1, int X2, int Y2);

/// <summary>Річ, яку детектор знайшов на фото людини. Атрибути — ключі довідника ShelfsService, 0 = не визначено.</summary>
public sealed record AnalyzedItem(
    OutfitSlot Slot,
    int AttributeType,
    int AttributeSeason,
    int AttributePattern,
    int AttributeMaterial,
    string ColorMain,
    string ColorSecond,
    float Confidence,
    AnalyzedBox Box,
    float[] Embedding);

/// <summary>Результат AnalyzeOutfit без прив'язки до gRPC-контракту.</summary>
public sealed record OutfitAnalysis(
    bool Success,
    string? ErrorMessage,
    int ImageWidth,
    int ImageHeight,
    string EmbeddingModel,
    IReadOnlyList<AnalyzedItem> Items);

/// <summary>Фото, яке воркер забрав із черги. <see cref="Attempts"/> уже враховує цей забір.</summary>
public sealed record ClaimedReferencePhoto(Guid Id, string BigImage, int Attempts);
