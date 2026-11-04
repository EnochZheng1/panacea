namespace Panacea.Sim;

/// <summary>
/// Investments (systems/investments.ts): things the proprietor buys that pay back over months or
/// years. A branch shop in another ward, an almanac, a wholesale contract, a laboratory.
/// </summary>
public static class Investments
{
    public static bool IsHeld(GameState state, string id) => state.Player.Investments.ContainsKey(id);

    /// <summary>Why an investment cannot be bought right now, or null if it can.</summary>
    public static string? InvestRefusal(GameState state, Investment inv, ContentPack content)
    {
        if (IsHeld(state, inv.Id)) return "already held";
        if (inv.Requires is not null && !Requirements.Evaluate(inv.Requires, state, content).Met) return "not yet offered";
        if (state.Player.CashCents < inv.CostCents) return "not enough cash";
        return null;
    }

    public static ActionResult Invest(GameState state, Invest action, ContentPack content)
    {
        if (!content.ById.Investments.TryGetValue(action.InvestmentId, out var inv))
            return ActionResult.Fail($"unknown investment \"{action.InvestmentId}\"");
        var refusal = InvestRefusal(state, inv, content);
        if (refusal is not null) return ActionResult.Fail(refusal);
        Ledger.Adjust(state, Metrics.Cash, -inv.CostCents, Causes.Investment, inv.Id);
        state.Player.Investments[inv.Id] = new InvestmentHolding
        {
            BoughtTurn = state.Meta.Turn,
            EndsTurn = inv.Months is int months ? state.Meta.Turn + months : null,
        };
        Effects.Apply(state, inv.Effects, Causes.Investment, inv.Id);
        state.Month.News.Add(new InvestmentNews(inv.Id, InvestmentChanges.Bought));
        return ActionResult.Success;
    }

    /// <summary>Gives an investment up: its capability changes are undone.</summary>
    private static void EndInvestment(GameState state, Investment inv, string change)
    {
        foreach (var e in inv.Effects)
        {
            if (e is CapabilityEffect c)
                state.Player.Capabilities[c.Id] = state.Player.Capabilities.GetOr(c.Id, 0) - c.Delta;
        }
        state.Player.Investments.Remove(inv.Id);
        state.Month.News.Add(new InvestmentNews(inv.Id, change));
    }

    /// <summary>The investments the shop holds, in content order.</summary>
    public static List<Investment> Held(GameState state, ContentPack content) =>
        content.Investments.Where(inv => IsHeld(state, inv.Id)).ToList();

    /// <summary>The bottles a wholesale contract asks for this month: more, the better the name.</summary>
    public static int WholesaleOrder(GameState state, Investment inv) =>
        inv.Wholesale is { } w
            ? (int)Math.Floor(w.BottlesPerMonth * (1 + state.Player.Reputation * w.PerReputation))
            : 0;

    /// <summary>What the druggists pay for a bottle sold on the shelf at <paramref name="priceCents"/>.</summary>
    public static int WholesalePrice(Investment inv, int priceCents) =>
        inv.Wholesale is { } w ? (int)Math.Min(w.MaxPriceCents, JsMath.Round(priceCents * w.PricePct / 100)) : 0;

    /// <summary>
    /// Turn phase, after expenses: upkeep is paid, accidents happen, contracts are cancelled when
    /// their condition holds, and investments that ran their months lapse at the end of the last one.
    /// </summary>
    public static readonly Phase InvestmentsPhase = new("investments", ctx =>
    {
        var state = ctx.State;
        foreach (var inv in Held(state, ctx.Content))
        {
            if (!state.Player.Investments.TryGetValue(inv.Id, out var holding)) continue;
            if (inv.UpkeepCents > 0) Ledger.Adjust(state, Metrics.Cash, -inv.UpkeepCents, Causes.Upkeep, inv.Id);
            if (inv.Mishap is { } mishap && ctx.Rng.Chance(mishap.ChancePerMonth))
            {
                Effects.Apply(state, mishap.Effects, Causes.Mishap, inv.Id);
                state.Month.News.Add(new InvestmentNews(inv.Id, InvestmentChanges.Mishap));
            }
            if (inv.LostWhen is not null && Requirements.Evaluate(inv.LostWhen, state, ctx.Content).Met)
                EndInvestment(state, inv, InvestmentChanges.Lost);
            else if (holding.EndsTurn is int ends && ends <= state.Meta.Turn + 1)
                EndInvestment(state, inv, InvestmentChanges.Lapsed);
        }
    });
}
