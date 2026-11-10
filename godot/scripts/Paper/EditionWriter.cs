using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Panacea.Paper;

/// <summary>
/// Writes an edition of the paper from <see cref="PaperFacts"/> and the copy in
/// data/paper.en.json. Deterministic: the same facts always produce the same paper (variant
/// choices are hashed from the seed, turn and item), and it never touches the simulation's RNG.
/// </summary>
public sealed partial class EditionWriter
{
    private readonly JsonElement _copy;

    public EditionWriter(string paperJson)
    {
        _copy = JsonDocument.Parse(paperJson).RootElement.Clone();
    }

    public Edition Write(PaperFacts f)
    {
        var e = new Edition();
        var mast = _copy.GetProperty("masthead");
        e.Masthead = S(mast, "title");
        e.Over = S(mast, "over");
        e.Motto = S(mast, "motto");
        // the weather box speaks of coffins only as THE MARKETS prices them, and of the forecast
        // only as the Signal Service or the Bureau of the year would
        var coffinMove = CoffinMove(f);
        Func<string, bool> earCond = c => c switch
        {
            "coffins_up" => coffinMove == "up",
            "coffins_down" => coffinMove == "down",
            "coffins_steady" => coffinMove == "steady",
            "deaths" => f.Deaths.Count > 0,
            _ => RoutineCond(f)(c),
        };
        e.EarLeft = Pick(f, "weather", _copy.GetProperty("weather").GetProperty(f.Month.ToString(CultureInfo.InvariantCulture)), earCond);
        e.EarRightTitle = S(mast, "price");
        e.EarRight = S(mast, "priceNote");
        e.DatelineRight = S(mast, "yearly");
        // the paper changes with the times: half-tones, the penny war with the World and the
        // Journal, and at the last the reformers
        foreach (var era in _copy.GetProperty("eras").EnumerateArray())
        {
            if (era.GetProperty("from").GetInt32() > f.Year) continue;
            if (era.TryGetProperty("over", out var o)) e.Over = o.GetString() ?? e.Over;
            if (era.TryGetProperty("motto", out var mo)) e.Motto = mo.GetString() ?? e.Motto;
            if (era.TryGetProperty("price", out var pr)) e.EarRightTitle = pr.GetString() ?? e.EarRightTitle;
            if (era.TryGetProperty("priceNote", out var pn)) e.EarRight = pn.GetString() ?? e.EarRight;
            if (era.TryGetProperty("yearly", out var y)) e.DatelineRight = y.GetString() ?? e.DatelineRight;
        }

        var date = f.FirstEdition ? FirstSaturday(f.Year, f.Month) : PublicationDate(f.Year, f.Month);
        e.DatelineLeft = $"VOL. {Roman(f.Year - 1871 + 1)}.—NO. {600 + f.Turn * 4}.";
        e.DatelineCentre = $"NEW-YORK, {date.DayOfWeek.ToString().ToUpperInvariant()}, {date.ToString("MMMM", CultureInfo.InvariantCulture).ToUpperInvariant()} {date.Day}, {date.Year}.";
        e.Brief = Brief(f);
        e.BriefClip = f.FirstEdition ? null : BooksClip(f);

        var product = f.Products.FirstOrDefault();
        var productName = product?.Name ?? "Dr. ——'s remedies";
        var sec = _copy.GetProperty("sections");

        // ---- the lead: your advertisement dressed as news, or the news itself ----
        e.Lead = WriteLead(f, product);
        // the last edition looks back over the whole career, under the lead
        if (f.Ended && f.Career is { } career) e.LeadFoot.Add(CareerReview(f, career, product?.Name ?? "Dr. ——'s remedies"));
        // standing matter, for the make-up to balance the columns with. The make-up sets at most
        // twelve paragraphs (EditionView.Balance), so the writer hands it twelve and counts them all
        // as printed: the memory then lives in the writer alone, and a saved game replayed through
        // it comes back with the same memory. Prose items (the jokes) are taken the longest
        // unprinted first, among those of the year and the season; around them the routine news
        // of the day (ships in, the forecast, the police court, the ball game, the week's deaths,
        // a fire), made fresh each edition from the year's ships, clubs and courts. An item of
        // the day (the Statue, the General's funeral) runs once, in the first edition of its
        // window that this game reaches, ahead of everything else.
        var nowIdx = f.Year * 12 + f.Month - 1;
        var startIdx = nowIdx - Math.Max(0, f.Turn);
        static int Idx(string t) => int.Parse(t[..4]) * 12 + int.Parse(t[5..7]) - 1;
        var dated = _copy.GetProperty("standingDated").EnumerateArray()
            .Where(x =>
            {
                var a = Idx(x.GetProperty("from").GetString() ?? "0000-01");
                var b = Idx(x.GetProperty("to").GetString() ?? "9999-12");
                return Math.Max(a, startIdx) == nowIdx && nowIdx <= b;
            })
            .Select(x => x.GetProperty("text").GetString() ?? "").ToList();
        if (!_standingFor.TryGetValue(f.Turn, out var picked))
        {
            picked = Standing(f, Math.Max(0, StandingSlots - dated.Count));
            _standingFor[f.Turn] = picked;
        }
        var turn = f.Turn;
        e.Standing = dated.Concat(picked).ToList();
        var keys = e.Standing.ToList();
        // (already counted when picked: the page's report changes nothing the writer relies on)
        e.Printed = i => { if (i >= 0 && i < keys.Count && _proseStanding.Contains(keys[i])) _printed[keys[i]] = Math.Max(turn, _printed.GetValueOrDefault(keys[i], int.MinValue)); };

        // ---- left column: the consequences ----
        if (f.Deaths.Count > 0)
        {
            var deaths = new Section { Title = S(sec, "deaths") };
            foreach (var d in f.Deaths)
            {
                var key = d.OutOfTown ? "outOfTown" : d.Anonymous ? "deathAnonymous" : d.Infant ? "deathInfant" : "deathSelf";
                var text = Fill(Pick(f, key + d.Id, _copy.GetProperty(key), DeathCond(f, d)), f, d, productName);
                SplitLead(text, out var lead, out var rest);
                var q = f.Inquests.FirstOrDefault(x => x.Person.Id == d.Id);
                var extra = new List<string>();
                if (d.Infant && !d.Anonymous) extra.Add("The medicine was for an infant in the family's care.");
                if (BottleLine(f, d) is { } plain) extra.Add(plain);
                // an inquest is called or not when the death happens (as the proof's note says)
                extra.Add(q != null ? $"The coroner's jury found: \u201c{q.Verdict}.\u201d"
                    : d.InquestPending ? "An inquest sits: see your letters."
                    : "No inquest was called.");
                deaths.Blocks.Add(new Block { Kind = BlockKind.Notice, Lead = lead, Text = rest, Clip = PersonClip(f, d, "IN MEMORIAM", extra.ToArray()) });
            }
            e.Left.Add(deaths);
        }
        if (f.Inquests.Count > 0)
        {
            var court = new Section { Title = S(sec, "coroner") };
            // a busy month in the court is a story of its own, under one run-in head
            if (f.Inquests.Count >= 3)
            {
                var named = f.Inquests.Count(q => q.Outcome == "implicated");
                court.Blocks.Add(new Block
                {
                    Kind = BlockKind.Notice,
                    Lead = "THE MULBERRY STREET BOTTLES.—",
                    Text = $"{Cap(Words(f.Inquests.Count))} inquests sat this month upon customers of a single shop, and the same label was found at every bedside." +
                        (named > 0 ? $" {Cap(Words(named))} {(named == 1 ? "jury" : "juries")} named it." : " No jury named it, which the ward has found remarkable."),
                });
            }
            foreach (var q in f.Inquests)
            {
                var key = q.Outcome switch { "bribed" => "inquestBribed", "implicated" => "inquestImplicated", _ => "inquestCleared" };
                var template = Pick(f, key + q.Person.Id, _copy.GetProperty(key));
                if (q.Person.Infant) template = template.Replace("{name}", "the infant child of {name}");
                var text = Fill(template, f, q.Person, q.Product ?? productName)
                    .Replace("{verdict}", q.Verdict);
                // with more than one remedy on sale, say whose bottle was on the mantel
                if (f.Products.Count > 1 && q.Product != null && !text.Contains(q.Product, StringComparison.Ordinal))
                    text += $" The bottle in evidence was {q.Product}.";
                var meaning = q.Outcome switch
                {
                    "bribed" => "Your envelope reached the coroner. The verdict is the one you paid for.",
                    "implicated" => "Your product is named in the findings. The Board of Health will take an interest, and the ward will talk.",
                    _ => "The jury found nothing to say against you. This time.",
                };
                court.Blocks.Add(new Block { Text = text, Clip = PersonClip(f, q.Person, "THE INQUEST", $"Verdict: \u201c{q.Verdict}.\u201d", meaning) });
            }
            e.Left.Add(court);
        }
        var personal = new Section { Title = S(sec, "personal") };
        var usedJoined = new HashSet<string>();
        // a customer's notice whose joke has run in the last two years is not told again: those
        // customers are named together, plainly, in one line
        var plainJoined = new List<PersonFact>();
        foreach (var p in f.Joined)
        {
            // while the ward remembers the deaths, a new customer comes in warily, if at all
            var template = f.FirstEdition ? S(_copy, "regularsIntro") : PickRested(f, "joined" + p.Id, _copy.GetProperty(f.WardMemory >= 35 ? "joinedWary" : "joined"), PersonCond(f, p), usedJoined);
            if (template == null) { plainJoined.Add(p); continue; }
            personal.Blocks.Add(new Block { Text = Fill(template, f, p, productName), Clip = PersonClip(f, p, "A NEW CUSTOMER", $"Looking for something for {p.Ailment.ToLowerInvariant()}.") });
        }
        if (plainJoined.Count > 0)
        {
            var names = plainJoined.Select(p => $"{p.Name}, of {Home(f, p)}").ToList();
            var clip = new Clipping { Kicker = "NEW CUSTOMERS", Title = plainJoined.Count == 1 ? plainJoined[0].Name : $"{Cap(Words(plainJoined.Count))} New This Month", Cut = CutFor(plainJoined[0]) };
            foreach (var p in plainJoined) clip.Lines.Add($"{p.Name}, {Occupation(p)}: came for {p.Ailment.ToLowerInvariant()}.");
            personal.Blocks.Add(new Block { Text = Pick(f, "joinedPlain", _copy.GetProperty("joinedPlain")).Replace("{names}", List(names)), Clip = clip });
        }
        var drifted = f.Left.Where(l => l.Reason == "drifted").ToList();
        if (drifted.Count > 2)
        {
            var names = drifted.Select(l => l.Person.Name).ToList();
            personal.Blocks.Add(new Block { Text = string.Join(", ", names.Take(names.Count - 1)) + " and " + names[^1] + " have not called at the shop this month. The proprietor has noticed." });
        }
        var plainLeft = new List<LeftFact>();
        foreach (var l in f.Left.Where(l => drifted.Count <= 2 || l.Reason != "drifted"))
        {
            var key = l.Reason switch { "recovered" => "leftRecovered", "dissatisfied" => "leftDissatisfied", "bereaved" => "leftBereaved", "afraid" => "leftAfraid", _ => "leftDrifted" };
            var why = l.Reason switch
            {
                "recovered" => "Got better on their own. It happens; the trade does not advertise it.",
                "dissatisfied" => "Felt it did nothing for them, and said so. Something stronger, or something cheaper, might have kept them.",
                "bereaved" => "Someone in the household died. The family has no further need of the remedy.",
                "afraid" => "Took fright at the talk of the deaths, and would not buy another bottle. The ward remembers.",
                _ => "Simply stopped coming. Perhaps a rival's bottle; perhaps the price.",
            };
            var said = PickRested(f, key + l.Person.Id, _copy.GetProperty(key), PersonCond(f, l.Person), usedJoined);
            if (said == null) { plainLeft.Add(l); continue; }
            personal.Blocks.Add(new Block { Text = Fill(said, f, l.Person, productName), Clip = PersonClip(f, l.Person, "GONE FROM THE SHOP", why) });
        }
        if (plainLeft.Count > 0)
        {
            static string Why(string reason) => reason switch
            {
                "recovered" => "who got better", "dissatisfied" => "who found it did nothing", "bereaved" => "who is in mourning",
                "afraid" => "who took fright at the talk", _ => "who stopped coming",
            };
            var names = plainLeft.Select(l => $"{l.Person.Name}, {Why(l.Reason)}").ToList();
            var clip = new Clipping { Kicker = "GONE FROM THE SHOP", Title = plainLeft.Count == 1 ? plainLeft[0].Person.Name : $"{Cap(Words(plainLeft.Count))} Gone This Month", Cut = CutFor(plainLeft[0].Person) };
            foreach (var l in plainLeft) clip.Lines.Add($"{l.Person.Name}: {Why(l.Reason)}.");
            personal.Blocks.Add(new Block { Text = Pick(f, "leftPlain", _copy.GetProperty("leftPlain")).Replace("{names}", string.Join("; ", names)), Clip = clip });
        }
        if (personal.Blocks.Count > 0) e.Left.Add(personal);

        var notices = new Section { Title = S(sec, "notices") };
        if (f.ShutdownMonthsLeft > 0)
            notices.Blocks.Add(new Block { Kind = BlockKind.Notice, Text = Fill(Pick(f, "shutdown", _copy.GetProperty("shutdown")), f, null, productName).Replace("{within}", f.ShutdownMonthsLeft == 1 ? "at the month's end" : $"in {Words(f.ShutdownMonthsLeft)} months") });
        foreach (var (prod, bottles) in f.BadBatches)
        {
            // where the batch was spoiled, and what went down the gutter with it: the drugs were
            // paid for when bought, so the loss is in the stock, not a line of its own in the books
            var lab = f.Held.Any(h => h.Id == "laboratory");
            var cost = Money(bottles * (f.Products.FirstOrDefault(p => p.Name == prod)?.UnitCostCents ?? 0));
            notices.Blocks.Add(new Block
            {
                Text = Fill(Pick(f, "bad", _copy.GetProperty("badBatch"), c => c == "laboratory" && lab), f, null, prod)
                    .Replace("{bottles}", bottles.ToString(CultureInfo.InvariantCulture))
                    .Replace("{place_upper}", lab ? "AT THE LABORATORY" : "IN THE BACK ROOM")
                    .Replace("{cost}", cost),
                Clip = new Clipping
                {
                    Kicker = "A BAD BATCH",
                    Title = prod,
                    Cut = "wc_bottle_panacea",
                    Lines =
                    {
                        $"{bottles} bottles came out of the still wrong, and your taster caught them before they reached the shelf.",
                        $"About {cost} of drugs, glass and corks poured away. The drugs were paid for when bought, so the loss shows in the stock, not as a line in the books.",
                        "Any batch may come out wrong; without the taster it would have been sold.",
                    },
                },
            });
        }
        if (f.BottlesSeized > 0)
            notices.Blocks.Add(new Block { Text = Fill(Pick(f, "seized", _copy.GetProperty("stockSeized")), f, null, productName).Replace("{bottles}", f.BottlesSeized.ToString(CultureInfo.InvariantCulture)) });
        if (f.Testimonial is { } tw)
        {
            notices.Blocks.Add(new Block
            {
                Kind = BlockKind.Notice,
                Lead = "A TESTIMONIAL.—",
                Text = $"\u201cThree physicians gave me up, and {productName} gave me back. I am a new woman.\u201d So writes {Misspell(tw.Name)}, of {Home(f, tw)}, top floor, rear, who asks that her name be spelled correctly.—Adv.",
                Clip = PersonClip(f, tw, "YOUR TESTIMONIAL", "Printed as she asked, very nearly.", f.CashCents >= 10000 ? "Standing in the ward rose; with $100 put by, the Gazette will sell you space from next month." : "Standing in the ward rose, and the Gazette will sell you space once you have $100 put by."),
            });
        }
        foreach (var (id, name, change) in f.InvestmentNews)
        {
            var key = $"{id}.{change}";
            if (_copy.GetProperty("investmentNotices").TryGetProperty(key, out var said))
                notices.Blocks.Add(new Block { Text = Fill(Pick(f, "venture" + key, said), f, null, productName), Clip = new Clipping { Kicker = "YOUR VENTURES", Title = name, Cut = "wc_proprietor", Lines = { change switch { "bought" => "Taken up this month.", "lapsed" => "Run out: it may be taken up again in the prospectus.", "lost" => "Lost: it is no longer yours.", _ => "An accident on the premises." } } } });
        }
        if (f.AdsLapsed)
            notices.Blocks.Add(new Block { Text = Fill(Pick(f, "lapsed", _copy.GetProperty("adsLapsed")), f, null, productName) });
        foreach (var (id, title, description) in f.Unlocks)
        {
            // the paper reports the trade's gossip about a third party; the cutting tells you plainly
            // what came, that it came with the shop's experience (nothing was bought), and what it does
            var clip = new Clipping { Kicker = "A NEW CAPABILITY", Title = title, Cut = "wc_proprietor" };
            if (_copy.GetProperty("unlockClips").TryGetProperty(id, out var lines))
                foreach (var line in lines.EnumerateArray()) clip.Lines.Add(Fill(line.GetString() ?? "", f, null, productName));
            else clip.Lines.Add(description);
            clip.Lines.Add("Earned this month, not bought. It is yours to use from the next.");
            notices.Blocks.Add(new Block
            {
                Text = _copy.GetProperty("unlockNotices").TryGetProperty(id, out var said)
                    ? Fill(said.GetString() ?? "", f, null, productName)
                    : Pick(f, "unlock", _copy.GetProperty("unlock")).Replace("{title}", title.ToUpperInvariant()).Replace("{description}", description),
                Clip = clip,
            });
        }
        if (notices.Blocks.Count > 0) e.Left.Add(notices);

        // a quiet month still fills the column: the classifieds
        var used = e.Left.Sum(x => x.Blocks.Count);
        if (used < 6)
        {
            var wants = new Section { Title = S(sec, "classified") };
            // the Gazette's own wants, of the year, the longest unprinted first, none twice in an
            // edition; between them the plain wants of the day, made fresh for the year
            var list = Opts(_copy.GetProperty("classifieds"), f);
            var usedWants = new HashSet<string>();
            var count = Math.Min(list.Count, 6 - used + 1);
            // (a want of the Gazette's own printed in the last two years gives way to a plain want)
            for (var i = 0; i < count; i++)
                wants.Blocks.Add(new Block { Text = i % 2 == 1 ? PlainWant(f, i) : FreshRested(f, "wants" + i, list, usedWants) ?? PlainWant(f, i) });
            e.Left.Add(wants);
        }

        // ---- under the lead: the trade, and the era ----
        var trade = new Section { Title = S(sec, "trade") };
        var books = BooksClip(f);
        if (f.Sales.Count == 0 && !f.FirstEdition)
            trade.Blocks.Add(new Block { Text = Pick(f, "nosales", _copy.GetProperty("tradeNoSales")), Clip = books });
        // the druggists out of town take theirs from the bottle the shop has most of: those are
        // reported under Wholesale, not as sold over the counter at the shelf price
        // (and so are the almanac's post orders): the counter's own count comes from the books
        var away = f.WholesaleDelivered + f.PostDelivered;
        var wholesaleFrom = away > 0 && f.Sales.Any(x => x.CounterBottles < 0) ? f.Sales.OrderByDescending(x => x.Bottles).FirstOrDefault()?.Product : null;
        var usedSales = new HashSet<string>();
        foreach (var s0 in f.Sales)
        {
            var s = s0.CounterBottles >= 0 ? s0 with { Bottles = s0.CounterBottles }
                : s0.Product == wholesaleFrom ? s0 with { Bottles = Math.Max(0, s0.Bottles - away) } : s0;
            if (s.Bottles == 0 && s0.Bottles > 0) continue;
            var price = f.Products.FirstOrDefault(p => p.Name == s.Product)?.PriceCents ?? 0;
            trade.Blocks.Add(new Block
            {
                Text = PickUnused(f, "sales" + s.Product, _copy.GetProperty("tradeSales"), usedSales)
                    .Replace("{product}", s.Product)
                    .Replace("{bottles}", s.Bottles.ToString(CultureInfo.InvariantCulture))
                    .Replace("{price}", Money(price))
                    // selling out is money left on the counter: say so (unless the lead story already has)
                    + (f.Products.FirstOrDefault(p => p.Name == s.Product) is { Stock: 0 } && s.Bottles > 0
                       && !(_leadKinds.TryGetValue(f.Turn, out var lk) && lk.TrimEnd('+') == "shortage" && f.Products.Count == 1)
                        ? " " + Pick(f, "shelfBare" + s.Product, _copy.GetProperty("tradeShelfBare"))
                        : ""),
                Clip = books,
            });
        }
        if (f.Prospects.Count > 0 && !f.FirstEdition)
        {
            // gossip that says what the next thing is and what it takes, as the trade would put it,
            // with the count it waits on; a thing already earned and only waiting to be taken up
            // (the shop next door, offered in the ventures) is said to be waiting
            var x = f.Prospects[0];
            // the trade talks of a thing when it changes, and every few months while it waits:
            // not the same gossip in every edition
            string Band(double p) => p >= 0.75 ? "near" : p >= 0.4 ? "half" : "far";
            var said = (x.Id, f.ProspectsReady.Contains(x.Id) ? "ready" : Band(x.Progress));
            var lastSaid = _talk.Where(k => k.Key < f.Turn).OrderByDescending(k => k.Key).Select(k => ((int Turn, (string, string) Said)?)(k.Key, k.Value)).FirstOrDefault();
            var talk = lastSaid is not { } ls || ls.Said != said || f.Turn - ls.Turn >= 4;
            if (talk) _talk[f.Turn] = said;
            string text = "";
            if (!talk) { }
            else if (f.ProspectsReady.Contains(x.Id) && _copy.GetProperty("prospectReady").TryGetProperty(x.Id, out var ready))
                text = Pick(f, "ready" + x.Id, ready);
            else
            {
                string Near(double p) => p >= 0.75 ? "very near to" : p >= 0.4 ? "halfway to" : "some way yet from";
                var what = _copy.GetProperty("prospectHints").TryGetProperty(x.Id, out var h) ? Pick(f, "hint" + x.Id, h) : x.Name.Trim('"').ToLowerInvariant();
                var count = f.ProspectCounts.TryGetValue(x.Id, out var cnt) ? $" ({cnt} so far)" : "";
                text = Pick(f, "tradeTalk", _copy.GetProperty("tradeTalk")).Replace("{near}", Near(x.Progress)).Replace("{what}", what + count);
            }
            if (text.Length > 0) trade.Blocks.Add(new Block { Text = Fill(text, f, null, productName) });
        }
        // what the shop's letters bought it, set in type with the claims (the Chicago medal, the radium
        // licence), and news in the trade, once, in the edition it first appears
        var honours = _copy.GetProperty("honours");
        var marks = honours.EnumerateObject().Where(h => f.Flags.Contains(h.Name)).Select(h => S(h.Value, "mark")).ToList();
        _flagsAt[f.Turn] = f.Flags;
        var before = _flagsAt.Where(k => k.Key < f.Turn).OrderByDescending(k => k.Key).Select(k => k.Value).FirstOrDefault() ?? new HashSet<string>();
        foreach (var h in honours.EnumerateObject().Where(h => f.Flags.Contains(h.Name) && !before.Contains(h.Name) && !f.FirstEdition))
            trade.Blocks.Add(new Block { Text = Fill(S(h.Value, "trade"), f, null, productName) });
        // the ledger's lines in the trade column: the figures are the books', the words the
        // compositor's, and they are not set the same way every month
        string Frame(string key, params (string Slot, string Value)[] slots)
        {
            var t = Pick(f, key, _copy.GetProperty(key));
            foreach (var (slot, value) in slots) t = t.Replace("{" + slot + "}", value);
            return t;
        }
        var stockDelta = f.StockValueCents - f.StockValueStartCents;
        if (!f.FirstEdition && f.StockValueCents > 0)
            trade.Blocks.Add(new Block
            {
                Text = Frame("tradeStock", ("stock", Money(f.StockValueCents))).TrimEnd('.')
                    + (Math.Abs(stockDelta) >= 100 ? $" ({(stockDelta > 0 ? "up" : "down")} {Money(Math.Abs(stockDelta))} on the month)." : "."),
                Clip = books,
            });
        var sources = _copy.GetProperty("callerSources");
        var callers = f.Callers.Where(c => c.Callers > 0).ToList();
        if (callers.Count > 1)
        {
            // each source in one of its phrasings: "ten by the almanac", "ten with the almanac in hand"
            var list = callers.Select(c => $"{Words(c.Callers)} {(sources.TryGetProperty(c.Source, out var w) ? Pick(f, "source." + c.Source, w) : c.Source)}").ToList();
            trade.Blocks.Add(new Block { Text = Frame("tradeCallers", ("list", string.Join(", ", list))), Clip = books });
        }
        if (f.PostDelivered > 0)
            trade.Blocks.Add(new Block { Text = Frame("tradePost", ("bottles", Words(f.PostDelivered) + " bottles"), ("money", Money(f.PostRevenueCents))), Clip = books });
        if (f.WholesaleOrdered > 0)
            trade.Blocks.Add(new Block
            {
                Text = f.WholesaleDelivered == 0
                    ? Frame("tradeWholesaleNone", ("ordered", Words(f.WholesaleOrdered) + " bottles"))
                    : Frame("tradeWholesale", ("ordered", Words(f.WholesaleOrdered) + " bottles"), ("had", Words(f.WholesaleDelivered)), ("money", Money(f.WholesaleRevenueCents)))
                      + (f.WholesaleDelivered < f.WholesaleOrdered ? " The rest went short." : ""),
                Clip = books,
            });
        if (f.TurnedAwayByPrice > 0)
            trade.Blocks.Add(new Block { Text = Frame("tradeTurned", ("n", Words(f.TurnedAwayByPrice))), Clip = books });
        if (f.RefusedNarcotic > 0)
            trade.Blocks.Add(new Block { Text = Frame("tradeRefusedNarcotic", ("n", Words(f.RefusedNarcotic))), Clip = books });
        if (TillFlat(f) is { } flat) trade.Blocks.Add(new Block { Lead = "WHERE THE MONEY WENT.—", Text = flat, Clip = books });
        if (!f.FirstEdition && f.CashLines.Count > 0)
        {
            var takings = f.CashLines.Where(x => x.Cents > 0).Sum(x => x.Cents);
            var costs = f.CashLines.Where(x => x.Cents < 0).OrderBy(x => x.Cents).ToList();
            // every outgoing is listed, so the items add up to the total
            var itemised = string.Join(", ", costs.Select(x => $"{x.Label.ToLowerInvariant()} {Money(-x.Cents)}"));
            trade.Blocks.Add(new Block { Text = $"Takings {Money(takings)}; outgoings {Money(-costs.Sum(x => x.Cents))}" + (itemised.Length > 0 ? $" ({itemised})." : "."), Clip = books });
        }
        trade.Blocks.Add(new Block { Text = Pick(f, "books", _copy.GetProperty("tradeBooks")).Replace("{cash}", Money(f.CashCents)), Clip = books });
        e.LeadFoot.Add(trade);
        if (f.Era.Count > 0 && !(e.Lead.Headline == f.Era[0].Headline.ToUpperInvariant() && f.Era.Count == 1))
        {
            var era = new Section { Title = S(sec, "era") };
            foreach (var (headline, body) in f.Era)
            {
                if (e.Lead.Headline == headline.ToUpperInvariant()) continue;
                era.Blocks.Add(new Block { Lead = headline.ToUpperInvariant() + ".—", Text = body });
            }
            e.LeadFoot.Add(era);
        }

        // ---- right column: advertisements ----
        var ads = new Section();
        var tier = f.CanAdvertise ? AdSpace.TierFor(f.AdSpendCents) : 0;
        if (product != null && tier > 0)
        {
            ads.Blocks.Add(new Block
            {
                Kind = BlockKind.Ad,
                Mid = tier >= 3 ? "THE WONDER OF THE AGE!" : "",
                Head = product.Name.ToUpperInvariant(),
                Text = string.Join(" · ", (product.ClaimNames.Count > 0 ? product.ClaimNames.Select(c => c.ToUpperInvariant()) : new[] { "FOR ALL COMPLAINTS" }).Concat(marks)),
                Fine = tier >= 2 ? $"{Money(product.PriceCents)} the bottle, at the shop on Mulberry street." : "",
                Cut = tier >= 2 ? "wc_bottle_panacea" : "",
                Clip = new Clipping
                {
                    Kicker = "YOUR NOTICE",
                    Title = product.Name,
                    Cut = "wc_bottle_panacea",
                    Lines =
                    {
                        $"Space bought: {AdSpace.Tiers[tier].Name.ToLowerInvariant()}, {Money(AdSpace.Tiers[tier].Cents)} a month.",
                        product.ClaimNames.Count > 0 ? "Claims set in type: " + string.Join("; ", product.ClaimNames) + "." : "No claims: just the name and the price.",
                        f.Ended ? "Bolder claims brought in more custom, and more talk in the ward." : "Bolder claims bring in more custom, and more talk in the ward. Change them on the proof.",
                    },
                },
            });
        }
        if (product != null && tier > 0 && marks.Count > 0) ads.Blocks[^1].Clip!.Lines.Insert(2, "Also set in type, by right: " + string.Join("; ", marks) + ".");
        // the rival advertisements of the day: the bicycle tonic in the nineties, radium after 1903
        // two of the year's advertisers, the longest out of the paper first; each changes its copy
        // as the years go on, and its last line (the joke) is never the same twice in two years
        var rivals = _copy.GetProperty("rivals").EnumerateArray().Where(r => Fits(r, f, null)).ToList();
        var heads = rivals.Select(r => S(r, "head")).ToList();
        var usedRivals = new HashSet<string>();
        for (var i = 0; i < 2 && i < rivals.Count; i++)
        {
            var head = Fresh(f, "rival" + i, heads, usedRivals);
            var r = rivals[heads.IndexOf(head)];
            string Part(string name, string plural)
            {
                if (r.TryGetProperty(plural, out var list)) return Pick(f, "rival." + head + "." + name, list);
                return r.TryGetProperty(name, out var one) ? one.GetString() ?? "" : "";
            }
            ads.Blocks.Add(new Block
            {
                Kind = BlockKind.Ad,
                Head = head,
                Mid = Part("mid", "mids"),
                Text = Part("body", "bodies"),
                Fine = r.TryGetProperty("fines", out var fines) ? PickRested(f, "rival." + head + ".fine", fines) ?? "" : Part("fine", "fines"),
                Cut = r.TryGetProperty("cut", out var cut) ? cut.GetString() ?? "" : "",
            });
        }
        if (f.Deaths.Count > 0)
        {
            var u = _copy.GetProperty("undertaker");
            ads.Blocks.Add(new Block { Kind = BlockKind.BlackAd, Head = S(u, "head"), Mid = S(u, "mid"), Text = S(u, "body") });
        }
        e.Right.Add(ads);

        // ---- the markets ----
        e.Markets = WriteMarkets(f, product);
        Typeset(e);
        return e;
    }

