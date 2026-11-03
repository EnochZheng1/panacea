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
