namespace Panacea.Sim;

/// <summary>What a formula does (systems/formula.ts).</summary>
public sealed class FormulaAnalysis
{
    /// <summary>Fluid ounces per bottle.</summary>
    public double Volume { get; init; }
    /// <summary>Per-bottle properties after processing.</summary>
    public required Properties Properties { get; init; }
    /// <summary>The same properties split by drug class ("none" = no drug class). Sums to Properties.</summary>
    public required IReadOnlyDictionary<string, Properties> ByClass { get; init; }
    /// <summary>Ingredients + bottle + processing, per bottle, in (fractional) cents.</summary>
    public double UnitCostCents { get; init; }
}

public static class Formulas
{
    public const string NoDrugClass = "none";

    /// <summary>Computes what a formula does. Assumes the formula is valid.</summary>
    public static FormulaAnalysis Analyze(IReadOnlyList<FormulaComponent> components, string processId, ContentPack content)
    {
        var props = new Properties();
        var byClass = new Dictionary<string, Properties>(StringComparer.Ordinal);
        foreach (var c in DrugClasses.All) byClass[c] = new Properties();
        byClass[NoDrugClass] = new Properties();
        content.ById.Processes.TryGetValue(processId, out var process);
        double volume = 0;
        double cost = content.Config.BottleCostCents + (process?.ExtraCostPerBottleCents ?? 0);
        foreach (var c in components)
        {
            if (!content.ById.Ingredients.TryGetValue(c.IngredientId, out var ing)) continue;
            volume += c.Amount;
            cost += c.Amount * ing.CostPerUnitCents;
            var bucket = byClass[ing.DrugClass ?? NoDrugClass];
            foreach (var k in Properties.Keys)
            {
                double v = c.Amount * ing.Properties[k] * (process?.Multiplier(k) ?? 1);
                props[k] += v;
                bucket[k] += v;
            }
        }
        return new FormulaAnalysis { Volume = volume, Properties = props, ByClass = byClass, UnitCostCents = cost };
    }

    public static bool IsIngredientAvailable(string ingredientId, GameState state, ContentPack content)
    {
        if (!content.ById.Ingredients.TryGetValue(ingredientId, out var ing)) return false;
        return ing.AvailableFrom is null || Dates.Compare(state.Meta.Date, ing.AvailableFrom) >= 0;
    }

    public static bool IsProcessAvailable(string processId, GameState state, ContentPack content)
    {
        if (!content.ById.Processes.TryGetValue(processId, out var p)) return false;
        return string.IsNullOrEmpty(p.RequiresUnlock) || state.Unlocks.Unlocked.ContainsKey(p.RequiresUnlock);
    }

    /// <summary>Returns an error message, or null if the formula is valid right now.</summary>
    public static string? Validate(IReadOnlyList<FormulaComponent>? components, string? processId, GameState state, ContentPack content)
    {
        if (components is null || components.Count == 0) return "formula has no ingredients";
        var seen = new HashSet<string>(StringComparer.Ordinal);
        double volume = 0;
        foreach (var c in components)
        {
            if (c is null || c.IngredientId is null || !content.ById.Ingredients.ContainsKey(c.IngredientId))
                return $"unknown ingredient \"{c?.IngredientId}\"";
            if (!IsIngredientAvailable(c.IngredientId, state, content))
                return $"ingredient \"{c.IngredientId}\" is not yet available";
            if (seen.Contains(c.IngredientId)) return $"ingredient \"{c.IngredientId}\" listed twice";
            seen.Add(c.IngredientId);
            if (!double.IsFinite(c.Amount) || c.Amount <= 0)
                return $"amount for \"{c.IngredientId}\" must be positive";
            volume += c.Amount;
        }
        if (volume > content.Config.BottleCapacity + 1e-9)
        {
            return $"formula volume {JsJson.FormatNumber(volume)} oz exceeds bottle capacity " +
                   $"{JsJson.FormatNumber(content.Config.BottleCapacity)} oz";
        }
        if (processId is null || !content.ById.Processes.ContainsKey(processId)) return $"unknown process \"{processId}\"";
        if (!IsProcessAvailable(processId, state, content)) return $"process \"{processId}\" is not unlocked";
        return null;
    }