    private Story WriteLead(PaperFacts f, ProductFact? product)
    {
        if (f.Ended && f.EndReason != null && _copy.GetProperty("endings").TryGetProperty(f.EndReason, out var end))
        {
            var name = product?.Name ?? "Dr. ——'s remedies";
            string Sub(string x) => x.Replace("{product_upper}", name.ToUpperInvariant()).Replace("{product}", name);
            return new Story
            {
                Headline = S(end, "headline"),
                Decks = end.GetProperty("decks").EnumerateArray().Select(x => Sub(x.GetString() ?? "")).ToList(),
                Cut = S(end, "cut"),
                Cutline = "FROM OUR ARCHIVE.",
                Body = Sub(S(end, "body")),
            };
        }
        if (f.FirstEdition)
        {
            var fe = _copy.GetProperty("firstEdition");
            return new Story
            {
                Headline = S(fe, "headline"),
                Decks = fe.GetProperty("decks").EnumerateArray().Select(x => x.GetString() ?? "").ToList(),
                Cut = "wc_proprietor",
                Cutline = "THE PROPRIETOR.—From a likeness.",
                Body = S(fe, "body"),
            };
        }
        // the lead is the month's biggest fact: the coroner naming you, then the dead, then the
        // news of the age, then trade; a quiet month only when nothing happened at all
        // a bought verdict found out outranks everything but the end
        if (f.Exposed.Count > 0)
        {
            var x = _copy.GetProperty("exposedLead");
            var who = f.Exposed[0];
            string Sub(string t) => Fill(t.Replace("{who_upper}", Who(f, who).ToUpperInvariant()), f, who, product?.Name ?? "the remedy");
            return new Story
            {
                Headline = Pick(f, "exposedHead", x.GetProperty("headline")),
                Decks = x.GetProperty("decks").EnumerateArray().Select(d => Sub(d.GetString() ?? "")).ToList(),
                Cut = S(x, "cut"),
                Cutline = S(x, "cutline"),
                Body = Sub(S(x, "body")),
                Clip = PersonClip(f, who, "THE INQUEST", "Your envelope to the coroner has come out. A bought verdict found out costs more than a jury's: suspicion and standing both.", "The coroner's fee rises with every envelope, and so does the chance of this."),
            };
        }
        var named = f.Inquests.FirstOrDefault(q => q.Outcome == "implicated");
        if (named != null)
        {
            return new Story
            {
                Headline = _copy.TryGetProperty("verdictHeadlines", out var vh) ? Pick(f, "verdictHead", vh) : "THE CORONER NAMES A REMEDY",
                Decks = new List<string> { "A Verdict Against the Bottle.", $"THE JURY FINDS THAT {Who(f, named.Person).ToUpperInvariant()}: \u201c{named.Verdict.ToUpperInvariant()}.\u201d", "The Board of Health Is Informed." },
                Cut = "wc_death",
                Cutline = "A GENTLEMAN SEEN AT THE INQUEST.",
                Body = Pick(f, "verdictLead" + named.Person.Id, _copy.GetProperty("verdictLead")).Replace("{label}", named.Product ?? product?.Name ?? "a proprietary medicine"),
                Clip = PersonClip(f, named.Person, "THE INQUEST", $"Verdict: \u201c{named.Verdict}.\u201d", "Your product is named in the findings. The Board of Health will take an interest, and the ward will talk."),
            };
        }
        // deaths out of town are the druggists' strangers: the ward leads with its own
        var local = f.Deaths.Where(x => !x.OutOfTown).ToList();
        if (local.Count >= 3)
        {
            // the bottles are blamed only where a bottle could be: a register that says overdose or
            // a wasting, or a remedy with harm in it; otherwise the joke is that there is nothing to find
            var sick = _copy.GetProperty("sickly");
            var risky = local.Any(x => PersonCond(f, x)("risky"));
            Func<string, bool> cond = c => c == "risky" && risky;
            string N(string t) => t.Replace("{n_upper}", Words(local.Count).ToUpperInvariant()).Replace("{n}", Words(local.Count));
            return new Story
            {
                Headline = N(Pick(f, "sickly.head", sick.GetProperty("headline"), cond)),
                Decks = Decks(f, "sickly.deck", sick.GetProperty("decks"), cond).Select(N).ToList(),
                Cut = "wc_death",
                Cutline = "A GENTLEMAN SEEN IN THE NEIGHBORHOOD.",
                Body = N(Pick(f, "sickly.body", sick.GetProperty("bodies"), cond)),
                Clip = BooksClip(f),
            };
        }
        // An advertisement dressed as news, when the player is paying for space.
        if (product != null && f.CanAdvertise && AdSpace.TierFor(f.AdSpendCents) == AdSpace.Front)
        {
            var rn = _copy.GetProperty("readingNotice");
            // the Gazette quotes a different satisfied customer each month, the longest unquoted first
            var subject = f.Enthusiasts.Count == 0 ? null
                : f.Enthusiasts.First(p => p.Name == Fresh(f, "rn.subject", f.Enthusiasts.Select(x => x.Name).Distinct().ToList()));
            // the claims set in type take turns at the head of the notice, under a frame of the month's
            var claim = product.ClaimNames.Count > 0 ? Fresh(f, "rn.claim", product.ClaimNames) : null;
            var frame = rn.TryGetProperty("headlines", out var frames)
                ? Pick(f, "rn.head", frames, c => c == "claim" ? claim != null : c == "subject" && subject != null)
                : "{claim}!";
            if (claim == null && frame.Contains("{claim}")) frame = "NEVER FELT BETTER!";
            var headline = frame.Replace("{claim}", claim?.ToUpperInvariant() ?? "").Replace("{product_upper}", product.Name.ToUpperInvariant()).Replace("{street_upper}", Home(f, subject).ToUpperInvariant());
            Func<string, bool> cond = c => c switch
            {
                "habit" => product.Habit >= 1,
                "drug" => product.Kick >= 2 || product.Calm >= 2,
                "subject" => subject != null,
                _ => false,
            };
            // (the deck that names the customer changes with the customer; the others rest two years)
            var decks = rn.GetProperty("decks").EnumerateArray().Select((x, i) => Deck(f, "rn.deck" + i, x, cond, rested: i != 1))
                .Where(x => x.Length > 0 && (subject != null || !(x.Contains("{name}") || x.Contains("{name_upper}")))).ToList();
            if (decks.Count == 0) decks.Add(Deck(f, "rn.deck0.any", rn.GetProperty("decks")[0], cond));
            // a notice that quotes a customer, unless every one has run in two years: then the
            // proprietor's own puff, and only if that too has run, the longest unprinted of either
            var body = (subject != null ? PickRested(f, "rn", rn.GetProperty("bodies"), cond) : null)
                ?? PickRested(f, "rnf", rn.GetProperty("fallbackBody"), cond)
                ?? (subject != null ? Pick(f, "rn.any", rn.GetProperty("bodies"), cond) : Pick(f, "rnf.any", rn.GetProperty("fallbackBody"), cond));
            var street = Home(f, subject);
            string Sub(string s) => Fill(s.Replace("{name_upper}", subject?.Name.ToUpperInvariant() ?? ""), f, subject, product.Name)
                .Replace("{street_upper}", street.ToUpperInvariant())
                .Replace("{product_upper}", product.Name.ToUpperInvariant())
                .Replace("{price}", Money(product.PriceCents));
            return new Story
            {
                Headline = headline,
                Decks = decks.Select(Sub).ToList(),
                Cut = subject == null ? "wc_bottle_panacea" : CutFor(subject),
                Cutline = subject == null ? "A BOTTLE.—From the engraver's stock." : $"{subject.Name.ToUpperInvariant()}.—From a stock likeness.",
                Body = Sub(body),
                IsAdvertisement = true,
                Clip = subject == null ? null : PersonClip(f, subject, "QUOTED IN YOUR NOTICE", "The Gazette set your advertisement as news, and found this customer to quote. Nobody asked them."),
            };
        }
        if (local.Count > 0)
        {
            var d = local[0];
            var street = Home(f, d);
            return new Story
            {
                Headline = d.Anonymous ? $"FOUND DEAD IN {street.ToUpperInvariant()}"
                    : d.Infant ? $"THE {SplitName(d.Name).Surname.ToUpperInvariant()} INFANT"
                    : $"DEATH OF {ShortName(d).ToUpperInvariant()}",
                Decks = Decks(f, "deathDeck", _copy.GetProperty("deathDecks").GetProperty(d.Anonymous ? "anonymous" : d.Infant ? "infant" : "self"), PersonCond(f, d))
                    .Select(x => x.Replace("{who_upper}", Who(f, d).ToUpperInvariant())).ToList(),
                Cut = d.Anonymous ? "wc_death" : CutFor(d),
                Cutline = d.Anonymous ? "A GENTLEMAN SEEN IN THE NEIGHBORHOOD." : d.Infant ? $"THE MOTHER, {ShortName(d).ToUpperInvariant()}.—From a stock likeness." : $"{ShortName(d).ToUpperInvariant()}.—From a stock likeness.",
                Body = DeathBody(f, d, product?.Name ?? "the remedy"),
                Clip = PersonClip(f, d, "IN MEMORIAM", new[] { d.Infant && !d.Anonymous ? "The medicine was for an infant in the family's care." : "", BottleLine(f, d) ?? "", "See the deaths column for the notice." }.Where(x => x.Length > 0).ToArray()),
            };
        }
        if (f.Era.Count > 0)
        {
            var (headline, body) = f.Era[0];
            return new Story { Headline = headline.ToUpperInvariant(), Body = body };
        }
        if (product != null) return TradeLead(f, product);
        return new Story
        {
            Headline = "ALL QUIET IN THE WARD",
            Decks = new List<string> { "Nothing to Report.", "Coughs Steady." },
            Cut = "wc_clerk",
            Cutline = "A RESIDENT.",
            Body = "Our reporter walked the length of Mulberry street and found nothing worth printing. The editor has sent him back.",
        };
    }

