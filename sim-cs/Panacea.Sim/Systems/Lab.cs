namespace Panacea.Sim;

/// <summary>The lab: buying, formulas, brewing and production (systems/lab.ts).</summary>
public static class Lab
{
    /// <summary>Inventory quantities are rounded to avoid floating-point dust accumulating over a run.</summary>
    public static double RoundQty(double x) => JsMath.Round(x * 1e6) / 1e6;

    private static bool IsValidPrice(int p) => p >= 0 && p <= ActionLimits.MaxPriceCents;

    /// <summary>Bottles of a formula on the shelf.</summary>
    public static int StockOf(GameState state, string formulaId)
    {
        int n = 0;
        foreach (var lot in state.Lab.Stock)
            if (lot.FormulaId == formulaId) n += lot.Bottles;
        return n;
    }

    public static int TotalStock(GameState state)
    {
        int n = 0;
        foreach (var lot in state.Lab.Stock) n += lot.Bottles;
        return n;
    }

    /// <summary>Takes one bottle of a formula from its oldest lot.</summary>
    public static Lot? TakeBottle(GameState state, string formulaId)
    {
        int index = state.Lab.Stock.FindIndex(l => l.FormulaId == formulaId && l.Bottles > 0);
        if (index < 0) return null;
        var lot = state.Lab.Stock[index];
        lot.Bottles -= 1;
        var taken = lot.ShallowCopy();
        if (lot.Bottles == 0) state.Lab.Stock.RemoveAt(index);
        return taken;
    }

    public static double IngredientUnitCost(GameState state, string ingredientId, ContentPack content)
    {
        if (!content.ById.Ingredients.TryGetValue(ingredientId, out var ing)) return double.PositiveInfinity;
        double discount = Math.Min(90, state.Player.Capabilities.GetOr("ingredient_discount_pct", 0));
        // a supplier's rise (a shortage, a trust) for one ingredient
        double surcharge = Math.Max(0, state.Player.Capabilities.GetOr($"surcharge_pct_{ing.Id}", 0));
        return ing.CostPerUnitCents * (1 - discount / 100) * (1 + surcharge / 100);
    }

    /// <summary>
    /// What the shop's stock is worth at today's cost (with any discount or surcharge): the
    /// ingredients on hand, and every bottle brewing or on the shelf at what its ingredients and
    /// bottling would cost now. Money spent on stock is not lost, and the books can say so.
    /// </summary>
    public static StockValue StockValue(GameState state, ContentPack content)
    {
        double ingredients = 0;
        foreach (var (id, qty) in state.Lab.Inventory)
            if (qty > 0) ingredients += qty * IngredientUnitCost(state, id, content);
        double BottleCost(List<FormulaComponent> components, string processId)
        {
            double cost = BottlingCostPerBottle(processId, content);
            foreach (var c in components) cost += c.Amount * IngredientUnitCost(state, c.IngredientId, content);
            return cost;
        }
        double shelf = 0;
        foreach (var b in state.Lab.Batches) shelf += b.Bottles * BottleCost(b.Components, b.ProcessId);
        foreach (var lot in state.Lab.Stock) shelf += lot.Bottles * BottleCost(lot.Components, lot.ProcessId);
        long shelfCents = (long)JsMath.Round(shelf);
        long ingredientsCents = (long)JsMath.Round(ingredients);
        return new StockValue(shelfCents, ingredientsCents, shelfCents + ingredientsCents);
    }

    public static ActionResult BuyIngredient(GameState state, BuyIngredient action, ContentPack content)
    {
        if (action.IngredientId is null || !content.ById.Ingredients.TryGetValue(action.IngredientId, out var ing))
            return ActionResult.Fail($"unknown ingredient \"{action.IngredientId}\"");
        if (!Formulas.IsIngredientAvailable(ing.Id, state, content))
            return ActionResult.Fail($"\"{ing.Id}\" is not yet available");
        if (action.Units < 1) return ActionResult.Fail("units must be a positive integer");
        double cost = JsMath.Round(action.Units * IngredientUnitCost(state, ing.Id, content));
        if (cost > state.Player.CashCents) return ActionResult.Fail("not enough cash");
        Ledger.Adjust(state, Metrics.Cash, -cost, Causes.Ingredients, ing.Id);
        state.Lab.Inventory[ing.Id] = RoundQty(state.Lab.Inventory.GetOr(ing.Id, 0) + action.Units);
        return ActionResult.Success;
    }

