namespace Panacea.Sim;

public readonly record struct RequirementStatus(bool Met, double Progress);

public sealed record UnlockProgress(string Id, double Progress);

/// <summary>
/// One condition of a requirement as numbers a progress line can print (requirements.ts
/// ConditionProgress): "1,120 of 1,500 bottles sold", "suspicion 12, offered below 30".
/// </summary>
/// <param name="Kind">What is counted: a requirement type ("bottlesSold", "regulars", "cash", "heat",
/// "unlocked", "flag", "date", …), "not" for a negated condition that is not a plain threshold, or
/// "cost" for an investment's price against the cash in hand.</param>
/// <param name="Id">The unlock or flag waited on ("unlocked", "flag"), or null.</param>
/// <param name="Sense">"atLeast": met once Have reaches Need. "below": met while Have stays under Need
/// (a ceiling, such as "while the talk in the ward is low").</param>
/// <param name="Have">The present count. Cash and cost in cents; unlocked and flag 1 or 0; date a month index.</param>
/// <param name="Need">The threshold.</param>
/// <param name="Met">Whether this condition holds now.</param>
/// <param name="Via">On the conditions of an unlock that is itself waited on: that unlock's id.</param>
public sealed record ConditionProgress(string Kind, string? Id, string Sense, double Have, double Need, bool Met, string? Via);

/// <summary>Requirements (systems/requirements.ts).</summary>
public static class Requirements
{
    private static RequirementStatus Ratio(double value, double target) =>
        target <= 0 || value >= target
            ? new RequirementStatus(true, 1)
            : new RequirementStatus(false, Math.Max(0, Math.Min(1, value / target)));

    private static RequirementStatus Binary(bool met) => new(met, met ? 1 : 0);

    /// <summary>The count a numeric requirement compares.</summary>
    private static double CountFor(AtLeastRequirement r, GameState state) => r.Kind switch
    {
        "cash" => state.Player.CashCents,
        "reputation" => state.Player.Reputation,
        "notoriety" => state.Player.Notoriety,
        "bottlesSold" => state.Stats.BottlesSold,
        "batchesBrewed" => state.Stats.BatchesBrewed,
        "heat" => state.Scrutiny.Heat,
        "deaths" => state.Deaths.Count,
        "inquests" => state.Scrutiny.InquestsHeld,
        "regulars" => state.Customers.Count,
        _ => throw new InvalidOperationException($"unknown requirement \"{r.Kind}\""),
    };

    /// <summary>
    /// The leaves of a requirement as progress lines, in content order (requirements.ts
    /// requirementProgress). "All" lists every part; "any" the parts of its most-met branch; "not"
    /// turns a threshold into a ceiling. An "unlocked" condition not yet met is followed by its
    /// unlock's own conditions, marked Via that unlock.
    /// </summary>
    public static List<ConditionProgress> Progress(Requirement req, GameState state, ContentPack content, string? via = null)
    {
        switch (req)
        {
            case AllRequirement all:
                return all.Of.SelectMany(r => Progress(r, state, content, via)).ToList();
            case AnyRequirement any:
            {
                var best = any.Of[0];
                double bestProgress = double.NegativeInfinity;
                foreach (var r in any.Of)
                {
                    double p = Evaluate(r, state, content).Progress;
                    if (p > bestProgress)
                    {
                        best = r;
                        bestProgress = p;
                    }
                }
                return Progress(best, state, content, via);
            }
            case NotRequirement not:
            {
                var inner = Progress(not.Requirement, state, content, via);
                // A ceiling on one number reads as that number held below its threshold.
                if (inner.Count == 1 && inner[0].Sense == "atLeast" && inner[0].Via == via)
                    return [inner[0] with { Sense = "below", Met = !inner[0].Met }];
                bool met = !Evaluate(not.Requirement, state, content).Met;
                return [new ConditionProgress("not", null, "below", met ? 0 : 1, 1, met, via)];
            }
            case UnlockedRequirement u:
            {
                bool met = state.Unlocks.Unlocked.ContainsKey(u.Id);
                var line = new ConditionProgress("unlocked", u.Id, "atLeast", met ? 1 : 0, 1, met, via);
                if (met || !content.ById.Unlocks.TryGetValue(u.Id, out var unlock)) return [line];
                return [line, .. Progress(unlock.Requirement, state, content, u.Id)];
            }
            case FlagRequirement f:
            {
                bool met = state.Player.Flags.TryGetValue(f.Id, out var flag) && flag;
                return [new ConditionProgress("flag", f.Id, "atLeast", met ? 1 : 0, 1, met, via)];
            }
            case DateRequirement d:
            {
                int have = Dates.Index(state.Meta.Date);
                int need = Dates.Index(d.From);
                return [new ConditionProgress("date", null, "atLeast", have, need, have >= need, via)];
            }
            case AtLeastRequirement r:
            {
                double have = CountFor(r, state);
                double need = r.AtLeast;
                return [new ConditionProgress(r.Kind, null, "atLeast", have, need, need <= 0 || have >= need, via)];
            }
            default:
                throw new InvalidOperationException($"unknown requirement {req.Type}");
        }
    }