    /// <summary>
    /// The lead in a month without deaths or news: the state of the trade, read off the books.
    /// A bought verdict, arrears, a dull month, new faces, lost custom or a good month, each with
    /// its own cut and several bodies so the same words do not run month after month.
    /// </summary>
    /// <summary>Which trade lead ran in each edition, so the next can avoid repeating it.</summary>
    private readonly Dictionary<int, string> _leadKinds = new();

    private Story TradeLead(PaperFacts f, ProductFact product)
    {
        var sold = f.Sales.Sum(x => x.Bottles);
        // money laid out on stock still on the shelf is not a bad month: judge cash and stock together
        // and a venture paid for is an outlay, not a loss
        var books = f.CashCents - f.CashStartCents + (f.StockValueCents - f.StockValueStartCents);
        var delta = books + Math.Max(0, f.InvestedCents);
        var bribed = f.Inquests.FirstOrDefault(q => q.Outcome == "bribed");
        var lost = f.Left.Where(l => l.Reason != "bereaved").ToList();
        // every lead the month could carry, most pressing first
        var soldOut = sold > 0 && f.Products.Count > 0 && f.Products.All(p => p.Stock == 0);
        // a record beats every month before it, by a margin worth printing
        var record = f.BestSoldBefore >= 6 && sold > f.BestSoldBefore && sold * 20 >= f.BestSoldBefore * 21 && delta > 0;
        var slump = delta < 0 || (f.SoldLastMonth >= 6 && sold * 3 < f.SoldLastMonth * 2);
        var options = new List<string>();
        if (bribed != null) options.Add("bribed");
        if (f.CashCents < 0) options.Add("arrears");
        if (sold == 0) options.Add("nothing");
        // the neighbors' memory of the deaths outranks a good month's trade
        if (f.WardMemory >= 35 && f.RememberedDeaths + f.RememberedVerdicts + f.RememberedInfants > 0) options.Add("remembers");
        if (soldOut) options.Add("shortage");
        // the price drove callers off: say so, rather than blame the weather
        if (f.TurnedAwayByPrice >= 5 && f.TurnedAwayByPrice * 4 >= sold) options.Add("dear");
        if (record) options.Add("record");
        if (slump && !soldOut) options.Add("dull");
        if (f.Joined.Count >= 3 || (f.Joined.Count >= 2 && f.Joined.Count > lost.Count)) options.Add("growing");
        if (lost.Count >= 2 && lost.Count > f.Joined.Count) options.Add("losing");
        // an honest shop gets a word of its own now and then: a year without a death laid at its door
        if (f.Turn >= 6 && f.Turn % 4 == 0 && f.WardMemory < 3 && f.RememberedDeaths + f.RememberedVerdicts + f.RememberedInfants == 0 && sold > 0) options.Add("clean");
        if (delta >= 0) options.Add("brisk");
        if (options.Count == 0) options.Add("dull");
        // never the same lead two editions running if the month offers another; the pressing
        // ones (a bought verdict, arrears, an idle shop, a bare shelf) follow up instead
        var last = _leadKinds.TryGetValue(f.Turn - 1, out var l1) ? l1.TrimEnd('+') : null;
        var before = _leadKinds.TryGetValue(f.Turn - 2, out var l2) ? l2.TrimEnd('+') : null;
        var kind = options[0];
        var third = kind == last && kind == before;
        if (kind == last && (third || kind is not ("bribed" or "arrears" or "nothing" or "shortage")))
            kind = options.FirstOrDefault(o => o != last) ?? kind;
        // "SOLD AGAIN" and "HOLDS UP" are for a month that held: not one where sales fell
        var again = kind == last && !(kind == "brisk" && sold < f.SoldLastMonth);
        // a headline is not printed twice in two years: if every headline of this kind has run
        // lately, lead with another true thing about the month whose headline has not
        var cashUp = f.CashCents - f.CashStartCents;
        Func<string, bool> checkCond = c => c switch
        {
            "up" => cashUp > 0, "down" => cashUp < 0, "outlay" => f.InvestedCents > 0,
            "habit" => product.Habit >= 1, "one" => sold == 1, _ => false,
        };
        bool HasRested(string k, bool ag)
        {
            var node = _copy.GetProperty("tradeLeads").GetProperty(k);
            if (ag && node.TryGetProperty("again", out var a)) node = a;
            return Opts(node.GetProperty("headline"), f, checkCond).Any(h => Rested(f, h));
        }
        if (_leadKinds.TryGetValue(f.Turn, out var kept)) { again = kept.EndsWith("+", StringComparison.Ordinal); kind = kept.TrimEnd('+'); }
        else if (kind is not ("bribed" or "arrears" or "nothing") && !HasRested(kind, again)
            && options.FirstOrDefault(o => o != kind && o != last && o is not ("bribed" or "arrears" or "nothing") && HasRested(o, false)) is { } other)
        {
            kind = other;
            again = false;
        }
        _leadKinds[f.Turn] = kind + (again ? "+" : "");
        PersonFact? subject = kind switch
        {
            "bribed" => bribed!.Person,
            "growing" => f.Joined.FirstOrDefault(),
            "losing" => lost.FirstOrDefault()?.Person,
            "brisk" => f.Enthusiasts.FirstOrDefault(),
            _ => null,
        };

        var t = _copy.GetProperty("tradeLeads").GetProperty(kind);
        var head = again && t.TryGetProperty("again", out var follow) ? follow : t;
        // the books in the headline are the books in the brief: up only if the cash is up
        var cashDelta = f.CashCents - f.CashStartCents;
        Func<string, bool> cond = c => c switch
        {
            "up" => cashDelta > 0,
            "down" => cashDelta < 0,
            "outlay" => f.InvestedCents > 0,
            "subject" => subject != null,
            // a regular who cannot do without it needs a remedy one cannot do without
            "habit" => product.Habit >= 1,
            "one" => sold == 1,
            _ => false,
        };
        var bodies = Opts(t.GetProperty("bodies"), f, cond);
        // a body that names someone needs someone to name
        if (subject == null) bodies = bodies.Where(b => !b.Contains("{name}")).ToList();
        if (bodies.Count == 0) bodies = Opts(t.GetProperty("bodies"), f, null).Where(b => !b.Contains("{name}")).ToList();
        var body = Fresh(f, "trade" + kind, bodies);
        // only a price that went up this month is said to have been put up
        if (kind == "dear" && product.PrevPriceCents is long was && was < product.PriceCents && t.TryGetProperty("raised", out var raised))
            body = raised.GetString() ?? body;
        string Sub(string x)
        {
            if (subject is { Infant: true } && kind == "bribed") x = x.Replace("{name}", "the infant child of {name}");
            x = x.Replace("{sold_upper} BOTTLES", sold == 1 ? "ONE BOTTLE" : "{sold_upper} BOTTLES");
            return Fill(x, f, subject, product.Name)
                .Replace("{product_upper}", product.Name.ToUpperInvariant())
                .Replace("{sold_upper}", Words(sold).ToUpperInvariant())
                .Replace("{joined_upper}", Words(f.Joined.Count).ToUpperInvariant())
                .Replace("{left_upper}", Words(lost.Count).ToUpperInvariant())
                .Replace("{turned_upper}", Words(f.TurnedAwayByPrice).ToUpperInvariant())
                .Replace("{remembered}", RememberedPhrase(f))
                .Replace("{remembered_upper}", RememberedPhrase(f).ToUpperInvariant())
                .Replace("{price}", Money(product.PriceCents));
        }
        return new Story
        {
            Headline = Sub(Pick(f, "headline" + kind + again, head.GetProperty("headline"), cond)),
            Decks = Decks(f, $"deck{kind}{again}", head.GetProperty("decks"), cond).Select(Sub).ToList(),
            Cut = kind is "growing" or "losing" && subject != null ? CutFor(subject) : S(t, "cut"),
            Cutline = kind is "growing" or "losing" && subject != null ? $"{ShortName(subject).ToUpperInvariant()}.—From a stock likeness." : S(t, "cutline"),
            Body = Sub(body),
            Clip = subject != null && kind is "growing" or "losing" ? PersonClip(f, subject, kind == "growing" ? "A NEW CUSTOMER" : "GONE FROM THE SHOP") : BooksClip(f),
        };
    }

