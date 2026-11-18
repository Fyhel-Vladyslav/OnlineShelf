using FastEndpoints;
using ShelfsService.src.ShelfsService.Common;

namespace ShelfsService.src.ShelfsService.Host.Features.Embeddings;

public sealed record BackfillEmbeddingsRequest
{
    /// <summary>Обмежити backfill речами одного користувача; null — усі речі.</summary>
    public Guid? UserId { get; init; }
}

/// <summary>Ручний запуск backfill візуальних ембедингів (напр. після перезбирання ParseImageService).</summary>
public sealed class BackfillEmbeddingsEndpoint(IEmbeddingBackfillService backfill)
    : Endpoint<BackfillEmbeddingsRequest, EmbeddingBackfillResult>
{
    public override void Configure()
    {
        Post(ApiRoutes.BackfillEmbeddings);
        AllowAnonymous(); // авторизацію додамо разом для всіх сервісів
    }

    public override async Task HandleAsync(BackfillEmbeddingsRequest req, CancellationToken ct)
    {
        var result = await backfill.RunAsync(req.UserId, ct);

        if (result.Aborted)
        {
            await Send.ResultAsync(TypedResults.Json(result, statusCode: StatusCodes.Status503ServiceUnavailable));
            return;
        }

        await Send.OkAsync(result, ct);
    }
}