    public static RequirementStatus Evaluate(Requirement req, GameState state, ContentPack content)
    {
        switch (req)
        {
            case AtLeastRequirement r:
                double value = r.Kind switch
                {
                    "cash" => state.Player.CashCents,
                    "reputation" => state.Player.Reputation,
                    "notoriety" => state.Player.Notoriety,
                    "bottlesSold" => state.Stats.BottlesSold,
                    "batchesBrewed" => state.Stats.BatchesBrewed,
                    "heat" => state.Scrutiny.Heat,
                    "deaths" => state.Deaths.Count,
                    "inquests" => state.Scrutiny.InquestsHeld,
                    "regulars" => state.Customers.Count,
                    _ => throw new InvalidOperationException($"unknown requirement \"{r.Kind}\""),
                };
                return Ratio(value, r.AtLeast);
            case DateRequirement d:
            {
                int start = Dates.Index(content.Config.StartDate);
                int now = Dates.Index(state.Meta.Date);
                int target = Dates.Index(d.From);
                if (now >= target) return new RequirementStatus(true, 1);
                return new RequirementStatus(false, Math.Max(0, (double)(now - start) / Math.Max(1, target - start)));
            }
            case FlagRequirement f:
                return Binary(state.Player.Flags.TryGetValue(f.Id, out var flag) && flag);
            case UnlockedRequirement u:
                return Binary(state.Unlocks.Unlocked.ContainsKey(u.Id));
            case AllRequirement all:
            {
                // as far along as its least-met part, so a progress bar never reads nearer than it is
                var parts = all.Of.Select(r => Evaluate(r, state, content)).ToList();
                double min = double.PositiveInfinity;
                foreach (var p in parts) min = Math.Min(min, p.Progress);
                return new RequirementStatus(parts.All(p => p.Met), min);
            }
            case AnyRequirement any:
            {
                var parts = any.Of.Select(r => Evaluate(r, state, content)).ToList();
                double max = double.NegativeInfinity;
                foreach (var p in parts) max = Math.Max(max, p.Progress);
                return new RequirementStatus(parts.Any(p => p.Met), max);
            }
            case NotRequirement not:
                return Binary(!Evaluate(not.Requirement, state, content).Met);
            default:
                throw new InvalidOperationException($"unknown requirement {req.Type}");
        }
    }
}

/// <summary>Content-defined effects (systems/effects.ts).</summary>
public static class Effects
{
    /// <summary>Applies content-defined effects, attributing every numeric change to <paramref name="cause"/>.</summary>
    public static void Apply(GameState state, IReadOnlyList<Effect> effects, string cause, string? detail = null)
    {
        foreach (var e in effects)
        {
            switch (e)
            {
                case CashEffect c:
                    Ledger.Adjust(state, Metrics.Cash, c.DeltaCents, cause, detail);
                    break;
                case MetricEffect m:
                    Ledger.Adjust(state, m.Metric, m.Delta, cause, detail);
                    break;
                case SeizeStockEffect:
                {
                    int bottles = Lab.TotalStock(state);
                    state.Lab.Stock = new List<Lot>();
                    state.Month.News.Add(new StockSeizedNews(bottles));
                    break;
                }
                case InquestEffect:
                    // Needs the event's context; handled by ResolveEvent.
                    break;
                case SetFlagEffect s:
                    state.Player.Flags[s.Id] = true;
                    break;
                case ClearFlagEffect c:
                    state.Player.Flags.Remove(c.Id);
                    break;
                case CapabilityEffect c:
                    state.Player.Capabilities[c.Id] = state.Player.Capabilities.GetOr(c.Id, 0) + c.Delta;
                    if (c.Months is int months)
                    {
                        state.Player.TimedCapabilities.Add(new TimedCapability
                        {
                            Id = c.Id,
                            Delta = c.Delta,
                            ExpiresTurn = state.Meta.Turn + months,
                        });
                    }
                    break;
            }
        }
    }
}