    /// <summary>Where someone lives: chosen once per person, the same in every notice.</summary>
    private string Home(PaperFacts f, PersonFact? p)
    {
        var streets = _copy.GetProperty("streets");
        var key = p == null ? "street-" + f.Turn : "home" + (string.IsNullOrEmpty(p.Name) ? p.Id : p.Name);
        return streets[(int)(Hash(f.Seed, 0, key) % (uint)streets.GetArrayLength())].GetString() ?? "";
    }

    /// <summary>
    /// The compositor's last pass over everything printed: curly quotes the right way round, no
    /// doubled full stops after a price ("60c.."), a single space after a stop.
    /// </summary>
    public static string Set(string t)
    {
        if (string.IsNullOrEmpty(t)) return t;
        t = Regex.Replace(t, @"(?<!\.)\.\.(?!\.)", ".");
        t = Regex.Replace(t, "(^|[\\s(\\[—\u2014-])\"", "$1\u201c");
        t = t.Replace("\"", "\u201d");
        // a single quote opens after a space or bracket, or after a dash unless it is the
        // possessive of a blanked name ("Dr. ——'s"); everywhere else it is an apostrophe
        t = Regex.Replace(t, "(^|[\\s(\\[])'", "$1\u2018");
        t = Regex.Replace(t, "([—\u2014-])'(?![sS]\\b)", "$1\u2018");
        t = t.Replace("'", "\u2019");
        t = Regex.Replace(t, " {2,}", " ");
        // one of anything is singular, however the sentence was built ("1 bottles", "one callers")
        t = Regex.Replace(t, @"(?<![\w\-.,$])(1|one|One|ONE) (bottle|regular|caller|customer|month|death|batch|inquest|still)(s|es)\b", "$1 $2");
        t = Regex.Replace(t, @"(?<![\w\-.,$])(ONE) (BOTTLE|REGULAR|CALLER|CUSTOMER|MONTH|DEATH|BATCH|INQUEST|STILL)(S|ES)\b", "$1 $2");
        // a blanked name is never broken across a line: "Dr. ——" travels together
        t = t.Replace("Dr. ——", "Dr.\u00a0——").Replace("DR. ——", "DR.\u00a0——");
        return t;
    }

