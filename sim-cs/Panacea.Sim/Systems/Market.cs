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
    /// What a regular can afford to spend in a month, in cents. A habit eats into it: a craving
    /// regular loses work and wages to the bottle, so the more dependent they are, the less they have.
    /// </summary>
    public static double MeansCents(Customer c, ContentPack content) =>
        c.Profile.BaseWillingnessCents * content.Config.Market.MeansPerMonth *
        (1 - content.Config.Market.MeansLostToHabit * c.Body.Dependency);

    /// <summary>A regular who owes more than a month's means: one bottle at most, and a family that complains.</summary>
    public static bool IsRuined(Customer c, ContentPack content) => c.DebtCents > MeansCents(c, content);

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

        Bought? Sell(Offer offer, string cause)
        {
            var lot = Lab.TakeBottle(state, offer.Formula.Id);
            if (lot is null) return null;
            int price = offer.Formula.PriceCents;
            state.Stats.BottlesSold += 1;
            state.Stats.RevenueCents += price;
            string key = $"{cause}|{offer.Formula.Id}";
            var r = revenue.GetOr(key, null!) ?? new Revenue { Cause = cause, FormulaId = offer.Formula.Id, Cents = 0 };
            r.Cents += price;
            revenue[key] = r;
            var sale = state.Month.Sales.Find(s => s.FormulaId == offer.Formula.Id);
            if (sale is not null)
            {
                sale.Bottles += 1;
                sale.RevenueCents += price;
            }
            else
            {
                state.Month.Sales.Add(new SaleRecord { FormulaId = offer.Formula.Id, Bottles = 1, RevenueCents = price });
            }
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
                int desired = IsRuined(c, content) ? 1 : 1 + (int)Math.Floor(attachment * market.BottlesPerAttachment);
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
                    var b = Sell(offer, Causes.SalesShop);
                    if (b is null) break;
                    bought.Add(b);
                    budget -= offer.Formula.PriceCents;
                    spent += offer.Formula.PriceCents;
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

        WalkIns(ctx, offers, Sell);

        foreach (var kv in revenue)
            Ledger.Adjust(state, Metrics.Cash, kv.Value.Cents, kv.Value.Cause, kv.Value.FormulaId);

        Wholesale(ctx);
    });

    /// <summary>
    /// Druggists out of town under contract take their order from the shelf at month's end, at the
    /// wholesale price, of the bottle the shop has most of. They sell to strangers with no
    /// tolerance: a strong enough bottle kills some of them, and the talk comes home.
    /// </summary>
    private static void Wholesale(TurnContext ctx)
    {
        var state = ctx.State;
        var content = ctx.Content;
        foreach (var inv in Investments.Held(state, content))
        {
            if (inv.Wholesale is not { } spec) continue;
            int ordered = Investments.WholesaleOrder(state, inv);
            state.Month.Wholesale.Ordered += ordered;
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
            if (formula is null || ordered <= 0) continue;
            int price = Investments.WholesalePrice(inv, formula.PriceCents);
            var risk = new Dictionary<string, (double P, double Detectability)>(StringComparer.Ordinal);
            long revenue = 0;
            for (int i = 0; i < ordered; i++)
            {
                var lot = Lab.TakeBottle(state, formula.Id);
                if (lot is null) break;
                state.Month.Wholesale.Delivered += 1;
                state.Stats.BottlesSold += 1;
                state.Stats.RevenueCents += price;
                revenue += price;
                var sale = state.Month.Sales.Find(s => s.FormulaId == formula.Id);
                if (sale is not null)
                {
                    sale.Bottles += 1;
                    sale.RevenueCents += price;
                }
                else
                {
                    state.Month.Sales.Add(new SaleRecord { FormulaId = formula.Id, Bottles = 1, RevenueCents = price });
                }
                // A stranger in Paterson, of ordinary health, with no tolerance.
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
                        Cause = DeathCauses.Overdose,
                        ProductImplicated = true,
                        FormulaId = null,
                        BadBatch = lot.BadBatch,
                        Detectability = r.Detectability,
                    });
                }
            }
            state.Month.Wholesale.RevenueCents += revenue;
            if (revenue > 0) Ledger.Adjust(state, Metrics.Cash, revenue, Causes.SalesWholesale, inv.Id);
        }
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
        foreach (var inv in Investments.Held(state, content))
            if (inv.WalkInsPerMonth > 0) callers[inv.Id] = inv.WalkInsPerMonth;
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
            double pBuy = Math.Min(0.95, segment.Profile.BuyChance * (1 + 0.1 * claims)) * hesitation;
            if (!rng.Chance(pBuy)) continue;
            var bought = sell(offer, Causes.SalesWalkIn);
            if (bought is null) continue;

            // A walk-in who felt relief may come back as a regular.
            content.ById.Ailments.TryGetValue(ailmentId, out var ailment);
            double relief = Pharmacology.ReliefScore(offer.Analysis, buyer.Tolerance, buyer.Profile) - (ailment?.Severity ?? 0);
            double reputationFactor = 0.5 + state.Player.Reputation / 50;
            double pConvert = cfg.ConversionChance * (1 + 0.2 * claims) * reputationFactor;
            if (relief > 0 && state.Customers.Count < capacity && rng.Chance(Math.Min(0.9, pConvert)))
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