/// <summary>Timeline, unlocks and events (systems/progression.ts).</summary>
public static class Progression
{
    /// <summary>Turn phase: historical events fire on their dates, regardless of the player.</summary>
    public static readonly Phase TimelinePhase = new("timeline", ctx =>
    {
        var state = ctx.State;
        foreach (var entry in ctx.Content.Timeline)
        {
            if (state.Timeline.Fired.ContainsKey(entry.Id)) continue;
            if (Dates.Compare(entry.Date, state.Meta.Date) > 0) continue;
            state.Timeline.Fired[entry.Id] = state.Meta.Turn;
            Effects.Apply(state, entry.Effects, Causes.Timeline, entry.Id);
            state.Month.News.Add(new TimelineNews(entry.Id));
        }
    });

    /// <summary>
    /// Turn phase, last: capability changes that were only for a while run out at the end of their
    /// last month, so a change made in month t for n months holds through months t … t+n−1.
    /// </summary>
    public static readonly Phase TimersPhase = new("timers", ctx =>
    {
        var state = ctx.State;
        var live = new List<TimedCapability>();
        foreach (var t in state.Player.TimedCapabilities)
        {
            if (t.ExpiresTurn > state.Meta.Turn + 1)
            {
                live.Add(t);
                continue;
            }
            state.Player.Capabilities[t.Id] = state.Player.Capabilities.GetOr(t.Id, 0) - t.Delta;
        }
        state.Player.TimedCapabilities = live;
    });