    /// <summary>JavaScript String.prototype.trim (ECMAScript WhiteSpace and LineTerminator).</summary>
    internal static string JsTrim(string s)
    {
        static bool IsJsSpace(char c) => c switch
        {
            (char)0x09 or (char)0x0a or (char)0x0b or (char)0x0c or (char)0x0d or (char)0x20 => true,
            (char)0xa0 or (char)0x1680 or (char)0x2028 or (char)0x2029 or (char)0x202f or (char)0x205f => true,
            (char)0x3000 or (char)0xfeff => true,
            >= (char)0x2000 and <= (char)0x200a => true,
            _ => false,
        };
        int start = 0, end = s.Length;
        while (start < end && IsJsSpace(s[start])) start++;
        while (end > start && IsJsSpace(s[end - 1])) end--;
        return s[start..end];
    }

    public static ActionResult CreateFormula(GameState state, CreateFormula action, ContentPack content)
    {
        if (state.Lab.Formulas.Count >= content.Config.MaxFormulas) return ActionResult.Fail("formula book is full");
        string name = action.Name is null ? "" : JsTrim(action.Name);
        if (name.Length == 0 || name.Length > ActionLimits.MaxFormulaNameLength)
            return ActionResult.Fail($"name must be 1-{ActionLimits.MaxFormulaNameLength} characters");
        if (!IsValidPrice(action.PriceCents)) return ActionResult.Fail("invalid price");
        var error = Formulas.Validate(action.Components, action.ProcessId, state, content);
        if (error is not null) return ActionResult.Fail(error);
        var claims = action.Claims ?? [];
        var claimError = Formulas.ValidateClaims(claims, state, content);
        if (claimError is not null) return ActionResult.Fail(claimError);
        string id = $"formula_{state.Meta.NextId++}";
        state.Lab.Formulas.Add(new Formula
        {
            Id = id,
            Name = name,
            Components = action.Components.Select(c => new FormulaComponent(c.IngredientId, c.Amount)).ToList(),
            ProcessId = action.ProcessId,
            PriceCents = action.PriceCents,
            Claims = claims.ToList(),
            CreatedTurn = state.Meta.Turn,
        });
        return ActionResult.Created(id);
    }

    private static Formula? Find(GameState state, string? formulaId) =>
        state.Lab.Formulas.Find(f => f.Id == formulaId);

    public static ActionResult SetPrice(GameState state, SetPrice action)
    {
        var formula = Find(state, action.FormulaId);
        if (formula is null) return ActionResult.Fail($"unknown formula \"{action.FormulaId}\"");
        if (!IsValidPrice(action.PriceCents)) return ActionResult.Fail("invalid price");
        formula.PriceCents = action.PriceCents;
        return ActionResult.Success;
    }

    public static ActionResult Reformulate(GameState state, Reformulate action, ContentPack content)
    {
        var formula = Find(state, action.FormulaId);
        if (formula is null) return ActionResult.Fail($"unknown formula \"{action.FormulaId}\"");
        var error = Formulas.Validate(action.Components, action.ProcessId, state, content);
        if (error is not null) return ActionResult.Fail(error);
        formula.Components = action.Components.Select(c => new FormulaComponent(c.IngredientId, c.Amount)).ToList();
        formula.ProcessId = action.ProcessId;
        return ActionResult.Success;
    }

    public static ActionResult SetClaims(GameState state, SetClaims action, ContentPack content)
    {
        var formula = Find(state, action.FormulaId);
        if (formula is null) return ActionResult.Fail($"unknown formula \"{action.FormulaId}\"");
        var error = Formulas.ValidateClaims(action.Claims, state, content);
        if (error is not null) return ActionResult.Fail(error);
        formula.Claims = action.Claims.ToList();
        return ActionResult.Success;
    }

    public static ActionResult SetAdSpend(GameState state, SetAdSpend action)
    {
        if (!state.Unlocks.Unlocked.ContainsKey("newspaper_ads"))
            return ActionResult.Fail("newspaper advertising is not unlocked");
        if (action.Cents < 0 || action.Cents > ActionLimits.MaxAdSpendCents)
            return ActionResult.Fail("invalid advertising budget");
        state.Marketing.AdSpendCents = action.Cents;
        return ActionResult.Success;
    }

    public static int StillsFree(GameState state)
    {
        int busy = 0;
        foreach (var b in state.Lab.Batches)
            if (b.StartedTurn == state.Meta.Turn) busy++;
        return Math.Max(0, state.Player.Capabilities.GetOr("stills", 0) - busy);
    }

    public static int BottlingCostPerBottle(string processId, ContentPack content) =>
        content.Config.BottleCostCents +
        (content.ById.Processes.TryGetValue(processId, out var p) ? p.ExtraCostPerBottleCents : 0);