    /// <summary>Total boldness of a formula's claims (drives scrutiny).</summary>
    public static double ClaimBoldness(IReadOnlyList<string> claims, ContentPack content)
    {
        double sum = 0;
        foreach (var id in claims) sum = sum + (content.ById.Claims.TryGetValue(id, out var c) ? c.Boldness : 0);
        return sum;
    }

    /// <summary>How strongly a formula's claims appeal to someone with this ailment.</summary>
    public static double ClaimAppeal(IReadOnlyList<string> claims, string ailmentId, ContentPack content)
    {
        double total = 0;
        foreach (var id in claims)
        {
            if (!content.ById.Claims.TryGetValue(id, out var claim)) continue;
            if (claim.Ailments.Count == 0 || claim.Ailments.Contains(ailmentId)) total += claim.Appeal;
        }
        return total;
    }

    /// <summary>
    /// How far a bottle fell short of its label for a buyer with this complaint who felt
    /// <paramref name="relief"/> beyond it (formula.ts promiseShortfall, round 10). An ordinary claim
    /// that names the complaint promised PromiseShare × its appeal, made good by that much relief (the
    /// shortfall is at most the promise); a claim to cure (Claim.Cures) promised CureShare × its appeal,
    /// which no bottle makes good, its part growing over CurePatience months as a regular.
    /// </summary>
    public static double PromiseShortfall(IReadOnlyList<string> claims, string ailmentId, double relief, double months, ContentPack content)
    {
        var m = content.Config.Market;
        double promised = 0;
        double cure = 0;
        foreach (var id in claims)
        {
            if (!content.ById.Claims.TryGetValue(id, out var claim)) continue;
            if (claim.Ailments.Count > 0 && !claim.Ailments.Contains(ailmentId)) continue;
            if (claim.Cures) cure += m.CureShare * claim.Appeal;
            else promised += m.PromiseShare * claim.Appeal;
        }
        double patience = m.CurePatience > 0 ? Math.Min(1, Math.Max(0, months) / m.CurePatience) : 1;
        return Math.Min(promised, Math.Max(0, promised - relief)) + cure * patience;
    }

    /// <summary>A complaint that kills (its MortalityMultiplier is over 1).</summary>
    private static bool IsDeadly(string ailmentId, ContentPack content) =>
        (content.ById.Ailments.TryGetValue(ailmentId, out var a) ? a.MortalityMultiplier : 1) > 1;

    /// <summary>
    /// Whether a label claims to cure this complaint outright, and the complaint kills: a customer who
    /// dies of it is a broken promise.
    /// </summary>
    public static bool PromisesCure(IReadOnlyList<string> claims, string? ailmentId, ContentPack content)
    {
        if (ailmentId is null || !IsDeadly(ailmentId, content)) return false;
        foreach (var id in claims)
        {
            if (!content.ById.Claims.TryGetValue(id, out var claim) || !claim.Cures) continue;
            if (claim.Ailments.Count == 0 || claim.Ailments.Contains(ailmentId)) return true;
        }
        return false;
    }

    /// <summary>Claims on a label to cure a deadly disease (AdvertisingConfig.CuresHeat).</summary>
    public static int CureClaims(IReadOnlyList<string> claims, ContentPack content)
    {
        int n = 0;
        foreach (var id in claims)
        {
            if (!content.ById.Claims.TryGetValue(id, out var claim) || !claim.Cures) continue;
            if (claim.Ailments.Count == 0 || claim.Ailments.Any(a => IsDeadly(a, content))) n++;
        }
        return n;
    }

    /// <summary>Returns an error message, or null if this set of claims may be printed right now.</summary>
    public static string? ValidateClaims(IReadOnlyList<string>? claims, GameState state, ContentPack content)
    {
        if (claims is null) return "claims must be a list";
        if (claims.Count > content.Config.MaxClaimsPerFormula)
            return $"at most {content.Config.MaxClaimsPerFormula} claims per label";
        if (new HashSet<string>(claims, StringComparer.Ordinal).Count != claims.Count) return "claims must not repeat";
        foreach (var id in claims)
        {
            if (id is null || !content.ById.Claims.TryGetValue(id, out var claim)) return $"unknown claim \"{id}\"";
            if (!string.IsNullOrEmpty(claim.RequiresUnlock) && !state.Unlocks.Unlocked.ContainsKey(claim.RequiresUnlock))
                return $"claim \"{id}\" is not unlocked";
        }
        return null;
    }
}
