namespace Panacea.Sim;

public readonly record struct DoseContext(double Potency, double DoseScale, double Vulnerability);

/// <summary>How a bottle acts on a particular body (systems/pharmacology.ts).</summary>
public static class Pharmacology
{
    public static DrugValues ZeroTolerance() => new();

    /// <summary>Felt kick and calm from one bottle, after tolerance.</summary>
    public static (double Kick, double Calm) FeltEffect(FormulaAnalysis a, DrugValues tolerance)
    {
        double kick = a.ByClass[Formulas.NoDrugClass].Kick;
        double calm = a.ByClass[Formulas.NoDrugClass].Calm;
        foreach (var c in DrugClasses.All)
        {
            double factor = 1 / (1 + tolerance[c]);
            kick += a.ByClass[c].Kick * factor;
            calm += a.ByClass[c].Calm * factor;
        }
        return (kick, calm);
    }

    /// <summary>How much relief a buyer feels from one bottle (before comparing with their ailment).</summary>
    public static double ReliefScore(FormulaAnalysis a, DrugValues tolerance, BuyerProfile profile)
    {
        var felt = FeltEffect(a, tolerance);
        return profile.Preference.Kick * felt.Kick + profile.Preference.Calm * felt.Calm;
    }

    /// <summary>How attractive a bottle looks to a buyer, before price.</summary>
    public static double Appeal(FormulaAnalysis a, DrugValues tolerance, BuyerProfile profile, double dependency, double claimAppeal)
    {
        double bitterPenalty =
            profile.BitternessAversion * Math.Max(0, a.Properties.Bitterness - profile.BitternessTolerance);
        double craving = dependency * Math.Min(5, a.Properties.Habit) * 0.5;
        return ReliefScore(a, tolerance, profile) - bitterPenalty + craving + 0.5 * claimAppeal;
    }

    /// <summary>Acute danger of one bottle for this body. Compared against a health-based threshold.</summary>
    public static double AcuteDanger(FormulaAnalysis a, DrugValues tolerance, DoseContext dose, ContentPack content)
    {
        double protection = content.Config.Body.ToleranceProtection;
        double danger = a.ByClass[Formulas.NoDrugClass].AcuteToxicity;
        foreach (var c in DrugClasses.All)
        {
            danger += a.ByClass[c].AcuteToxicity / (1 + protection * tolerance[c]);
        }
        return Math.Max(0, danger) * dose.Potency * dose.DoseScale * dose.Vulnerability;
    }

    public static double DangerThreshold(double health) => Math.Max(1, health / 10);

    /// <summary>Probability that one bottle kills outright, given its danger and the drinker's health.</summary>
    public static double OverdoseChance(double danger, double health)
    {
        double risk = danger / DangerThreshold(health);
        double x = Math.Min(1, Math.Max(0, (risk - 0.7) / 1.3));
        return x * x;
    }

    public static DoseContext DoseContextFor(string consumer, string segment, double potency, ContentPack content)
    {
        content.ById.Segments.TryGetValue(segment, out var seg);
        bool infant = consumer == Consumers.Infant;
        return new DoseContext(
            potency,
            infant ? content.Config.Body.InfantDoseScale : 1,
            (seg?.Vulnerability ?? 1) * (infant ? content.Config.Body.InfantVulnerability : 1));
    }

    public static Body NewBody(double baseHealth) => new()
    {
        Health = baseHealth,
        BaseHealth = baseHealth,
        Tolerance = ZeroTolerance(),
        Dependency = 0,
        Load = 0,
    };
}

/// <summary>The shop's regulars (systems/customers.ts).</summary>
public static class CustomersSystem
{
    /// <summary>Builds the shop's starting regulars from content.</summary>
    public static List<Customer> InitialCustomers(ContentPack content) =>
        content.Regulars.Select(r =>
        {
            content.ById.Segments.TryGetValue(r.Segment, out var seg);
            content.ById.Ailments.TryGetValue(r.Ailment, out var ailment);
            return new Customer
            {
                Id = r.Id,
                Name = r.DisplayName,
                Segment = r.Segment,
                Ailment = r.Ailment,
                Consumer = ailment?.Consumer ?? Consumers.Self,
                Profile = r.Profile.Clone(),
                JoinedTurn = 0,
                // Starting regulars' self-limiting complaints clear at the late end of their range.
                AilmentEndsTurn = ailment?.ResolvesAfterMonths is { } range ? range[1] : null,
                Body = Pharmacology.NewBody(seg?.BaseHealth ?? 75),
                Satisfaction = 0,
                Purchases = 0,
                LastPurchaseTurn = null,
                LastFormulaId = null,
                BottlesLastMonth = 0,
                InWithdrawal = false,
                DebtCents = 0,
            };
        }).ToList();

    public static Segment PickSegment(ContentPack content, Rng rng) =>
        rng.Weighted(content.Segments.Select(s => (s, s.Weight)).ToList())
        ?? throw new InvalidOperationException("no segments with positive weight");

    public static string PickAilment(Segment segment, Rng rng) =>
        rng.Weighted(segment.Ailments.Select(a => (a.Id, a.Weight)).ToList())!;

    public static int CustomerCapacity(GameState state, ContentPack content) =>
        content.Config.MaxRegulars + state.Player.Capabilities.GetOr("shop_capacity", 0);

    /// <summary>Creates a new regular from a walk-in of the given segment and ailment.</summary>
    public static Customer GenerateCustomer(GameState state, ContentPack content, Rng rng, Segment segment, string ailmentId)
    {
        bool female = segment.Gender == "female" || (segment.Gender == "any" && rng.Chance(0.5));
        string given = rng.Pick(female ? content.Names.Female : content.Names.Male);
        IReadOnlyList<string> titles = segment.Titles;
        if (segment.Gender == "any")
        {
            var gendered = titles.Where(t => female ? t != "Mr." : t == "Mr.").ToList();
            if (gendered.Count > 0) titles = gendered;
        }
        string title = rng.Pick(titles);
        string surname = rng.Pick(content.Names.Surnames);
        string name = $"{title} {given} {surname}";
        content.ById.Ailments.TryGetValue(ailmentId, out var ailment);
        var resolves = ailment?.ResolvesAfterMonths;
        string id = $"cust_{state.Meta.NextId++}";
        return new Customer
        {
            Id = id,
            Name = name,
            Segment = segment.Id,
            Ailment = ailmentId,
            Consumer = ailment?.Consumer ?? Consumers.Self,
            Profile = segment.Profile.Clone(),
            JoinedTurn = state.Meta.Turn,
            AilmentEndsTurn = resolves is not null ? state.Meta.Turn + rng.Int(resolves[0], resolves[1]) : null,
            Body = Pharmacology.NewBody(segment.BaseHealth),
            Satisfaction = 0.2,
            Purchases = 0,
            LastPurchaseTurn = null,
            LastFormulaId = null,
            BottlesLastMonth = 0,
            InWithdrawal = false,
            DebtCents = 0,
        };
    }
}
