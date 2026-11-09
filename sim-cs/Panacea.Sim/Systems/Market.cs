namespace Panacea.Sim;

/// <summary>A formula on the shelf, judged by the lot a buyer would be sold (the oldest).</summary>
public sealed record Offer(Formula Formula, FormulaAnalysis Analysis);

public sealed record Buyer(BuyerProfile Profile, string Ailment, DrugValues Tolerance, double Dependency);

/// <summary>The shop's market (systems/market.ts).</summary>
public static class Market
{
    /// <summary>What is on the shelf.</summary>
    public static List<Offer> CurrentOffers(GameState state, ContentPack content)
    {
        var offers = new List<Offer>();
        foreach (var f in state.Lab.Formulas)
        {
            var lot = state.Lab.Stock.Find(l => l.FormulaId == f.Id && l.Bottles > 0);
            if (lot is not null) offers.Add(new Offer(f, Formulas.Analyze(lot.Components, lot.ProcessId, content)));
        }
        return offers;
    }

    /// <summary>
    /// The most a buyer would pay for a bottle of this appeal. A stronger-feeling bottle commands a
    /// premium (up to WillingnessMax × their usual price), and dependency raises it further.
    /// </summary>
    public static double WillingnessToPay(Buyer buyer, double appealScore, ContentPack content)
    {
        var m = content.Config.Market;
        return buyer.Profile.BaseWillingnessCents *
            Math.Min(m.WillingnessMax, Math.Max(0, m.WillingnessBase + appealScore / m.AppealPerWillingness)) *
            (1 + buyer.Dependency);
    }

    /// <summary>The best value-for-money bottle this buyer would accept, if any.</summary>
    public static Offer? ChooseOffer(GameState state, IReadOnlyList<Offer> offers, Buyer buyer, ContentPack content,
        double maxPriceCents = double.PositiveInfinity)
    {
        Offer? best = null;
        double bestScore = double.NegativeInfinity;
        foreach (var o in offers)
        {
            if (o.Formula.PriceCents > maxPriceCents || Lab.StockOf(state, o.Formula.Id) <= 0) continue;
            double a = Pharmacology.Appeal(o.Analysis, buyer.Tolerance, buyer.Profile, buyer.Dependency,
                Formulas.ClaimAppeal(o.Formula.Claims, buyer.Ailment, content));
            if (a <= 0 || o.Formula.PriceCents > WillingnessToPay(buyer, a, content)) continue;
            double score = a / Math.Max(1, o.Formula.PriceCents);
            if (score > bestScore)
            {
                best = o;
                bestScore = score;
            }
        }
        return best;
    }

    /// <summary>
    /// Whether a buyer who left empty-handed was put off by the price: some bottle on the shelf
    /// appealed to them, but every one that did cost more than they would pay. (Call it only when
    /// ChooseOffer with no price cap found nothing.)
    /// </summary>
    public static bool TurnedAwayByPrice(GameState state, IReadOnlyList<Offer> offers, Buyer buyer, ContentPack content)
    {
        foreach (var o in offers)
        {
            if (Lab.StockOf(state, o.Formula.Id) <= 0) continue;
            double a = Pharmacology.Appeal(o.Analysis, buyer.Tolerance, buyer.Profile, buyer.Dependency,
                Formulas.ClaimAppeal(o.Formula.Claims, buyer.Ailment, content));
            if (a > 0) return true;
        }
        return false;
    }

    /// <summary>
    /// How much a price near a stranger's limit puts them off: 1 when the bottle costs at most
    /// (1 − HesitationMargin) of what they would pay, falling to 1 − PriceHesitation at their limit.
    /// </summary>
    public static double PriceHesitation(double priceCents, double wtpCents, ContentPack content)
    {
        var m = content.Config.Market;
        if (m.PriceHesitation <= 0 || wtpCents <= 0) return 1;
        double headroom = 1 - priceCents / wtpCents;
        return 1 - m.PriceHesitation * Math.Max(0, Math.Min(1, 1 - headroom / m.HesitationMargin));
    }

