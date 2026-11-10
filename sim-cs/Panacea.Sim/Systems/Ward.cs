namespace Panacea.Sim;

/// <summary>What the ward remembers, as the paper could print it (Observation.WardRemembers).</summary>
public sealed record WardRemembers(
    int Months, // the months counted: this one and the ones before it
    int Deaths, // deaths in those months the ward lays at the bottle's door (an overdose or a wasting)
    int InfantDeaths, // of those, infants
    int StrangerDeaths, // of those, strangers found with the bottle
    int Verdicts, // inquests whose jury named the remedy (a bribe found out included)
    int Seizures); // times the Board of Health carried the bottles away

/// <summary>
/// The ward's memory of harm (systems/ward.ts). The heat is the authorities' suspicion; the memory
/// is the neighbours'. It rises with the harm the remedy did and was seen to do, fades by a share
/// each month, and costs the shop at the counter: strangers buy less often and seldom come back as
/// regulars, and regulars who are not hooked go elsewhere.
/// </summary>
public static class WardSystem
{
    /// <summary>The cause of a death as the ward gives it (<see cref="ReportedCauses"/>).</summary>
    public static string ReportedCauseOf(string cause, string segment, string? ailment, ContentPack content)
    {
        if (cause == DeathCauses.Overdose) return ReportedCauses.Overdose;
        if (cause == DeathCauses.Poisoning) return ReportedCauses.Wasting;
        Ailment? a = null;
        if (ailment is not null) content.ById.Ailments.TryGetValue(ailment, out a);
        if (cause == DeathCauses.Disease && a is not null) return ReportedCauses.Ailment;
        if (a is not null && a.MortalityMultiplier > 1) return ReportedCauses.Ailment;
        if (segment == "elderly") return ReportedCauses.OldAge;
        return ReportedCauses.Natural;
    }

    /// <summary>Whether the ward lays a death at the bottle's door: reported as an overdose or a wasting.</summary>
    public static bool BlamedOnTheBottle(DeathRecord d) =>
        d.ReportedCause == ReportedCauses.Overdose || d.ReportedCause == ReportedCauses.Wasting;

    /// <summary>How much one death adds to the ward's memory.</summary>
    public static double DeathWeight(DeathRecord d, ContentPack content)
    {
        var cfg = content.Config.Ward;
        // the family keeps the label that said it cured what killed them
        double promise = d.PromisedCure ? cfg.PerBrokenPromise : 0;
        if (!d.ProductImplicated) return promise;
        double w = promise;
        if (!BlamedOnTheBottle(d)) w += cfg.PerHastenedDeath;
        else if (d.Anonymous) w += cfg.PerStrangerDeath;
        else if (d.Consumer == Consumers.Infant) w += cfg.PerInfantDeath;
        else w += cfg.PerDeath;
        if (d.BadBatch) w += cfg.PerBadBatchDeath;
        return w;
    }

    private static double NewsWeight(GameState state, NewsItem n, ContentPack content)
    {
        var cfg = content.Config.Ward;
        switch (n)
        {
            case DeathNews dn:
                var d = state.Deaths.Find(x => x.Id == dn.DeathId);
                return d is not null ? DeathWeight(d, content) : 0;
            case InquestNews i:
                return i.Outcome == InquestStates.Implicated ? cfg.PerVerdict : 0;
            case BribeExposedNews:
                return cfg.PerExposedBribe;
            case StockSeizedNews s:
                return s.Bottles > 0 ? cfg.PerSeizure : 0;
            default:
                return 0;
        }
    }

    /// <summary>The share (0..1) left of something the memory puts off: 1 − loss at a memory of 100.</summary>
    public static double MemoryLoss(GameState state, double loss) =>
        1 - loss * Math.Min(1, Math.Max(0, state.Ward.Memory / 100));

    /// <summary>Whether a regular who is not hooked hears the talk and goes elsewhere this month.</summary>
    public static bool FrightenedAway(GameState state, Customer c, ContentPack content, Rng rng)
    {
        double p = content.Config.Ward.LeaveChance * (state.Ward.Memory / 100);
        return p > 0 && c.Body.Dependency < content.Config.Body.WithdrawalThreshold && rng.Chance(p);
    }

    /// <summary>
    /// Turn phase, after the body: last month's memory fades, and this month's harms are added to it
    /// (the deaths, the verdicts the coroner's juries returned, a bribe that came out, a seizure).
    /// </summary>
    public static readonly Phase WardPhase = new("ward", ctx =>
    {
        var state = ctx.State;
        var cfg = ctx.Content.Config.Ward;
        double m = Math.Max(0, state.Ward.Memory * (1 - cfg.MemoryDecay) - cfg.MemoryDecayFlat);
        foreach (var n in state.Month.News) m += NewsWeight(state, n, ctx.Content);
        state.Ward.Memory = Math.Min(100, m);
        state.Ward.PeakMemory = Math.Max(state.Ward.PeakMemory, state.Ward.Memory);
    });

    /// <summary>What the ward remembers over the last RememberMonths months (this one included).</summary>
    public static WardRemembers Remembers(GameState state, ContentPack content)
    {
        int months = content.Config.Ward.RememberMonths;
        int since = state.Meta.Turn - months + 1;
        var news = state.Ledger.Where(e => e.Turn >= since).SelectMany(e => e.News).ToList();
        if (state.Meta.Status == GameStatus.Running) news.AddRange(state.Month.News);
        int deaths = 0, infants = 0, strangers = 0, verdicts = 0, seizures = 0;
        foreach (var n in news)
        {
            switch (n)
            {
                case DeathNews dn:
                    var d = state.Deaths.Find(x => x.Id == dn.DeathId);
                    if (d is null || !BlamedOnTheBottle(d)) break;
                    deaths += 1;
                    if (d.Anonymous) strangers += 1;
                    else if (d.Consumer == Consumers.Infant) infants += 1;
                    break;
                case InquestNews i when i.Outcome == InquestStates.Implicated:
                    verdicts += 1;
                    break;
                case StockSeizedNews s when s.Bottles > 0:
                    seizures += 1;
                    break;
            }
        }
        return new WardRemembers(months, deaths, infants, strangers, verdicts, seizures);
    }
}
