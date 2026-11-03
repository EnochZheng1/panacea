namespace Panacea.Sim;

/// <summary>Rules that must hold in every reachable state (packages/sim/src/invariants.ts).</summary>
public static class Invariants
{
    private const double Eps = 1e-6;
    private const long MaxSafeInteger = 9007199254740991;

    /// <summary>Returns human-readable violations (empty when healthy).</summary>
    public static List<string> Check(GameState state, ContentPack content)
    {
        var v = new List<string>();
        var player = state.Player;
        var lab = state.Lab;
        var meta = state.Meta;
        var scrutiny = state.Scrutiny;
        string N(double x) => JsJson.FormatNumber(x);

        if (Math.Abs(player.CashCents) > MaxSafeInteger) v.Add($"cash is not an integer: {player.CashCents}");
        if (!(player.Reputation >= Ledger.ReputationMin - Eps && player.Reputation <= Ledger.ReputationMax + Eps))
            v.Add($"reputation out of range: {N(player.Reputation)}");
        if (!(player.Notoriety >= -Eps)) v.Add($"notoriety negative: {N(player.Notoriety)}");
        if (!(scrutiny.Heat >= -Eps && scrutiny.Heat <= Ledger.HeatMax + Eps)) v.Add($"heat out of range: {N(scrutiny.Heat)}");

        // Lab
        foreach (var (id, qty) in lab.Inventory)
        {
            if (!double.IsFinite(qty) || qty < -Eps) v.Add($"inventory \"{id}\" invalid: {N(qty)}");
            if (!content.ById.Ingredients.ContainsKey(id)) v.Add($"inventory holds unknown ingredient \"{id}\"");
        }
        foreach (var lot in lab.Stock)
        {
            if (lot.Bottles <= 0) v.Add($"lot \"{lot.Id}\" has invalid bottles: {lot.Bottles}");
            if (!(lot.Potency > 0) || !double.IsFinite(lot.Potency)) v.Add($"lot \"{lot.Id}\" has invalid potency: {N(lot.Potency)}");
            if (!lab.Formulas.Any(f => f.Id == lot.FormulaId)) v.Add($"lot \"{lot.Id}\" is of unknown formula \"{lot.FormulaId}\"");
        }
        if (lab.Formulas.Count > content.Config.MaxFormulas) v.Add("too many formulas");
        foreach (var f in lab.Formulas)
        {
            if (f.Claims.Count > content.Config.MaxClaimsPerFormula) v.Add($"formula \"{f.Id}\" has too many claims");
        }

        // Customers
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in state.Customers)
        {
            if (ids.Contains(c.Id)) v.Add($"duplicate customer \"{c.Id}\"");
            ids.Add(c.Id);
            var b = c.Body;
            if (!(b.Health > 0 && b.Health <= 100 + Eps)) v.Add($"customer \"{c.Id}\" health invalid: {N(b.Health)}");
            if (!(b.Dependency >= 0 && b.Dependency <= 1)) v.Add($"customer \"{c.Id}\" dependency invalid");
            if (!(b.Load >= -Eps) || !double.IsFinite(b.Load)) v.Add($"customer \"{c.Id}\" load invalid");
            foreach (var cls in DrugClasses.All)
            {
                if (!(b.Tolerance[cls] >= -Eps) || !double.IsFinite(b.Tolerance[cls]))
                    v.Add($"customer \"{c.Id}\" tolerance \"{cls}\" invalid");
            }
            if (!(c.Satisfaction >= -1 - Eps && c.Satisfaction <= 1 + Eps)) v.Add($"customer \"{c.Id}\" satisfaction out of range");
            if (!content.ById.Segments.ContainsKey(c.Segment)) v.Add($"customer \"{c.Id}\" has unknown segment");
            if (!content.ById.Ailments.ContainsKey(c.Ailment)) v.Add($"customer \"{c.Id}\" has unknown ailment");
        }
        if (state.Deaths.Any(d => d.CustomerId is not null && ids.Contains(d.CustomerId) && d.Consumer == Consumers.Self))
            v.Add("a dead customer is still a customer");

        // Time
        if (meta.Date.Month < 1 || meta.Date.Month > 12) v.Add($"invalid month {meta.Date.Month}");
        if (Dates.Index(meta.Date) - Dates.Index(content.Config.StartDate) != meta.Turn) v.Add($"turn {meta.Turn} does not match date");
        if (Dates.Compare(meta.Date, content.Config.EndDate) > 0) v.Add("date is past the end date");
        if (meta.Status == GameStatus.Ended && meta.EndReason is null) v.Add("ended without a reason");
        if (meta.Status == GameStatus.Running && meta.EndReason is not null) v.Add("running with an end reason");

        // The Ledger must explain every change: deltas sum to the actual change, months chain up.
        var entries = state.Ledger
            .Select(e => (e.Turn, e.CashStartCents, e.CashEndCents, e.ReputationStart, e.ReputationEnd,
                e.NotorietyStart, e.NotorietyEnd, e.HeatStart, e.HeatEnd, Deltas: (IReadOnlyList<CausalDelta>)e.Deltas))
            .ToList();
        if (meta.Status == GameStatus.Running)
        {
            var m = state.Month;
            entries.Add((meta.Turn, m.StartCashCents, player.CashCents, m.StartReputation, player.Reputation,
                m.StartNotoriety, player.Notoriety, m.StartHeat, scrutiny.Heat, m.Deltas));
        }
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            var checks = new (string Metric, double Start, double End)[]
            {
                (Metrics.Cash, e.CashStartCents, e.CashEndCents),
                (Metrics.Reputation, e.ReputationStart, e.ReputationEnd),
                (Metrics.Notoriety, e.NotorietyStart, e.NotorietyEnd),
                (Metrics.Heat, e.HeatStart, e.HeatEnd),
            };
            foreach (var (metric, start, end) in checks)
            {
                double sum = Ledger.SumDeltas(e.Deltas, metric);
                if (Math.Abs(sum - (end - start)) > Eps) v.Add($"ledger turn {e.Turn}: {metric} deltas do not explain the change");
            }
            if (i > 0 && entries[i - 1].CashEndCents != e.CashStartCents)
                v.Add($"ledger turn {e.Turn}: cash does not continue from the previous month");
        }

        // Events
        int randomPending = 0;
        foreach (var p in state.Events.Pending)
        {
            if (!content.ById.Events.TryGetValue(p.EventId, out var ev))
            {
                v.Add($"pending unknown event \"{p.EventId}\"");
                continue;
            }
            if (ev.Trigger == "random") randomPending++;
            if (ev.Trigger == "system")
            {
                var death = state.Deaths.Find(d => d.Id == p.DeathId);
                if (death is null || death.Inquest != InquestStates.Pending) v.Add($"inquest \"{p.InstanceId}\" has no pending death");
            }
        }
        if (randomPending > content.Config.MaxEventsPerMonth) v.Add("too many pending events");
        foreach (var d in state.Deaths)
        {
            if (d.Inquest == InquestStates.Pending && !state.Events.Pending.Any(p => p.DeathId == d.Id))
                v.Add($"death \"{d.Id}\" awaits an inquest that is not scheduled");
        }
        return v;
    }
}
