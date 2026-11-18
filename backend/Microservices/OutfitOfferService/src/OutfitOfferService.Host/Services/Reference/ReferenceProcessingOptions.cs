namespace OutfitOfferService.src.OutfitOfferService.Host.Services.Reference;

/// <summary>Налаштування черги еталонних фото (секція ReferenceProcessing).</summary>
public sealed class ReferenceProcessingOptions
{
    public const string SectionName = "ReferenceProcessing";

    /// <summary>
    /// Скільки фото обробляється одночасно. Parse спільний із гардеробом, а DETR на CPU забирає ядра,
    /// тому за замовчуванням одне.
    /// </summary>
    public int Parallelism { get; set; } = 1;

    /// <summary>Пауза воркера, коли черга порожня.</summary>
    public double PollIntervalSeconds { get; set; } = 2;

    /// <summary>Пауза воркера після збою БД: фото не втрачається, воно лишається Processing до кінця замка.</summary>
    public double ErrorBackoffSeconds { get; set; } = 10;

    /// <summary>
    /// Скільки фото тримається у Processing, перш ніж його вважають покинутим (контейнер упав посеред обробки).
    /// Має перевищувати дедлайн AnalyzeOutfit.
    /// </summary>
    public double LockMinutes { get; set; } = 5;

    /// <summary>Дедлайн AnalyzeOutfit: перший виклик після рестарту Parse тривав 26 с.</summary>
    public double AnalyzeDeadlineSeconds { get; set; } = 120;

    /// <summary>Пауза перед повтором після тимчасової помилки: 30 с, 2 хв, 10 хв, потім Failed.</summary>
    public double[] RetryDelaysSeconds { get; set; } = [30, 120, 600];

    /// <summary>Скільки разів фото беруть у роботу: перша спроба плюс повтори.</summary>
    public int MaxAttempts => RetryDelaysSeconds.Length + 1;
}