    private static void Typeset(Edition e)
    {
        void Clip(Clipping? c)
        {
            if (c == null) return;
            c.Title = Set(c.Title);
            for (var i = 0; i < c.Lines.Count; i++) c.Lines[i] = Set(c.Lines[i]);
        }
        foreach (var sec in e.Left.Concat(e.Right).Concat(e.LeadFoot))
            foreach (var b in sec.Blocks)
            {
                b.Text = Set(b.Text); b.Lead = Set(b.Lead); b.Head = Set(b.Head); b.Mid = Set(b.Mid); b.Fine = Set(b.Fine);
                Clip(b.Clip);
            }
        e.Lead.Headline = Set(e.Lead.Headline);
        e.Lead.Body = Set(e.Lead.Body);
        e.Lead.Cutline = Set(e.Lead.Cutline);
        for (var i = 0; i < e.Lead.Decks.Count; i++) e.Lead.Decks[i] = Set(e.Lead.Decks[i]);
        Clip(e.Lead.Clip);
        Clip(e.BriefClip);
        e.Markets = Set(e.Markets);
        e.Brief = Set(e.Brief);
        for (var i = 0; i < e.Standing.Count; i++) e.Standing[i] = Set(e.Standing[i]);
    }

    private static readonly string[] Units = { "no", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
    private static readonly string[] Tens = { "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

    /// <summary>Numbers under a hundred spelled out, as a compositor would; larger ones in figures.</summary>
    /// <summary>The career in review: years, bottles, takings, the dead, and (in 1906) what the label must now say.</summary>
    private Section CareerReview(PaperFacts f, CareerFact c, string product)
    {
        var sec = new Section { Title = "A CAREER IN REVIEW." };
        var start = new DateTime(f.Year, f.Month, 1).AddMonths(-Math.Max(0, c.Months - 1));
        var years = c.Months / 12;
        var months = c.Months % 12;
        var span = years == 0 ? $"{Words(months)} month{(months == 1 ? "" : "s")}"
            : $"{Words(years)} year{(years == 1 ? "" : "s")}" + (months > 0 ? $" and {Words(months)} month{(months == 1 ? "" : "s")}" : "");
        sec.Blocks.Add(new Block { Lead = "IN TRADE.—", Text = $"{Cap(span)} on Mulberry street, from {start.ToString("MMMM yyyy", CultureInfo.InvariantCulture)}, in the making and selling of {product}." });
        sec.Blocks.Add(new Block { Lead = "THE TAKINGS.—", Text = $"{c.Bottles.ToString("N0", CultureInfo.InvariantCulture)} bottles sold over the counter, by post and to the trade, for {Money(c.RevenueCents)} in all." });
        var dead = c.Deaths == 0 ? "No customer of the shop died while it traded, which the trade will think a waste."
            : $"{(c.Deaths < 100 ? Cap(Words(c.Deaths)) : c.Deaths.ToString("N0", CultureInfo.InvariantCulture))} of the shop's customers, or the infants they bought for, died while it traded. "
              + (c.Inquests == 0 ? "No inquest was ever held." : $"{Cap(Words(c.Inquests))} inquest{(c.Inquests == 1 ? " was" : "s were")} held, and the jury named the bottle {(c.Named == 0 ? "not once" : c.Named == 1 ? "once" : $"{Words(c.Named)} times")}")
              + (c.Inquests > 0 && c.Bribes > 0 ? $"; {Words(c.Bribes)} verdict{(c.Bribes == 1 ? " was" : "s were")} bought." : c.Inquests > 0 ? "." : "");
        sec.Blocks.Add(new Block { Lead = "THE DEAD.—", Text = dead });
        if (f.EndReason == "timeline_end")
        {
            var label = c.Declared.Count > 0
                ? $"Under the Act, the label of {product} must now say: {string.Join("; ", c.Declared)}."
                : $"Under the Act, {product} need declare nothing: there is nothing in it the Act asks after.";
            if (c.Undeclared.Count > 0) label += $" The Act does not ask after the {string.Join(", or the ", c.Undeclared)}, and the label will not mention it.";
            sec.Blocks.Add(new Block { Lead = "THE NEW LABEL.—", Text = label });
        }
        return sec;
    }

    /// <summary>The editions that explained a flat till, so the explanation is not every month's.</summary>
    private readonly HashSet<int> _flatAt = new();

    /// <summary>
    /// When the counter took more than last month and the till is no fuller, where the money went,
    /// in a sentence a shopkeeper would follow: what a bottle earns over its cost, and what went out
    /// again (the ventures' rent and wages, letters, the next batches' drugs and glass). At most
    /// once in three editions, and only in a month the till ends about where it began.
    /// </summary>
    private string? TillFlat(PaperFacts f)
    {
        if (f.FirstEdition || f.TakingsLastMonthCents is not long lastTakings || f.CashCauses.Count == 0) return null;
        var takings = f.CashLines.Where(x => x.Cents > 0).Sum(x => x.Cents);
        var rose = takings - lastTakings;
        var kept = f.CashCents - f.CashStartCents;
        if (_flatAt.Contains(f.Turn)) { }
        // flat: the till within an eighth of the takings of where it began, though the takings rose
        else if (takings < 1000 || rose < Math.Max(50, takings / 40) || Math.Abs(kept) > Math.Max(150, takings / 8) || _flatAt.Contains(f.Turn - 1) || _flatAt.Contains(f.Turn - 2)) return null;
        _flatAt.Add(f.Turn);
        var chief = f.Sales.OrderByDescending(s => s.Bottles).Select(s => f.Products.FirstOrDefault(p => p.Name == s.Product)).FirstOrDefault(p => p != null);
        var said = new List<string>();
        var opening = $"The shop took {Money(takings)} this month, {Money(rose)} more than last, " +
            (kept >= 100 ? $"and yet the till closed only {Money(kept)} fuller than it opened." : kept <= -100 ? $"and yet the till closed {Money(-kept)} lighter than it opened." : "and yet the till closed no fuller than it opened.");
        said.Add(opening);
        if (chief != null && chief.UnitCostCents > 0 && chief.PriceCents > chief.UnitCostCents)
            said.Add($"A bottle of {chief.Name} sells at {Money(chief.PriceCents)} and costs {Money(chief.UnitCostCents)} to make, so {Money(chief.PriceCents - chief.UnitCostCents)} of each sale is the shop's.");
        string Name(string cause) => cause switch
        {
            "expense.upkeep" => "the rent and wages of the ventures",
            "expense.rent" => "the shop's rent",
            "expense.ingredients" => "drugs for the next batches",
            "expense.bottling" => "bottles, corks and labels",
            "expense.advertising" => "the Gazette's space",
            "event" => "the letters answered",
            "event.trade_lost" => "trade given up to answer letters",
            "expense.bribe" => "consulting fees",
            "expense.retainer" => "subscriptions and retainers",
            "expense.investment" => "a venture taken up",
            _ => "sundries",
        };
        var outs = f.CashCauses.Where(x => x.Cents < 0).GroupBy(x => Name(x.Cause)).Select(g => (Name: g.Key, Cents: -g.Sum(x => x.Cents))).OrderByDescending(x => x.Cents).ToList();
        if (outs.Count > 0)
        {
            var top = outs.Take(3).Select(x => $"{x.Name} {Money(x.Cents)}").ToList();
            var rest = outs.Skip(3).Sum(x => x.Cents);
            var list = top.Count == 1 ? top[0] : string.Join(", ", top.Take(top.Count - 1)) + " and " + top[^1];
            said.Add($"What came in went out again: {list}" + (rest > 0 ? $", with {Money(rest)} in smaller sums." : "."));
        }
        var stock = f.StockValueCents - f.StockValueStartCents;
        if (stock >= 100) said.Add($"Of that, {Money(stock)} is on the shelf and in the jars, not lost.");
        return string.Join(" ", said);
    }

    /// <summary>What the ward remembers, in a phrase: "three deaths and a coroner's verdict".</summary>
    private static string RememberedPhrase(PaperFacts f)
    {
        var parts = new List<string>();
        var deaths = f.RememberedDeaths + f.RememberedInfants;
        if (deaths > 0) parts.Add($"{Words(deaths)} death{(deaths == 1 ? "" : "s")}");
        if (f.RememberedVerdicts > 0) parts.Add(f.RememberedVerdicts == 1 ? "a coroner's verdict" : $"{Words(f.RememberedVerdicts)} coroner's verdicts");
        return parts.Count == 0 ? "the talk" : string.Join(" and ", parts);
    }

    /// <summary>
    /// The death lead: one that calls the dead a faithful regular only for one who was, that
    /// speaks of a habit only for a remedy that forms one (or a regular who was seen to crave it),
    /// and that lays the death at the bottle only where the register or the bottle allows it.
    /// </summary>
    private string DeathLead(PaperFacts f, PersonFact d)
    {
        var cond = PersonCond(f, d);
        if (d.Months < 0) return Pick(f, "deathLead" + d.Id, _copy.GetProperty("deathLeadStranger"), cond);
        return Pick(f, "deathLead" + d.Id, _copy.GetProperty("deathLead"), cond);
    }

    /// <summary>The infant lead: the one that quotes the label only if the label said it, the quiet child only for a sedative.</summary>
    private string InfantLead(PaperFacts f, PersonFact d) => Pick(f, "infantLead" + d.Id, _copy.GetProperty("infantLead"), PersonCond(f, d));

    /// <summary>The remedy a person took: the one the register names, else the shop's chief remedy.</summary>
    private static ProductFact? RemedyOf(PaperFacts f, PersonFact p) =>
        (p.Product != null ? f.Products.FirstOrDefault(x => x.Name == p.Product) : null) ?? f.Products.FirstOrDefault();

    /// <summary>
    /// What the facts allow the paper to say of a person: "habit" (the remedy forms one, or they
    /// were seen to crave it), "craving", "harm" (the register says overdose or a wasting), "risky"
    /// (that, or a remedy with danger in it), "sedative" (a remedy that quiets), "drug" (one you
    /// would feel), "regular" (three months or more), "known" (a customer at all), "stranger",
    /// "oldage", "ailment", "teething" (the label promised it).
    /// </summary>
    private Func<string, bool> PersonCond(PaperFacts f, PersonFact p)
    {
        var r = RemedyOf(f, p);
        var habitForming = (r?.Habit ?? 0) >= 1;
        var harm = p.Cause is "overdose" or "wasting";
        var risky = harm || (r != null && (r.Danger >= 0.8 || r.Accumulation >= 0.3));
        // a quiet end, or high spirits at the last, are laid at a bottle only if it could have done it
        var sedative = r != null && (r.Calm >= 2 || r.Habit >= 1) && risky;
        return c => c switch
        {
            "habit" => habitForming || p.Craving,
            "craving" => p.Craving || (r?.Habit ?? 0) >= 3,
            "harm" => harm,
            "risky" => risky,
            "sedative" => sedative,
            "drug" => sedative || ((r?.Kick ?? 0) >= 5 && risky),
            "regular" => p.Months >= 3,
            "known" => p.Months >= 1,
            "stranger" => p.Months < 0,
            "oldage" => p.Cause == "old_age",
            "ailment" => p.Cause == "ailment",
            "female" => p.Female,
            // the physician's word on the bottle: it had no part in this death, or it had
            "blameless" => BottleRole(f, p) == "none",
            "blamed" => BottleRole(f, p) is "maybe" or "caused",
            // lives somewhere other than the shop's own street ("walked from Hester street")
            "away" => !Home(f, p).StartsWith("Mulberry", StringComparison.Ordinal),
            "teething" => f.Products.Any(x => x.ClaimNames.Any(n => n.Contains("teeth", StringComparison.OrdinalIgnoreCase) || n.Contains("teething", StringComparison.OrdinalIgnoreCase))),
            _ => false,
        };
    }

    private Func<string, bool> DeathCond(PaperFacts f, PersonFact d) => PersonCond(f, d);

    /// <summary>
    /// What the shop's bottle had to do with a death, by the same reading of the facts as the
    /// proof's note (ProofView.BottleNote), so the paper and the proof agree: "caused" (the register
    /// says overdose or a wasting), "none" (a remedy harmless by the assay, and the dead not craving
    /// it), "maybe" (a stronger bottle, or a craving: it may have hastened it, nobody can say),
    /// "unbought" (nothing bought at the shop lately), or "" (a stranger out of town).
    /// </summary>
    private static string BottleRole(PaperFacts f, PersonFact d)
    {
        if (d.OutOfTown) return "";
        if (d.Cause is "overdose" or "wasting") return "caused";
        if (d.Product == null) return "unbought";
        var product = f.Products.FirstOrDefault(p => p.Name == d.Product) ?? f.Products.FirstOrDefault();
        return product != null && product.Danger < 0.8 && product.Accumulation < 0.3 && !d.Craving ? "none" : "maybe";
    }

    /// <summary>How the bottle may have done harm: "dose", "want" (the habit), "metals" (a slow poison).</summary>
    private static string Harm(PaperFacts f, PersonFact d)
    {
        if (d.Cause == "overdose") return "dose";
        if (d.Cause == "wasting") return "metals";
        var r = RemedyOf(f, d);
        return d.Craving && (r?.Habit ?? 0) > 0 ? "want" : (r?.Accumulation ?? 0) >= 0.3 ? "metals" : "dose";
    }

    /// <summary>The cutting's plain word on the bottle's part in a death, in the proof's words.</summary>
    private static string? BottleLine(PaperFacts f, PersonFact d) => BottleRole(f, d) switch
    {
        "caused" => "The register blames the bottle.",
        "none" => "The remedy is harmless by the assay; the bottle is not blamed.",
        "maybe" => "A strong bottle may have hastened it; nobody can say.",
        "unbought" => "Nothing had been bought at the shop in six months.",
        _ => null,
    };

    /// <summary>
    /// The death lead's body, closed by what the physician says the bottle had to do with it,
    /// unless the lead already gives the physician's word (an entry marked "physician").
    /// </summary>
    private string DeathBody(PaperFacts f, PersonFact d, string product)
    {
        var raw = d.Anonymous ? Pick(f, "unknownLead" + d.Id, _copy.GetProperty("unknownLead"), PersonCond(f, d))
            : d.Infant ? InfantLead(f, d) : DeathLead(f, d);
        var body = d.Anonymous ? raw : Fill(raw, f, d, product);
        var spoken = new[] { "unknownLead", "infantLead", "deathLead", "deathLeadStranger" }
            .SelectMany(k => _copy.GetProperty(k).EnumerateArray())
            .Any(x => x.ValueKind == JsonValueKind.Object && x.TryGetProperty("physician", out var ph) && ph.GetBoolean() && S(x, "text") == raw);
        return !spoken && BottleSays(f, d) is { } says ? body + " " + says : body;
    }

    /// <summary>The same, in the paper's voice, for the lead story: from "deathBottle" by role and harm.</summary>
    private string? BottleSays(PaperFacts f, PersonFact d)
    {
        var role = BottleRole(f, d);
        if (role is "" or "unbought" || !_copy.TryGetProperty("deathBottle", out var all)) return null;
        var key = role == "none" ? "none" : role + "." + Harm(f, d);
        if (!all.TryGetProperty(key, out var list) && !all.TryGetProperty(role, out list)) return null;
        var text = Pick(f, "deathBottle." + d.Id, list, PersonCond(f, d));
        // an infant's death is the child's, not the mother's who bought the bottle
        if (d.Infant) text = text.Replace("{pronounCap}", "The child").Replace("{pronoun}", "the child").Replace("{object}", "the child").Replace("{possessive}", "its");
        return Fill(text, f, d, d.Product ?? f.Products.FirstOrDefault()?.Name ?? "the remedy");
    }

    /// <summary>Where each edition starts reading the shuffled standing matter, among items equally stale.</summary>
    private const int Rotation = 6;
    /// <summary>The most paragraphs of standing matter the make-up sets on a page (EditionView.Balance).</summary>
    private const int StandingSlots = 12;
    /// <summary>Of those, how many are the routine news of the day rather than prose items.</summary>
    private const int RoutineSlots = 6;
    /// <summary>When each item of standing matter was last set on a page, and each month's list.</summary>
    private readonly Dictionary<string, int> _printed = new();
    private readonly Dictionary<int, List<string>> _standingFor = new();
    private readonly HashSet<string> _proseStanding = new();
    /// <summary>The paper's flags at each edition, so a medal is news only in the edition it first appears.</summary>
    private readonly Dictionary<int, HashSet<string>> _flagsAt = new();
    /// <summary>Each edition whose trade column carried TRADE TALK, and what it talked of.</summary>
    private readonly Dictionary<int, (string Id, string Band)> _talk = new();

    /// <summary>"POLICE COURT.—…" → "POLICE COURT".</summary>
    private static string Heading(string t)
    {
        var i = t.IndexOf(".—", StringComparison.Ordinal);
        return i > 0 ? t[..i] : "";
    }

    /// <summary>
    /// The month's standing matter: prose items the paper has gone longest without printing (in
    /// the game's shuffled order among equals), no two under one heading, with the routine news
    /// of the day set between them. Every prose item chosen is counted as printed this edition.
    /// </summary>
    private List<string> Standing(PaperFacts f, int slots)
    {
        var pool = _copy.GetProperty("standing").EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String || Fits(x, f, DateCond(f)))
            .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() ?? "" : x.GetProperty("text").GetString() ?? "")
            .Distinct()
            .OrderBy(x => Hash(f.Seed, 0, x)).ToList();
        var ordered = pool
            .Select((t, i) => (Text: t, Order: (i - f.Turn * Rotation % Math.Max(1, pool.Count) + pool.Count) % Math.Max(1, pool.Count)))
            .OrderBy(x => _printed.TryGetValue(x.Text, out var at) ? at : int.MinValue)
            .ThenBy(x => x.Order)
            .Select(x => x.Text).ToList();
        var routine = Routine(f);
        var proseWant = Math.Max(0, slots - Math.Min(RoutineSlots, routine.Count));
        var prose = new List<string>();
        var heads = new HashSet<string>();
        foreach (var t in ordered)
        {
            if (prose.Count >= proseWant) break;
            var h = Heading(t);
            if (h.Length > 0 && !heads.Add(h)) continue;
            prose.Add(t);
        }
        // the routine news, unless a prose item already stands under its heading
        var news = routine.Where(r => !heads.Contains(Heading(r))).Take(slots - prose.Count).ToList();
        // a short month for routine news: more prose
        foreach (var t in ordered)
        {
            if (prose.Count + news.Count >= slots) break;
            if (prose.Contains(t)) continue;
            var h = Heading(t);
            if (h.Length > 0 && !heads.Add(h)) continue;
            prose.Add(t);
        }
        foreach (var t in prose) { _printed[t] = f.Turn; _proseStanding.Add(t); }
        // set the news between the prose, so a column is not all ships and weather
        var list = new List<string>();
        for (int i = 0, j = 0; i < prose.Count || j < news.Count;)
        {
            if (i < prose.Count) list.Add(prose[i++]);
            if (j < news.Count && (list.Count % 2 == 1 || i >= prose.Count)) list.Add(news[j++]);
        }
        return list;
    }