    /// <summary>Turn phase: grant any unlock whose requirement is now met.</summary>
    public static readonly Phase UnlocksPhase = new("unlocks", ctx =>
    {
        var state = ctx.State;
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var unlock in ctx.Content.Unlocks)
            {
                if (state.Unlocks.Unlocked.ContainsKey(unlock.Id)) continue;
                if (!Requirements.Evaluate(unlock.Requirement, state, ctx.Content).Met) continue;
                state.Unlocks.Unlocked[unlock.Id] = state.Meta.Turn;
                Effects.Apply(state, unlock.Effects, Causes.Unlock, unlock.Id);
                state.Month.News.Add(new UnlockNews(unlock.Id));
                changed = true;
            }
        }
    });

    /// <summary>The closest not-yet-gained unlocks, for the "next unlocks" progress bars.</summary>
    public static List<UnlockProgress> NextUnlocks(GameState state, ContentPack content, int limit = 3)
    {
        var list = content.Unlocks
            .Where(u => !state.Unlocks.Unlocked.ContainsKey(u.Id))
            .Select(u => new UnlockProgress(u.Id, Requirements.Evaluate(u.Requirement, state, content).Progress))
            .ToList();
        // Stable sort (OrderBy) by progress descending, then id in ordinal order.
        return list
            .OrderBy(x => x, Comparer<UnlockProgress>.Create((a, b) =>
            {
                double d = b.Progress - a.Progress;
                if (d != 0 && !double.IsNaN(d)) return d < 0 ? -1 : 1;
                return string.CompareOrdinal(a.Id, b.Id);
            }))
            .Take(limit)
            .ToList();
    }

    private static bool IsRandomPending(PendingEvent p, ContentPack content) =>
        !(content.ById.Events.TryGetValue(p.EventId, out var ev) && ev.Trigger == "system");

    /// <summary>Draws this month's random events into The Post. System events (inquests) don't count.</summary>
    public static void DrawEvents(GameState state, ContentPack content, Rng rng)
    {
        int max = content.Config.MaxEventsPerMonth;
        foreach (var ev in content.Events)
        {
            if (ev.Trigger != "random" || ev.Condition is null) continue;
            if (state.Events.Pending.Count(p => IsRandomPending(p, content)) >= max) break;
            if (state.Events.Pending.Any(p => p.EventId == ev.Id)) continue;
            int times = state.Events.TimesDrawn.GetOr(ev.Id, 0);
            if (ev.Once && times > 0) continue;
            if (state.Events.LastDrawn.TryGetValue(ev.Id, out int last) && state.Meta.Turn - last < ev.CooldownMonths) continue;
            if (!Requirements.Evaluate(ev.Condition, state, content).Met) continue;
            if (!rng.Chance(ev.Chance)) continue;
            state.Events.Pending.Add(new PendingEvent
            {
                InstanceId = $"event_{state.Meta.NextId++}",
                EventId = ev.Id,
                DrawnTurn = state.Meta.Turn,
            });
            state.Events.LastDrawn[ev.Id] = state.Meta.Turn;
            state.Events.TimesDrawn[ev.Id] = times + 1;
        }
    }

    /// <summary>Whether a choice can be taken right now (its requirement, and anything it would cost).</summary>
    public static bool IsChoiceAvailable(GameState state, ContentPack content, EventChoice choice)
    {
        if (choice.Requires is not null && !Requirements.Evaluate(choice.Requires, state, content).Met) return false;
        foreach (var e in choice.Effects)
        {
            if (e is CashEffect c && c.DeltaCents < 0 && state.Player.CashCents < -c.DeltaCents) return false;
            if (e is InquestEffect { Approach: "bribe" } && state.Player.CashCents < Scrutiny.BribeCostCents(state, content))
                return false;
        }
        return true;
    }

    public static List<EventChoice> AvailableChoices(GameState state, ContentPack content, PendingEvent pending) =>
        content.ById.Events.TryGetValue(pending.EventId, out var ev)
            ? ev.Choices.Where(c => IsChoiceAvailable(state, content, c)).ToList()
            : new List<EventChoice>();

    /// <summary>Resolves a pending event with the given choice. Returns an error message on failure.</summary>
    public static string? ResolveEvent(GameState state, ContentPack content, Rng rng, string instanceId, string choiceId, bool auto)
    {
        int index = state.Events.Pending.FindIndex(p => p.InstanceId == instanceId);
        if (index < 0) return $"no pending event \"{instanceId}\"";
        var pending = state.Events.Pending[index];
        if (!content.ById.Events.TryGetValue(pending.EventId, out var ev)) return $"unknown event \"{pending.EventId}\"";
        var choice = ev.Choices.FirstOrDefault(c => c.Id == choiceId);
        if (choice is null) return $"event \"{ev.Id}\" has no choice \"{choiceId}\"";
        if (!IsChoiceAvailable(state, content, choice)) return $"choice \"{choiceId}\" is not available";
        state.Events.Pending.RemoveAt(index);
        foreach (var e in choice.Effects)
        {
            if (e is not InquestEffect inquest) continue;
            var error = Scrutiny.ResolveInquest(state, content, rng, pending.DeathId, inquest.Approach);
            if (error is not null)
            {
                state.Events.Pending.Insert(index, pending);
                return error;
            }
        }
        Effects.Apply(state, choice.Effects, Causes.Event, ev.Id);
        state.Month.News.Add(new EventResolvedNews(ev.Id, choice.Id, auto));
        return null;
    }

    /// <summary>Resolves every pending event with its default (or first available) choice.</summary>
    public static void AutoResolveEvents(GameState state, ContentPack content, Rng rng)
    {
        foreach (var pending in state.Events.Pending.ToList())
        {
            var choices = AvailableChoices(state, content, pending);
            var choice = choices.FirstOrDefault(c => c.Default) ?? choices.FirstOrDefault();
            var error = choice is not null
                ? ResolveEvent(state, content, rng, pending.InstanceId, choice.Id, true)
                : "no available choice";
            if (error is not null) state.Events.Pending = state.Events.Pending.Where(p => !ReferenceEquals(p, pending)).ToList();
        }
    }
}