    /// <summary>
    /// Whether the ward has households of this kind left to win (market.ts wardHasRoomFor, balance
    /// pass 8). The ward is so many households of each kind, in the proportions of the segments'
    /// weights; a shop can be the regular shop of at most WardShareSlack × its capacity × that kind's
    /// share of them: a ward has only so many clerks, and fewer carriage folk.
    /// </summary>
    public static bool WardHasRoomFor(GameState state, ContentPack content, string segmentId, int capacity)
    {
        double slack = content.Config.WalkIns.WardShareSlack;
        if (slack <= 0) return true;
        double total = 0;
        foreach (var seg in content.Segments) total = total + seg.Weight;
        double share = (content.ById.Segments.TryGetValue(segmentId, out var s) ? s.Weight : 0) / total;
        int have = 0;
        foreach (var c in state.Customers) if (c.Segment == segmentId) have += 1;
        return have < slack * capacity * share;
    }

    /// <summary>The habit a bottle's narcotics make: its opiates and its cocaine, not its spirits.</summary>
    public static double NarcoticHabit(FormulaAnalysis a) =>
        a.ByClass[DrugClasses.Opiate].Habit + a.ByClass[DrugClasses.Cocaine].Habit;

    /// <summary>
    /// The chance a regular buys this month at the shelf price rather than make the last bottle do
    /// (market.ts dearness, balance pass 8): 1 up to DearAbove × their usual price, falling as
    /// (comfortable / price) ^ DearElasticity above it. A bottle whose narcotics make a habit of
    /// DearHabitHold or more is found the money for at up to twice the comfortable price.
    /// </summary>
    public static double Dearness(Customer c, Offer offer, ContentPack content)
    {
        var m = content.Config.Market;
        double hold = Math.Min(1, NarcoticHabit(offer.Analysis) / m.DearHabitHold);
        double comfortable = c.Profile.BaseWillingnessCents * m.DearAbove * (1 + hold);
        double price = offer.Formula.PriceCents;
        if (price <= comfortable || m.DearElasticity <= 0) return 1;
        return DMath.Exp(m.DearElasticity * DMath.Log(comfortable / price));
    }

    /// <summary>
    /// What a regular can afford to spend in a month, in cents. A habit eats into it: a craving
    /// regular loses work and wages to the bottle, so the more dependent they are, the less they have.
    /// </summary>
    public static double MeansCents(Customer c, ContentPack content) =>
        c.Profile.BaseWillingnessCents * content.Config.Market.MeansPerMonth *
        (1 - content.Config.Market.MeansLostToHabit * c.Body.Dependency);

    /// <summary>
    /// A regular who owes more than RuinMonths months' means: one bottle at most, and a family that
    /// complains. A habit can run up a long slate before it comes to that.
    /// </summary>
    public static bool IsRuined(Customer c, ContentPack content) =>
        c.DebtCents > content.Config.Market.RuinMonths * MeansCents(c, content);

    private static double TotalTolerance(Customer c)
    {
        double sum = 0;
        foreach (var cls in DrugClasses.All) sum = sum + c.Body.Tolerance[cls];
        return sum;
    }

    /// <summary>
    /// The bottles a regular wants this month: one, and as many more as their attachment calls for.
    /// Satisfaction alone buys a bottle for the cupboard; a craving buys more, and more again as the
    /// dose stops working (tolerance), up to HabitBottlesMax extra.
    /// </summary>
    public static int BottlesWanted(Customer c, ContentPack content)
    {
        if (IsRuined(c, content)) return 1;
        var m = content.Config.Market;
        double dep = c.Body.Dependency;
        double tolerance = TotalTolerance(c);
        double craving = dep * (m.BottlesPerAttachment + m.BottlesPerTolerance * Math.Max(0, tolerance - m.ToleranceFloor));
        double liking = Math.Max(dep, c.Satisfaction) * m.BottlesPerAttachment;
        return 1 + (int)Math.Floor(Math.Min(m.HabitBottlesMax, Math.Max(liking, craving)));
    }