    /// <summary>
    /// What the edition's date allows a seasonal item to say happened: the paper comes out on the
    /// last Saturday of the month, which in some years falls before Thanksgiving (the last Thursday
    /// of November) or Christmas.
    /// </summary>
    private static Func<string, bool> DateCond(PaperFacts f)
    {
        var date = f.FirstEdition ? FirstSaturday(f.Year, f.Month) : PublicationDate(f.Year, f.Month);
        return c => c switch
        {
            "thanksgiving_past" => f.Month != 11 || date.Day > LastThursday(f.Year, 11).Day,
            "christmas_past" => f.Month != 12 || date.Day > 25,
            _ => false,
        };
    }

    private static DateTime LastThursday(int year, int month)
    {
        var d = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        while (d.DayOfWeek != DayOfWeek.Thursday) d = d.AddDays(-1);
        return d;
    }

    /// <summary>
    /// The copy a list offers this edition: a string always; an entry {text, from, to, months, if}
    /// only in its years and months, and only when its conditions hold of the facts.
    /// </summary>
    private static List<string> Opts(JsonElement options, PaperFacts f, Func<string, bool>? cond = null)
    {
        var list = new List<string>();
        if (options.ValueKind == JsonValueKind.String) { list.Add(options.GetString() ?? ""); return list; }
        foreach (var x in options.EnumerateArray())
        {
            if (x.ValueKind == JsonValueKind.String) list.Add(x.GetString() ?? "");
            else if (Fits(x, f, cond)) list.Add(x.GetProperty("text").GetString() ?? "");
        }
        return list;
    }

    /// <summary>Whether an entry of copy suits this edition: its years, its months and its conditions ("habit", "!up").</summary>
    private static bool Fits(JsonElement x, PaperFacts f, Func<string, bool>? cond)
    {
        if (x.ValueKind != JsonValueKind.Object) return true;
        // a year (1890), or a month ("1885-05": a ship's maiden voyage, a theatre's opening night)
        var now = f.Year * 12 + f.Month - 1;
        static int? Month(JsonElement v, bool end) => v.ValueKind switch
        {
            JsonValueKind.Number => v.GetInt32() * 12 + (end ? 11 : 0),
            JsonValueKind.String when v.GetString() is { Length: 7 } s => int.Parse(s[..4], CultureInfo.InvariantCulture) * 12 + int.Parse(s[5..7], CultureInfo.InvariantCulture) - 1,
            _ => null,
        };
        if (x.TryGetProperty("from", out var a) && Month(a, false) is int lo && lo > now) return false;
        if (x.TryGetProperty("to", out var b) && Month(b, true) is int hi && hi < now) return false;
        if (x.TryGetProperty("months", out var ms) && !ms.EnumerateArray().Any(m => m.GetInt32() == f.Month)) return false;
        if (x.TryGetProperty("if", out var c))
        {
            var all = c.ValueKind == JsonValueKind.Array ? c.EnumerateArray().Select(y => y.GetString() ?? "").ToArray() : new[] { c.GetString() ?? "" };
            foreach (var k in all)
            {
                var neg = k.StartsWith('!');
                var holds = cond != null && cond(neg ? k[1..] : k);
                if (holds == neg) return false;
            }
        }
        return true;
    }

    public static string Words(int n) => n switch
    {
        < 0 => n.ToString(CultureInfo.InvariantCulture),
        < 20 => Units[n],
        < 100 => Tens[n / 10] + (n % 10 == 0 ? "" : "-" + Units[n % 10]),
        _ => n.ToString("N0", CultureInfo.InvariantCulture),
    };

    private static string ShortName(PersonFact p)
    {
        var parts = p.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[0] + " " + SplitName(p.Name).Surname : p.Name;
    }

    /// <summary>"Mr. Elias Whitlock, retired", or "the infant child of Mrs. Halloran".</summary>
    private string Who(PaperFacts f, PersonFact p) =>
        p.Anonymous ? "a stranger" : p.Infant ? $"the infant child of {ShortName(p)}" : NameAnd(p);

    /// <summary>"Mr. Elias Whitlock, retired"; "Widow Agnes Pruitt" (the honorific already says it).</summary>
    private string NameAnd(PersonFact p)
    {
        var occ = Occupation(p);
        return p.Name.StartsWith("Widow ", StringComparison.Ordinal) && occ == "widow" ? p.Name : $"{p.Name}, {occ}";
    }

