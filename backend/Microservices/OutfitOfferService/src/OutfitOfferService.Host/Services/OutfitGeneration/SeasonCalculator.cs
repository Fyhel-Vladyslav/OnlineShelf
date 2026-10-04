namespace OutfitOfferService.src.OutfitOfferService.Host.Services.OutfitGeneration;

/// <summary>
/// Фази сезону у тих самих одиницях, що й довідник Season у ShelfsService:
/// 1..3 — рання/середня/пізня зима, 4..6 — весна, 7..9 — літо, 10..12 — осінь. 0 — сезон не вказано.
/// </summary>
public static class SeasonCalculator
{
    public const int PhaseCount = 12;

    /// <summary>Поточна фаза за датою; для південної півкулі (latitude &lt; 0) сезони зсунуті на пів року.</summary>
    public static int CurrentPhase(DateTime date, double? latitude)
    {
        // Грудень — рання зима (1), січень — 2, …, листопад — пізня осінь (12)
        var phase = date.Month % 12 + 1;

        if (latitude is < 0)
        {
            phase = (phase + 5) % PhaseCount + 1;
        }

        return phase;
    }

    /// <summary>Циклічна відстань між фазами (пізня осінь і рання зима — сусіди).</summary>
    public static int Distance(int phaseA, int phaseB)
    {
        var diff = Math.Abs(phaseA - phaseB) % PhaseCount;
        return Math.Min(diff, PhaseCount - diff);
    }
}