    /// <summary>
    /// What a regular who craves the bottle pays over its shelf price: a habit pays what it is asked,
    /// and something over to have a bottle kept back. Only for a bottle strong enough to make a habit
    /// (habit past PremiumHabitFloor, the whole premium at PremiumHabitSpan beyond it), and only while
    /// the regular still has the money (they owe less than PremiumDebtMonths months' means). Up to
    /// CravingPremium × dependency of the price.
    /// </summary>
    public static long CravingPremiumCents(Customer c, Offer offer, ContentPack content)
    {
        var m = content.Config.Market;
        if (c.Body.Dependency <= content.Config.Body.WithdrawalThreshold) return 0;
        if (c.DebtCents > m.PremiumDebtMonths * MeansCents(c, content)) return 0;
        double habit = offer.Analysis.Properties.Habit;
        double strength = Math.Min(1, Math.Max(0, (habit - m.PremiumHabitFloor) / m.PremiumHabitSpan));
        return (long)JsMath.Round(offer.Formula.PriceCents * m.CravingPremium * c.Body.Dependency * strength);
    }

    /// <summary>Adds one bottle sold to the month's sales of its formula, under where it went.</summary>
    public static void RecordSale(GameState state, string formulaId, long priceCents, SaleSource source, long premiumCents = 0)
    {
        priceCents += premiumCents;
        state.Stats.BottlesSold += 1;
        state.Stats.RevenueCents += priceCents;
        var sale = state.Month.Sales.Find(s => s.FormulaId == formulaId);
        if (sale is null)
        {
            sale = new SaleRecord { FormulaId = formulaId };
            state.Month.Sales.Add(sale);
        }
        sale.Bottles += 1;
        sale.RevenueCents += priceCents;
        switch (source)
        {
            case SaleSource.Shop: sale.ShopBottles += 1; sale.ShopRevenueCents += priceCents; break;
            case SaleSource.WalkIn: sale.WalkInBottles += 1; sale.WalkInRevenueCents += priceCents; break;
            case SaleSource.Wholesale: sale.WholesaleBottles += 1; sale.WholesaleRevenueCents += priceCents; break;
            case SaleSource.Post: sale.PostBottles += 1; sale.PostRevenueCents += priceCents; break;
        }
        sale.CravingPremiumCents += premiumCents;
    }

    /// <summary>
    /// Callers drawn by word of a strong bottle ("word"): the most habit-forming bottle on the shelf
    /// is talked about, and strangers come asking for the stuff that works.
    /// </summary>
    public static int WordOfStrength(IReadOnlyList<Offer> offers, ContentPack content)
    {
        var w = content.Config.WalkIns;
        double habit = 0;
        foreach (var o in offers) habit = Math.Max(habit, o.Analysis.Properties.Habit);
        return (int)Math.Floor(w.WordPerHabit * Math.Min(w.WordHabitCap, Math.Max(0, habit - w.WordHabitFloor)));
    }

    private static Buyer CustomerAsBuyer(Customer c) => new(c.Profile, c.Ailment, c.Body.Tolerance, c.Body.Dependency);

    private static double MaxBoldness(IEnumerable<Formula> formulas, ContentPack content)
    {
        double max = 0;
        foreach (var f in formulas) max = Math.Max(max, Formulas.ClaimBoldness(f.Claims, content));
        return max;
    }