    /// <summary>The month at a glance, printed under the dateline.</summary>
    private static string Brief(PaperFacts f)
    {
        if (f.FirstEdition) return "";
        var parts = new List<string>();
        var sold = f.Sales.Sum(x => x.Bottles);
        parts.Add(sold == 0 ? "Not a bottle sold" : $"{Cap(Words(sold))} bottle{(sold == 1 ? "" : "s")} sold");
        var delta = f.CashCents - f.CashStartCents;
        parts.Add(delta == 0 ? "the books even" : $"the books {(delta > 0 ? "up" : "down")} {Money(Math.Abs(delta))}");
        // money laid out is not money lost: say how much of a fall was an outlay
        // (in every month there was an outlay, not only the months the books are down)
        if (f.InvestedCents > 0) parts[^1] += delta < 0 && f.InvestedCents <= -delta
            ? $", {Money(f.InvestedCents)} of it laid out on the business"
            : $", after {Money(f.InvestedCents)} laid out on the business";
        // money laid out on stock is still on the shelf: say so beside the cash
        var stock = f.StockValueCents - f.StockValueStartCents;
        if (Math.Abs(stock) >= 100) parts.Add($"stock on hand {(stock > 0 ? "up" : "down")} {Money(Math.Abs(stock))}");
        var people = new List<string>();
        if (f.Deaths.Count > 0) people.Add($"{Words(f.Deaths.Count)} dead");
        if (f.Inquests.Count > 0) people.Add($"{Words(f.Inquests.Count)} inquest{(f.Inquests.Count == 1 ? "" : "s")}");
        if (f.Joined.Count > 0) people.Add($"{Words(f.Joined.Count)} new customer{(f.Joined.Count == 1 ? "" : "s")}");
        if (f.Left.Count > 0) people.Add($"{Words(f.Left.Count)} {(f.Left.Count == 1 ? "customer" : "customers")} gone");
        if (people.Count > 0) parts.Add(people.Count == 1 ? people[0] : string.Join(", ", people.Take(people.Count - 1)) + " and " + people[^1]);
        return string.Join("; ", parts) + ".";
    }

    /// <summary>Each edition's price of a pine coffin, so the next can say whether it advanced.</summary>
    private readonly Dictionary<int, long> _coffinsAt = new();
    /// <summary>The druggists' prices each edition quoted, so a change can be reported as one.</summary>
    private readonly Dictionary<int, Dictionary<string, long>> _drugsAt = new();

    /// <summary>
    /// The week's deaths in the city, as the Health Department gives them (the routine news prints
    /// the same figure): the city grows, takes in Brooklyn in 1898, and the summer kills the infants.
    /// </summary>
    private static (int Deaths, int Usual) CityDeaths(PaperFacts f)
    {
        var years = f.Year - 1885;
        var basis = f.Year < 1898 ? 640 + years * 10 : 1180 + (f.Year - 1898) * 30;
        var season = f.Month is 6 or 7 or 8 ? 1.35 : f.Month is 1 or 2 or 3 ? 1.12 : 1.0;
        return ((int)(basis * season * (0.92 + Roll(f, "mort", 0, 16) / 100.0)), basis);
    }

    /// <summary>
    /// A pine coffin: dearer when the city's week was a heavy one and when the shop's own
    /// customers were among the dead; the weather box says it advanced only if it did.
    /// </summary>
    private long Coffins(PaperFacts f)
    {
        if (_coffinsAt.TryGetValue(f.Turn, out var kept)) return kept;
        var m = _copy.GetProperty("markets");
        var (n, usual) = CityDeaths(f);
        var c = m.GetProperty("coffinsBase").GetInt32() + (n - usual) * m.GetProperty("coffinsPerCityDeath").GetDouble() + f.Deaths.Count * m.GetProperty("coffinsPerDeath").GetInt32();
        return _coffinsAt[f.Turn] = (long)Math.Round(c / 5) * 5;
    }

    /// <summary>What the coffin did since the last edition: "up", "down" or "steady".</summary>
    private string CoffinMove(PaperFacts f)
    {
        var now = Coffins(f);
        if (!_coffinsAt.TryGetValue(f.Turn - 1, out var was)) return "steady";
        return now > was ? "up" : now < was ? "down" : "steady";
    }

    /// <summary>
    /// A commodity on the Exchange: its price for the year (it may carry era prices), with the
    /// season's swing and a month's drift fixed by the seed and the edition.
    /// </summary>
    private static long Quote(JsonElement c, PaperFacts f, int turnOffset)
    {
        var month = (f.Month - 1 + 12 * 4 + turnOffset) % 12 + 1;
        var year = f.Year + (int)Math.Floor((f.Month - 1 + turnOffset) / 12.0);
        double cents = c.GetProperty("base").GetDouble();
        if (c.TryGetProperty("eras", out var eras))
            foreach (var e in eras.EnumerateArray())
                if (e.GetProperty("from").GetInt32() <= year) cents = e.GetProperty("base").GetDouble();
        if (c.TryGetProperty("season", out var season) && season.TryGetProperty(month.ToString(CultureInfo.InvariantCulture), out var s))
            cents *= s.GetDouble();
        if (c.TryGetProperty("spikes", out var spikes))
            foreach (var sp in spikes.EnumerateArray())
            {
                var at = year * 12 + month - 1;
                static int Ix(string t) => int.Parse(t[..4], CultureInfo.InvariantCulture) * 12 + int.Parse(t[5..7], CultureInfo.InvariantCulture) - 1;
                if (Ix(S(sp, "from")) <= at && at <= Ix(S(sp, "to"))) cents *= sp.GetProperty("times").GetDouble();
            }
        var swing = c.TryGetProperty("swing", out var sw) ? sw.GetDouble() : 0.05;
        var drift = (Hash(f.Seed, f.Turn + turnOffset, "market." + S(c, "name")) % 2001) / 1000.0 - 1;
        cents *= 1 + swing * drift;
        var step = cents >= 500 ? 5 : 1;
        return (long)Math.Round(cents / step) * step;
    }

    private string WriteMarkets(PaperFacts f, ProductFact? product)
    {
        var m = _copy.GetProperty("markets");
        var parts = new List<string> { Pick(f, "market.lead", m.GetProperty("lead")) };
        var words = m.GetProperty("moves");
        string Move(long now, long? was, string key) =>
            was is not long w ? Pick(f, key + ".quoted", words.GetProperty("quoted"))
            : now * 100 > w * 102 ? Pick(f, key + ".up", words.GetProperty("up"))
            : now * 100 < w * 98 ? Pick(f, key + ".down", words.GetProperty("down"))
            : Pick(f, key + ".steady", words.GetProperty("steady"));
        // the remedy's own quotation, with the change if the proprietor moved his price
        if (product != null)
            parts.Add(product.PrevPriceCents is long was && was != product.PriceCents
                ? $"{product.Name}, {(product.PriceCents > was ? "up" : "down")} to {Money(product.PriceCents)} from {Money(was)}".TrimEnd('.') + "."
                : $"{product.Name}, {Pick(f, "market.own", m.GetProperty("own"))} {Money(product.PriceCents)}".TrimEnd('.') + ".");
        // the druggists' sundries houses, at what the shop pays for them: two of the dear drugs in
        // turn, with any change (a discount won, a supplier's rise) since the last edition
        var drugs = f.Markets.ToDictionary(x => x.Name, x => x.CostCents);
        _drugsAt[f.Turn] = drugs;
        _drugsAt.TryGetValue(f.Turn - 1, out var drugsBefore);
        var usedDrugs = new HashSet<string>();
        for (var i = 0; i < 2 && i < f.Markets.Count; i++)
        {
            var name = Fresh(f, "market.drug" + i, f.Markets.Select(x => x.Name).ToList(), usedDrugs);
            var cost = drugs[name];
            long? before = drugsBefore != null && drugsBefore.TryGetValue(name, out var b) ? b : null;
            var move = before is long bb && bb != cost ? (cost > bb ? " (dearer)" : " (cheaper)") : "";
            parts.Add($"{name}, {Money(cost)} the ounce{move}.");
        }
        // the Exchange: two of the year's commodities, and coal in the cold months, ice in the hot
        var goods = m.GetProperty("commodities").EnumerateArray().Where(c => Fits(c, f, null)).ToList();
        var always = goods.Where(c => c.TryGetProperty("months", out _)).ToList();
        var rotating = goods.Except(always).ToList();
        var usedGoods = new HashSet<string>();
        var shown = new List<JsonElement>();
        for (var i = 0; i < 2 && i < rotating.Count; i++)
        {
            var name = Fresh(f, "market.good" + i, rotating.Select(c => S(c, "name")).ToList(), usedGoods);
            shown.Add(rotating.First(c => S(c, "name") == name));
        }
        shown.AddRange(always);
        foreach (var c in shown)
        {
            var now = Quote(c, f, 0);
            long? was = f.Turn > 0 ? Quote(c, f, -1) : null;
            // (the first edition has no last month to compare with: the price alone)
            parts.Add($"{S(c, "name")}, {S(c, "grade")}{Money(now)} {S(c, "unit")}" + (was is null ? "." : $", {Move(now, was, "market." + S(c, "name"))}."));
        }
        var coffins = Coffins(f);
        var cm = CoffinMove(f);
        parts.Add($"Coffins, pine, {Money(coffins)}" + (cm == "up" ? ", advancing." : cm == "down" ? ", easier." : "."));
        // the lilies: by the dead of the shop's own, and the season (Easter, the summer's funerals)
        var lilies = m.GetProperty("lilies");
        var row = lilies[Math.Min(f.Deaths.Count, lilies.GetArrayLength() - 1)];
        parts.Add(Pick(f, "market.lilies", row));
        return string.Join(" ", parts);
    }

    // ---------- helpers ----------

    private static string CutFor(PersonFact p) => Panacea.UI.Cuts.For(p.Segment, p.Female, p.Name);

    private string Fill(string template, PaperFacts f, PersonFact? p, string product)
    {
        var s = template.Replace("{product}", product);
        if (p is { Anonymous: true })
        {
            // nobody knew the name: the paper describes the body instead
            var female = p.Female || (Hash(f.Seed, 2, p.Id) & 1) == 0;
            var someone = Anonymous(p.Segment, female);
            s = s.Replace("{someone}", someone)
                .Replace("{name}", "an unknown " + someone.Split(' ', 2)[1])
                .Replace("{surname}", "UNKNOWN")
                .Replace("{pronounCap}", female ? "She" : "He")
                .Replace("{pronoun}", female ? "she" : "he")
                .Replace("{object}", female ? "her" : "him")
                .Replace("{possessive}", female ? "her" : "his")
                .Replace("{occupation}", someone.Split(' ', 2)[1]);
        }
        else if (p != null)
        {
            var (given, surname) = SplitName(p.Name);
            var pronoun = p.Female ? "she" : "he";
            var occupation = Occupation(p);
            s = s.Replace("{name}", p.Name)
                .Replace("{surname}", surname.ToUpperInvariant())
                .Replace("{given}", given)
                .Replace("{occupation}", occupation)
                .Replace("{ailment}", p.Ailment.ToLowerInvariant())
                .Replace("{age}", ((p.Segment == "elderly" ? 61 : 22) + (int)(Hash(f.Seed, 0, p.Id) % (p.Segment == "elderly" ? 24u : 40u))).ToString(CultureInfo.InvariantCulture))
                .Replace("{months}", (2 + (int)(Hash(f.Seed, 1, p.Id) % 14)).ToString(CultureInfo.InvariantCulture))
                .Replace("{pronounCap}", Cap(pronoun))
                .Replace("{pronoun}", pronoun)
                .Replace("{object}", p.Female ? "her" : "him")
                .Replace("{possessive}", p.Female ? "her" : "his");
        }
        // "Widow Agnes Pruitt, widow": the honorific already says it
        if (p != null && p.Name.StartsWith("Widow ", StringComparison.Ordinal)) s = s.Replace("{name}, widow", "{name}").Replace(p.Name + ", widow", p.Name);
        s = s.Replace("{street}", Home(f, p))
            .Replace("{weekday}", Pick(f, "day" + (p?.Id ?? ""), _copy.GetProperty("weekdays")))
            .Replace("{coroner}", Coroner(f))
            .Replace("{cash}", Money(f.CashCents));
        return s;
    }