    /// <summary>The largest batch the player could brew right now (ingredients, cash, and a free still).</summary>
    public static int MaxBrewable(GameState state, string formulaId, ContentPack content)
    {
        var formula = Find(state, formulaId);
        if (formula is null || StillsFree(state) == 0) return 0;
        double max = double.PositiveInfinity;
        foreach (var c in formula.Components)
        {
            max = Math.Min(max, Math.Floor(RoundQty(state.Lab.Inventory.GetOr(c.IngredientId, 0) / c.Amount)));
        }
        int perBottle = BottlingCostPerBottle(formula.ProcessId, content);
        if (perBottle > 0) max = Math.Min(max, Math.Floor((double)state.Player.CashCents / perBottle));
        return double.IsFinite(max) ? (int)Math.Max(0, max) : 0;
    }

    public static ActionResult BrewBatch(GameState state, BrewBatch action, ContentPack content)
    {
        var formula = Find(state, action.FormulaId);
        if (formula is null) return ActionResult.Fail($"unknown formula \"{action.FormulaId}\"");
        if (action.Bottles < 1) return ActionResult.Fail("bottles must be a positive integer");
        if (StillsFree(state) == 0) return ActionResult.Fail("no free still this month");
        foreach (var c in formula.Components)
        {
            double need = RoundQty(c.Amount * action.Bottles);
            if (state.Lab.Inventory.GetOr(c.IngredientId, 0) + 1e-9 < need)
                return ActionResult.Fail($"not enough \"{c.IngredientId}\" (need {JsJson.FormatNumber(need)} oz)");
        }
        long cost = (long)action.Bottles * BottlingCostPerBottle(formula.ProcessId, content);
        if (cost > state.Player.CashCents) return ActionResult.Fail("not enough cash for bottles");

        foreach (var c in formula.Components)
        {
            double left = RoundQty(state.Lab.Inventory.GetOr(c.IngredientId, 0) - c.Amount * action.Bottles);
            state.Lab.Inventory[c.IngredientId] = Math.Max(0, left);
        }
        Ledger.Adjust(state, Metrics.Cash, -cost, Causes.Bottling, formula.Id);
        string id = $"batch_{state.Meta.NextId++}";
        state.Lab.Batches.Add(new Batch
        {
            Id = id,
            FormulaId = formula.Id,
            Components = formula.Components.ToList(),
            ProcessId = formula.ProcessId,
            Bottles = action.Bottles,
            StartedTurn = state.Meta.Turn,
            ReadyTurn = state.Meta.Turn,
        });
        state.Stats.BatchesBrewed += 1;
        return ActionResult.Created(id);
    }

    /// <summary>
    /// Turn phase: finished batches go onto the shelf as lots. Every batch's true strength varies a
    /// little; occasionally one is badly mis-measured. With quality control, bad batches are caught.
    /// </summary>
    public static readonly Phase ProductionPhase = new("production", ctx =>
    {
        var state = ctx.State;
        var cfg = ctx.Content.Config.Batches;
        var rng = ctx.Rng;
        bool qc = state.Player.Capabilities.GetOr("quality_control", 0) > 0;
        var remaining = new List<Batch>();
        foreach (var b in state.Lab.Batches)
        {
            if (b.ReadyTurn > state.Meta.Turn)
            {
                remaining.Add(b);
                continue;
            }
            double spread = qc ? cfg.PotencySpread / 2 : cfg.PotencySpread;
            double potency = Math.Min(1.5, Math.Max(0.5, 1 + spread * Rng.Gaussian(rng)));
            bool badBatch = false;
            if (rng.Chance(cfg.BadBatchChance))
            {
                badBatch = true;
                potency = cfg.BadBatchPotency[0] + rng.Float() * (cfg.BadBatchPotency[1] - cfg.BadBatchPotency[0]);
            }
            if (badBatch && qc)
            {
                state.Month.News.Add(new BadBatchCaughtNews(b.FormulaId, b.Bottles));
                continue;
            }
            state.Lab.Stock.Add(new Lot
            {
                Id = b.Id,
                FormulaId = b.FormulaId,
                Components = b.Components,
                ProcessId = b.ProcessId,
                Bottles = b.Bottles,
                Potency = potency,
                BadBatch = badBatch,
            });
        }
        state.Lab.Batches = remaining;
    });
}

/// <summary>The stock at cost (systems/lab.ts StockValue): bottles brewing or on the shelf, and ingredients on hand.</summary>
public sealed record StockValue(long ShelfCents, long IngredientsCents, long TotalCents);