    /// <summary>Turn phase: claims on labels draw notice; newspaper advertising amplifies them.</summary>
    public static readonly Phase AdvertisingPhase = new("advertising", ctx =>
    {
        var state = ctx.State;
        var content = ctx.Content;
        var adv = content.Config.Advertising;
        var onSale = state.Lab.Formulas.Where(f => Lab.StockOf(state, f.Id) > 0).ToList();
        double labelBoldness = MaxBoldness(onSale, content);
        if (labelBoldness > 0)
            Ledger.Adjust(state, Metrics.Heat, labelBoldness * adv.LabelHeatPerBoldness, Causes.Claims);
        // An almanac prints every claim in the formula book, for a year, on every kitchen table.
        double printedBoldness = MaxBoldness(state.Lab.Formulas, content);
        foreach (var inv in Investments.Held(state, content))
        {
            if (inv.HeatPerBoldness > 0 && printedBoldness > 0)
                Ledger.Adjust(state, Metrics.Heat, printedBoldness * inv.HeatPerBoldness, Causes.Claims, inv.Id);
        }

        int spend = state.Marketing.AdSpendCents;
        if (spend <= 0) return;
        if (spend > state.Player.CashCents)
        {
            state.Marketing.AdSpendCents = 0;
            state.Month.News.Add(new AdsLapsedNews());
            return;
        }
        Ledger.Adjust(state, Metrics.Cash, -spend, Causes.Advertising);
        ctx.Scratch.AdVisitors = (int)Math.Floor(adv.VisitorsPerSqrtCent * Math.Sqrt(spend));
        double boldness = MaxBoldness(state.Lab.Formulas, content);
        if (boldness > 0) Ledger.Adjust(state, Metrics.Heat, boldness * adv.HeatPerBoldness, Causes.Claims);
    });

    private sealed class Revenue
    {
        public required string Cause;
        public required string FormulaId;
        public long Cents;
    }

    /// <summary>Turn phase: regulars and walk-ins buy.</summary>
    public static readonly Phase SalesPhase = new("sales", ctx =>
    {
        var state = ctx.State;
        var content = ctx.Content;
        var rng = ctx.Rng;
        var scratch = ctx.Scratch;
        var offers = CurrentOffers(state, content);
        // Insertion-ordered, like the TS Map: revenue is booked in first-sale order.
        var revenue = new OrderedMap<Revenue>();

        void Book(string cause, string formulaId, long cents)
        {
            string key = $"{cause}|{formulaId}";
            var r = revenue.GetOr(key, null!) ?? new Revenue { Cause = cause, FormulaId = formulaId, Cents = 0 };
            r.Cents += cents;
            revenue[key] = r;
        }

        Bought? Sell(Offer offer, string cause, long premium)
        {
            var lot = Lab.TakeBottle(state, offer.Formula.Id);
            if (lot is null) return null;
            int price = offer.Formula.PriceCents;
            RecordSale(state, offer.Formula.Id, price, cause == Causes.SalesShop ? SaleSource.Shop : SaleSource.WalkIn, premium);
            Book(cause, offer.Formula.Id, price);
            if (premium > 0) Book(Causes.SalesCraving, offer.Formula.Id, premium);
            return new Bought(offer.Formula.Id, lot.Id, lot.Components, lot.ProcessId, lot.Potency, lot.BadBatch);
        }

        // Regulars, in a fixed order for determinism. A regular's attachment to the shop is the
        // stronger of habit and plain satisfaction; every regular who comes in buys one bottle they
        // think worth its price, and the budget limits the extra ones. A habit pays what it is asked
        // until the money runs out: spending beyond their means runs up a debt, and a regular who
        // is ruined buys one bottle at most.
        var market = content.Config.Market;
        foreach (var c in state.Customers)
        {
            var bought = new List<Bought>();
            double dep = c.Body.Dependency;
            double pBuy = Math.Min(0.98, Math.Max(0.05, c.Profile.BuyChance + 0.3 * c.Satisfaction + 0.5 * dep));
            double spent = 0;
            if (offers.Count > 0 && rng.Chance(pBuy))
            {
                double attachment = Math.Max(dep, c.Satisfaction);
                int desired = BottlesWanted(c, content);
                double budget = c.Profile.BaseWillingnessCents *
                    (market.BudgetBase + market.BudgetPerAttachment * attachment) *
                    (1 + dep);
                for (int k = 0; k < desired; k++)
                {
                    var offer = ChooseOffer(state, offers, CustomerAsBuyer(c), content,
                        k == 0 ? double.PositiveInfinity : budget);
                    if (offer is null)
                    {
                        if (k == 0 && TurnedAwayByPrice(state, offers, CustomerAsBuyer(c), content))
                            state.Month.TurnedAwayByPrice += 1;
                        break;
                    }
                    // A dear bottle: the regular asks the price and makes the last one do this month.
                    if (k == 0)
                    {
                        double keep = Dearness(c, offer, content);
                        if (keep < 1 && !rng.Chance(keep))
                        {
                            state.Month.TurnedAwayByPrice += 1;
                            break;
                        }
                    }
                    long premium = CravingPremiumCents(c, offer, content);
                    var b = Sell(offer, Causes.SalesShop, premium);
                    if (b is null) break;
                    bought.Add(b);
                    budget -= offer.Formula.PriceCents + premium;
                    spent += offer.Formula.PriceCents + premium;
                }
            }
            c.DebtCents = Math.Max(0, c.DebtCents + spent - MeansCents(c, content));
            scratch.Purchases[c.Id] = bought;
            c.BottlesLastMonth = bought.Count;
            if (bought.Count > 0)
            {
                c.Purchases += bought.Count;
                c.LastPurchaseTurn = state.Meta.Turn;
                c.LastFormulaId = bought[^1].FormulaId;
            }
        }

        // Orders from away are filled next, from what the regulars left, up to a share of it:
        // strangers at the counter get the rest, so a contract held on a thin shelf costs the shop
        // new custom.
        OrdersFromAway(ctx);

        WalkIns(ctx, offers, (o, cause) => Sell(o, cause, 0));

        foreach (var kv in revenue)
            Ledger.Adjust(state, Metrics.Cash, kv.Value.Cents, kv.Value.Cause, kv.Value.FormulaId);
    });