    private Clipping PersonClip(PaperFacts f, PersonFact p, string kicker, params string[] extra)
    {
        var c = new Clipping { Kicker = kicker, Cut = CutFor(p) };
        if (p.Anonymous)
        {
            var female = p.Female || (Hash(f.Seed, 2, p.Id) & 1) == 0;
            c.Title = "Name Unknown";
            c.Lines.Add(Cap(Anonymous(p.Segment, female)) + ". Nobody at the shop knew the name.");
        }
        else
        {
            // an infant's medicine is bought by its mother: she is the customer, the child the
            // one the paper writes about once it has died
            var child = p.Infant && kicker is "IN MEMORIAM" or "THE INQUEST";
            c.Title = child ? $"The Infant of {ShortName(p)}" : p.Name;
            var ailment = string.IsNullOrEmpty(p.Ailment) ? "" : p.Ailment.ToLowerInvariant();
            if (child) c.Lines.Add($"Infant child of {ShortName(p)}, {Occupation(p)}" + (ailment.Length > 0 ? $"; given the remedy for {ailment}." : "."));
            else if (p.Infant) c.Lines.Add(Cap(Occupation(p)) + (ailment.Length == 0 ? ", who buys at the shop for her infant." : ailment.StartsWith("a ") ? $", who buys at the shop for {ailment}." : $", who buys at the shop for her infant's {ailment}."));
            else if (p.Name.StartsWith("Widow ", StringComparison.Ordinal) && Occupation(p) == "widow") c.Lines.Add(ailment.Length > 0 ? $"Came to the shop for {ailment}." : "A customer of the shop.");
            else c.Lines.Add(Cap(Occupation(p)) + (ailment.Length > 0 ? $", who came to the shop for {ailment}." : "."));
            if (child)
            {
                // the infant was no customer: its mother bought for it
                if (p.Months > 0) c.Lines.Add($"Its mother bought the remedy for it for {p.Months} month{(p.Months == 1 ? "" : "s")}.");
            }
            else
            {
                c.Lines.Add(kicker == "A NEW CUSTOMER" ? "New to the shop this month." : p.Months switch
                {
                    < 0 => "Not one of your regulars, so far as the books show.",
                    0 => "New to the shop this month.",
                    1 => "A regular for a month.",
                    _ => $"A regular for {p.Months} months.",
                });
                if (!string.IsNullOrEmpty(p.LastSeen)) c.Lines.Add($"Last seen at the counter: {p.LastSeen}.");
            }
        }
        // the dead: the cause as the ward and the register give it, after who they were
        if (p.Cause.Length > 0)
        {
            var ail = p.Ailment.ToLowerInvariant();
            c.Lines.Add(p.Cause switch
            {
                "overdose" => "Died of an overdose, the register says.",
                "wasting" => "Died of a wasting; the physician spoke of metals.",
                "ailment" => ail.Length > 0 && !ail.StartsWith("a ", StringComparison.Ordinal) ? $"Died of the {ail} that brought {(p.Female ? "her" : "him")} to the shop." : "Died of the complaint that brought the family to the shop.",
                "old_age" => "Died of old age, the register says.",
                _ => "Died of natural causes, the register says.",
            });
        }
        c.Lines.AddRange(extra);
        return c;
    }

    private static Clipping BooksClip(PaperFacts f)
    {
        var c = new Clipping { Kicker = "THE LEDGER", Title = "The Month's Books", Cut = "wc_death" };
        if (f.FirstEdition)
        {
            c.Lines.Add($"The shop opens with {Money(f.CashCents)} in hand.");
            return c;
        }
        c.Lines.Add($"Opened the month with {Money(f.CashStartCents)}.");
        foreach (var (label, cents) in f.CashLines.OrderByDescending(x => x.Cents))
            c.Lines.Add($"{(cents >= 0 ? "+" : "−")}{Money(Math.Abs(cents))}  ·  {label}");
        c.Lines.Add($"Closed the month with {Money(f.CashCents)}.");
        foreach (var (label, amount) in f.HeatLines.Where(x => Math.Abs(x.Amount) >= 0.5))
            c.Lines.Add($"Suspicion in the ward {(amount > 0 ? "rose" : "fell")} {Math.Abs(amount):0}  ·  {label}");
        c.Lines.Add("The silent partner keeps a second set of books. They are not shown.");
        return c;
    }

    private string Anonymous(string segment, bool female)
    {
        var all = _copy.GetProperty("anonymous");
        if (!all.TryGetProperty(segment, out var forms)) forms = all.GetProperty("_");
        if (forms.TryGetProperty(female ? "female" : "male", out var x)) return x.GetString() ?? "a stranger";
        foreach (var any in forms.EnumerateObject()) return any.Value.GetString() ?? "a stranger";
        return "a stranger";
    }

    private string Occupation(PersonFact p)
    {
        if (_copy.GetProperty("occupations").TryGetProperty(p.Segment, out var occ))
        {
            if (occ.TryGetProperty(p.Female ? "female" : "male", out var o)) return o.GetString() ?? "";
            foreach (var any in occ.EnumerateObject()) return any.Value.GetString() ?? "";
        }
        return "resident";
    }

    private static (string Given, string Surname) SplitName(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return ("", "");
        // "Van Wyck", "De Groot": a particle belongs to the surname
        if (parts.Length >= 3 && parts[^2] is "Van" or "De" or "Von" or "Del" or "Di")
            return (parts.Length >= 4 ? parts[1] : parts[0], parts[^2] + " " + parts[^1]);
        return (parts.Length >= 3 ? parts[1] : parts[0], parts[^1]);
    }

    /// <summary>Splits "SURNAME.—rest" so the surname can be set in small capitals.</summary>
    private static void SplitLead(string text, out string lead, out string rest)
    {
        var m = Regex.Match(text, "^([A-Z'’ ]+\\.—)(.*)$", RegexOptions.Singleline);
        if (m.Success) { lead = m.Groups[1].Value; rest = m.Groups[2].Value; }
        else { lead = ""; rest = text; }
    }

    /// <summary>The Gazette's compositor at work: one pair of letters in the surname swapped.</summary>
    private static string Misspell(string name)
    {
        var (_, surname) = SplitName(name);
        if (surname.Length < 4) return name;
        var i = surname.Length / 2;
        var wrong = surname[..(i - 1)] + surname[i] + surname[i - 1] + surname[(i + 1)..];
        return name[..name.LastIndexOf(surname, StringComparison.Ordinal)] + wrong;
    }

    /// <summary>The ward's coroner: one man for the whole game, not a new one every month.</summary>
    private string Coroner(PaperFacts f)
    {
        var c = _copy.GetProperty("coroners");
        return c[(int)(Hash(f.Seed, 0, "coroner") % (uint)c.GetArrayLength())].GetString() ?? "";
    }

    /// <summary>Like Pick, but never the same variant twice in one edition while others remain.</summary>
    private string PickUnused(PaperFacts f, string key, JsonElement options, HashSet<string> used, Func<string, bool>? cond = null) =>
        Pick(f, key, options, cond, used);

    /// <summary>
    /// The variant of a piece of copy for this edition: of those that suit the year, the month and
    /// the facts (<paramref name="cond"/>), the one longest unprinted. If none suits, one that asks
    /// nothing of the facts.
    /// </summary>
    private string Pick(PaperFacts f, string key, JsonElement options, Func<string, bool>? cond = null, HashSet<string>? used = null)
    {
        if (options.ValueKind == JsonValueKind.String) return options.GetString() ?? "";
        var list = Opts(options, f, cond);
        if (list.Count == 0)
            list = options.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String || !x.TryGetProperty("if", out _))
                .Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() ?? "" : x.GetProperty("text").GetString() ?? "").ToList();
        if (list.Count == 0)
            list = options.EnumerateArray().Select(x => x.ValueKind == JsonValueKind.String ? x.GetString() ?? "" : x.GetProperty("text").GetString() ?? "").Take(1).ToList();
        return Fresh(f, key, list, used);
    }

    /// <summary>A deck line: fixed, or the freshest of its variants that suits the facts.</summary>
    private string Deck(PaperFacts f, string key, JsonElement line, Func<string, bool>? cond = null, bool rested = false) =>
        line.ValueKind == JsonValueKind.String ? line.GetString() ?? ""
        // a deck that has run in the last two years is left out: a headline may stand on fewer decks
        : rested ? PickRested(f, key, line, cond) ?? ""
        : Pick(f, key, line, cond);

    /// <summary>The decks that are left, or, if every one has run lately, the freshest of the first.</summary>
    private List<string> Decks(PaperFacts f, string key, JsonElement decks, Func<string, bool>? cond)
    {
        var kept = decks.EnumerateArray().Select((x, i) => Deck(f, key + i, x, cond, rested: true)).Where(x => x.Length > 0).ToList();
        if (kept.Count == 0 && decks.GetArrayLength() > 0) kept.Add(Deck(f, key + "0.any", decks[0], cond));
        return kept;
    }

    /// <summary>When each piece of copy was last printed this game, and what each month's picks were.</summary>
    private readonly Dictionary<string, int> _lastUsed = new();
    private readonly Dictionary<string, string> _memo = new();

    /// <summary>
    /// Picks the piece of copy the paper has gone longest without printing (one never printed
    /// first, from a seeded starting point), so a phrase is not reprinted while the list holds
    /// another. The month's pick is remembered: the edition is written twice (for the press run
    /// and for the reading) and must come out the same both times.
    /// </summary>
    private string Fresh(PaperFacts f, string key, IReadOnlyList<string> options, HashSet<string>? used = null)
    {
        if (options.Count == 0) return "";
        var memoKey = $"{f.Turn}:{key}";
        if (_memo.TryGetValue(memoKey, out var kept)) { used?.Add(kept); return kept; }
        var n = options.Count;
        var start = (int)(Hash(f.Seed, f.Turn, key) % (uint)n);
        string? best = null;
        var bestTurn = int.MaxValue;
        for (var k = 0; k < n; k++)
        {
            var t = options[(start + k) % n];
            if (used != null && used.Contains(t)) continue;
            // (a rewrite of the month never gets here: the memo answers it)
            var last = _lastUsed.TryGetValue(t, out var at) ? at : int.MinValue;
            if (last < bestTurn) { best = t; bestTurn = last; }
            if (last == int.MinValue) break;
        }
        best ??= options[start];
        _lastUsed[best] = f.Turn;
        _memo[memoKey] = best;
        used?.Add(best);
        return best;
    }

    /// <summary>
    /// How many editions must pass before a joke, a headline or a seasonal line is printed again:
    /// two years of the paper.
    /// </summary>
    public const int Gap = 24;

    /// <summary>When a piece of copy was last printed this game, if it has been.</summary>
    private int? LastPrinted(string text) => _lastUsed.TryGetValue(text, out var at) ? at : null;

    /// <summary>Whether a piece of copy may be printed this edition: never yet, or not within <see cref="Gap"/> editions.</summary>
    private bool Rested(PaperFacts f, string text) => LastPrinted(text) is not int at || at == f.Turn || f.Turn - at >= Gap;

    /// <summary>
    /// Like <see cref="Fresh"/>, but only a piece of copy the paper has not printed in the last
    /// <see cref="Gap"/> editions: null when every one has been, so the caller can print something
    /// else (a want of the day, an item without its remark) rather than repeat a joke.
    /// </summary>
    private string? FreshRested(PaperFacts f, string key, IReadOnlyList<string> options, HashSet<string>? used = null)
    {
        var memoKey = $"{f.Turn}:{key}";
        if (_memo.TryGetValue(memoKey, out var kept)) { if (kept.Length > 0) used?.Add(kept); return kept.Length > 0 ? kept : null; }
        var rested = options.Where(t => (used == null || !used.Contains(t)) && Rested(f, t)).ToList();
        if (rested.Count == 0) { _memo[memoKey] = ""; return null; }
        return Fresh(f, key, rested, used);
    }

    /// <summary><see cref="Pick"/>, among the variants that suit the facts and have rested: null if none has.</summary>
    private string? PickRested(PaperFacts f, string key, JsonElement options, Func<string, bool>? cond = null, HashSet<string>? used = null) =>
        options.ValueKind == JsonValueKind.String ? options.GetString() : FreshRested(f, key, Opts(options, f, cond), used);

    private static string S(JsonElement e, string name) => e.GetProperty(name).GetString() ?? "";

    private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    /// <summary>"A", "A and B", "A, B and C".</summary>
    private static string List(IReadOnlyList<string> items) =>
        items.Count <= 1 ? string.Join("", items) : string.Join(", ", items.Take(items.Count - 1)) + " and " + items[^1];

    /// <summary>"60c.", "$4.19"; a debt is printed as what is owed, "$4.19 owing".</summary>
    public static string Money(long cents) =>
        cents < 0 ? Money(-cents) + " owing"
        : cents < 100 ? $"{cents}c." : "$" + (cents / 100m).ToString("#,0.00", CultureInfo.InvariantCulture);

    /// <summary>FNV-1a over seed, turn and key: stable across runs and platforms.</summary>
    private static uint Hash(string seed, int turn, string key)
    {
        var h = 2166136261u;
        foreach (var ch in $"{seed}|{turn}|{key}")
        {
            h ^= ch;
            h *= 16777619u;
        }
        return h;
    }

    public static DateTime FirstSaturday(int year, int month)
    {
        var d = new DateTime(year, month, 1);
        while (d.DayOfWeek != DayOfWeek.Saturday) d = d.AddDays(1);
        return d;
    }

    /// <summary>The paper comes out on the last Saturday of the month.</summary>
    public static DateTime PublicationDate(int year, int month)
    {
        var d = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        while (d.DayOfWeek != DayOfWeek.Saturday) d = d.AddDays(-1);
        return d;
    }

    public static string Roman(int n)
    {
        var map = new (int, string)[] { (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"), (50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I") };
        var s = "";
        foreach (var (v, r) in map) while (n >= v) { s += r; n -= v; }
        return s;
    }
}