    /// <summary>The bottle orders from away are filled with: the one the shop has most of on the shelf.</summary>
    public static Formula? OrderFormula(GameState state)
    {
        Formula? formula = null;
        int most = 0;
        foreach (var f in state.Lab.Formulas)
        {
            int n = Lab.StockOf(state, f.Id);
            if (n > most)
            {
                formula = f;
                most = n;
            }
        }
        return formula;
    }

    private sealed class Allowance
    {
        public int Left;
    }

    /// <summary>
    /// Orders from away (market.ts ordersFromAway), filled after the regulars have had theirs and
    /// before the walk-ins, from at most OrdersShelfShare of what the regulars left: druggists out
    /// of town under contract, at the wholesale price, and readers who send by post, at the shelf
    /// price. All take the bottle the shop has most of, and order fewer the dearer it is. They sell
    /// to (or are) strangers with no tolerance: a strong enough bottle kills some of them.
    /// </summary>
    private static void OrdersFromAway(TurnContext ctx)
    {
        var state = ctx.State;
        var content = ctx.Content;
        var allowance = new Allowance { Left = (int)Math.Ceiling(Lab.TotalStock(state) * content.Config.Market.OrdersShelfShare) };
        foreach (var inv in Investments.Held(state, content))
        {
            if (inv.Wholesale is { } w) Deliver(ctx, inv.Id, w, state.Month.Wholesale, Causes.SalesWholesale, allowance);
            if (inv.PostOrders is { } p) Deliver(ctx, inv.Id, p, state.Month.Post, Causes.SalesPost, allowance);
        }
    }

    private static void Deliver(TurnContext ctx, string investmentId, WholesaleSpec spec, WholesaleMonth month,
        string cause, Allowance allowance)
    {
        var state = ctx.State;
        var content = ctx.Content;
        var formula = OrderFormula(state);
        int ordered = Investments.WholesaleOrder(state, spec, formula?.PriceCents);
        month.Ordered += ordered;
        if (formula is null || ordered <= 0) return;
        int price = Investments.WholesalePrice(spec, formula.PriceCents);
        var risk = new Dictionary<string, (double P, double Detectability)>(StringComparer.Ordinal);
        long revenue = 0;
        for (int i = 0; i < ordered; i++)
        {
            if (allowance.Left <= 0) break;
            var lot = Lab.TakeBottle(state, formula.Id);
            if (lot is null) break;
            allowance.Left -= 1;
            month.Delivered += 1;
            revenue += price;
            RecordSale(state, formula.Id, price, cause == Causes.SalesPost ? SaleSource.Post : SaleSource.Wholesale);
            // A stranger of ordinary health, with no tolerance.
            if (!risk.TryGetValue(lot.Id, out var r))
            {
                var a = Formulas.Analyze(lot.Components, lot.ProcessId, content);
                var dose = new DoseContext(lot.Potency, 1, 1);
                double danger = Pharmacology.AcuteDanger(a, Pharmacology.ZeroTolerance(), dose, content);
                r = (Pharmacology.OverdoseChance(danger, spec.StrangerHealth), a.Properties.Detectability);
                risk[lot.Id] = r;
            }
            if (r.P > 0 && ctx.Rng.Chance(r.P))
            {
                var segment = CustomersSystem.PickSegment(content, ctx.Rng);
                Scrutiny.RecordDeath(state, content, ctx.Rng, new DeathInput
                {
                    CustomerId = null,
                    Name = "",
                    Segment = segment.Id,
                    Consumer = Consumers.Self,
                    Anonymous = true,
                    Ailment = null,
                    Cause = DeathCauses.Overdose,
                    ProductImplicated = true,
                    FormulaId = null,
                    BadBatch = lot.BadBatch,
                    Detectability = r.Detectability,
                });
            }
        }
        month.RevenueCents += revenue;
        if (revenue > 0) Ledger.Adjust(state, Metrics.Cash, revenue, cause, investmentId);
    }

    private const string CallersPrefix = "callers_";

    /// <summary>
    /// Callers a letter's arrangement sends to the counter (market.ts letterCallers): a capability
    /// "callers_&lt;source&gt;" (the wheelmen's club, the parish bazaar) brings that many strangers a
    /// month, counted under that source, in the ordinal order of the capability ids. They come on
    /// someone's word, scaled by <see cref="LetterTrust"/> and rounded.
    /// </summary>
    public static List<(string Source, int Callers)> LetterCallers(GameState state, ContentPack content)
    {
        var caps = state.Player.Capabilities;
        double trust = LetterTrust(state, content);
        var ids = caps.Keys.Where(k => k.StartsWith(CallersPrefix, StringComparison.Ordinal)).ToList();
        ids.Sort(string.CompareOrdinal);
        var list = new List<(string, int)>();
        foreach (var id in ids)
        {
            int n = caps.GetOr(id, 0);
            int sent = (int)Math.Floor(n * trust + 0.5);
            if (sent > 0) list.Add((id[CallersPrefix.Length..], sent));
        }
        return list;
    }

    /// <summary>
    /// How far the ward takes the shop at a letter's word, 0..1 (market.ts letterTrust): how readily
    /// a correspondent sends people to it, and how much standing a kindness earns. Lessened by the
    /// ward's memory of harm (Ward.LetterCallerLoss at a memory of 100) and by suspicion
    /// (Scrutiny.LetterCallerLoss at the heat that shuts the shop).
    /// </summary>
    public static double LetterTrust(GameState state, ContentPack content)
    {
        var scrutiny = content.Config.Scrutiny;
        double suspicion = Math.Min(1, Math.Max(0, state.Scrutiny.Heat / scrutiny.ShutdownHeat));
        return WardSystem.MemoryLoss(state, content.Config.Ward.LetterCallerLoss) * Math.Max(0, 1 - scrutiny.LetterCallerLoss * suspicion);
    }

    private static void WalkIns(TurnContext ctx, List<Offer> offers, Func<Offer, string, Bought?> sell)
    {
        var state = ctx.State;
        var content = ctx.Content;
        var rng = ctx.Rng;
        var scratch = ctx.Scratch;
        if (offers.Count == 0) return;
        var cfg = content.Config.WalkIns;
        var caps = state.Player.Capabilities;
        var callers = state.Month.Callers;
        callers["ward"] = rng.Int(0, cfg.Base + (int)Math.Floor(state.Player.Reputation * cfg.PerReputation));
        callers["newspaper"] = scratch.AdVisitors;
        callers["subscriptions"] = Math.Max(0, caps.GetOr("walk_in_bonus", 0));
        callers["word"] = WordOfStrength(offers, content);
        foreach (var inv in Investments.Held(state, content))
            if (inv.WalkInsPerMonth > 0) callers[inv.Id] = inv.WalkInsPerMonth;
        foreach (var (source, n) in LetterCallers(state, content)) callers[source] = n;
        int visitors = 0;
        foreach (var kv in callers) visitors += kv.Value;
        // A rival's sale: a share of the callers whose bottle costs more than his go to him instead.
        double rivalShare = (double)Math.Min(100, caps.GetOr("rival_share_pct", 0)) / 100;
        int rivalPrice = caps.GetOr("rival_price_cents", 0);
        int capacity = CustomersSystem.CustomerCapacity(state, content);
        for (int i = 0; i < visitors; i++)
        {
            var segment = CustomersSystem.PickSegment(content, rng);
            string ailmentId = CustomersSystem.PickAilment(segment, rng);
            var buyer = new Buyer(segment.Profile, ailmentId, Pharmacology.ZeroTolerance(), 0);
            var offer = ChooseOffer(state, offers, buyer, content);
            if (offer is null)
            {
                if (TurnedAwayByPrice(state, offers, buyer, content)) state.Month.TurnedAwayByPrice += 1;
                continue;
            }
            if (rivalShare > 0 && offer.Formula.PriceCents > rivalPrice && rng.Chance(rivalShare))
            {
                state.Month.TurnedAwayByPrice += 1;
                continue;
            }
            double claims = Formulas.ClaimAppeal(offer.Formula.Claims, ailmentId, content);
            // A stranger hesitates over a bottle priced near the most they would pay.
            double wtp = WillingnessToPay(buyer,
                Pharmacology.Appeal(offer.Analysis, buyer.Tolerance, buyer.Profile, 0, claims), content);
            double hesitation = PriceHesitation(offer.Formula.PriceCents, wtp, content);
            // A stranger who has heard what the ward says of the bottle thinks twice.
            double pBuy = Math.Min(0.95, segment.Profile.BuyChance * (1 + 0.1 * claims)) * hesitation *
                WardSystem.MemoryLoss(state, content.Config.Ward.WalkInLoss);
            if (!rng.Chance(pBuy)) continue;
            var bought = sell(offer, Causes.SalesWalkIn);
            if (bought is null) continue;

            // A walk-in who felt relief may come back as a regular.
            content.ById.Ailments.TryGetValue(ailmentId, out var ailment);
            double relief = Pharmacology.ReliefScore(offer.Analysis, buyer.Tolerance, buyer.Profile) - (ailment?.Severity ?? 0);
            double reputationFactor = 0.5 + state.Player.Reputation / 50;
            double pConvert = cfg.ConversionChance * (1 + 0.2 * claims) * reputationFactor *
                WardSystem.MemoryLoss(state, content.Config.Ward.ConversionLoss);
            if (relief > 0 && state.Customers.Count < capacity &&
                WardHasRoomFor(state, content, segment.Id, capacity) && rng.Chance(Math.Min(0.9, pConvert)))
            {
                var customer = CustomersSystem.GenerateCustomer(state, content, rng, segment, ailmentId);
                state.Customers.Add(customer);
                state.Stats.CustomersJoined += 1;
                scratch.Purchases[customer.Id] = [bought];
                customer.Purchases = 1;
                customer.BottlesLastMonth = 1;
                customer.LastPurchaseTurn = state.Meta.Turn;
                customer.LastFormulaId = bought.FormulaId;
                state.Month.News.Add(new CustomerJoinedNews(customer.Id));
            }
            else
            {
                scratch.WalkIns.Add((segment.Id, ailmentId, bought));
            }
        }
    }

    /// <summary>Turn phase: rent, and any retainers the shop has agreed to pay.</summary>
    public static readonly Phase ExpensesPhase = new("expenses", ctx =>
    {
        long rent = ctx.Content.Config.RentCents + ctx.State.Player.Capabilities.GetOr("rent_extra_cents", 0);
        Ledger.Adjust(ctx.State, Metrics.Cash, -rent, Causes.Rent);
        int retainer = ctx.State.Player.Capabilities.GetOr("retainer_cents", 0);
        if (retainer > 0) Ledger.Adjust(ctx.State, Metrics.Cash, -retainer, Causes.Retainer);
    });
}
