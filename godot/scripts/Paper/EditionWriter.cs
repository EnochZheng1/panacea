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
public sealed class EditionWriter
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
        e.EarLeft = Pick(f, "weather", _copy.GetProperty("weather").GetProperty(f.Month.ToString(CultureInfo.InvariantCulture)));
        e.EarRightTitle = S(mast, "price");
        e.EarRight = S(mast, "priceNote");

        var date = f.FirstEdition ? FirstSaturday(f.Year, f.Month) : PublicationDate(f.Year, f.Month);
        e.DatelineLeft = $"VOL. {Roman(f.Year - 1871 + 1)}.—NO. {600 + f.Turn * 4}.";
        e.DatelineCentre = $"NEW-YORK, {date.DayOfWeek.ToString().ToUpperInvariant()}, {date.ToString("MMMM", CultureInfo.InvariantCulture).ToUpperInvariant()} {date.Day}, {date.Year}.";
        e.DatelineRight = S(mast, "yearly");
        e.Brief = Brief(f);
        e.BriefClip = f.FirstEdition ? null : BooksClip(f);

        var product = f.Products.FirstOrDefault();
        var productName = product?.Name ?? "Dr. ——'s remedies";
        var sec = _copy.GetProperty("sections");

        // ---- the lead: your advertisement dressed as news, or the news itself ----
        e.Lead = WriteLead(f, product);
        // standing matter, shuffled by the edition, for the make-up to balance the columns with
        var standing = _copy.GetProperty("standing").EnumerateArray().Select(x => x.GetString() ?? "").ToList();
        e.Standing = standing.OrderBy(x => Hash(f.Seed, f.Turn, x)).ToList();

        // ---- left column: the consequences ----
        if (f.Deaths.Count > 0)
        {
            var deaths = new Section { Title = S(sec, "deaths") };
            foreach (var d in f.Deaths)
            {
                var key = d.Anonymous ? "deathAnonymous" : d.Infant ? "deathInfant" : "deathSelf";
                var text = Fill(Pick(f, key + d.Id, _copy.GetProperty(key)), f, d, productName);
                SplitLead(text, out var lead, out var rest);
                var q = f.Inquests.FirstOrDefault(x => x.Person.Id == d.Id);
                var extra = new List<string>();
                if (d.Infant && !d.Anonymous) extra.Add("The medicine was for an infant in the family's care.");
                extra.Add(q != null ? $"The coroner's jury found: \u201c{q.Verdict}.\u201d" : "No inquest has been called. Yet.");
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
                if (q.Person.Infant && !q.Person.Anonymous) template = template.Replace("{name}", "the infant child of {name}");
                var text = Fill(template, f, q.Person, q.Product ?? productName)
                    .Replace("{verdict}", q.Verdict);
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
        foreach (var p in f.Joined)
        {
            var template = f.FirstEdition ? S(_copy, "regularsIntro") : PickUnused(f, "joined" + p.Id, _copy.GetProperty("joined"), usedJoined);
            personal.Blocks.Add(new Block { Text = Fill(template, f, p, productName), Clip = PersonClip(f, p, "A NEW CUSTOMER", $"Looking for something for {p.Ailment.ToLowerInvariant()}.") });
        }
        var drifted = f.Left.Where(l => l.Reason == "drifted").ToList();
        if (drifted.Count > 2)
        {
            var names = drifted.Select(l => l.Person.Name).ToList();
            personal.Blocks.Add(new Block { Text = string.Join(", ", names.Take(names.Count - 1)) + " and " + names[^1] + " have not called at the shop this month. The proprietor has noticed." });
        }
        foreach (var l in f.Left.Where(l => drifted.Count <= 2 || l.Reason != "drifted"))
        {
            var key = l.Reason switch { "recovered" => "leftRecovered", "dissatisfied" => "leftDissatisfied", "bereaved" => "leftBereaved", _ => "leftDrifted" };
            var why = l.Reason switch
            {
                "recovered" => "Got better on their own. It happens; the trade does not advertise it.",
                "dissatisfied" => "Felt it did nothing for them, and said so. Something stronger, or something cheaper, might have kept them.",
                "bereaved" => "Someone in the household died. The family has no further need of the remedy.",
                _ => "Simply stopped coming. Perhaps a rival's bottle; perhaps the price.",
            };
            personal.Blocks.Add(new Block { Text = Fill(Pick(f, key + l.Person.Id, _copy.GetProperty(key)), f, l.Person, productName), Clip = PersonClip(f, l.Person, "GONE FROM THE SHOP", why) });
        }
        if (personal.Blocks.Count > 0) e.Left.Add(personal);

        var notices = new Section { Title = S(sec, "notices") };
        if (f.ShutdownMonthsLeft > 0)
            notices.Blocks.Add(new Block { Kind = BlockKind.Notice, Text = Fill(Pick(f, "shutdown", _copy.GetProperty("shutdown")), f, null, productName).Replace("{within}", f.ShutdownMonthsLeft == 1 ? "at the month's end" : $"in {Words(f.ShutdownMonthsLeft)} months") });
        foreach (var (prod, bottles) in f.BadBatches)
            notices.Blocks.Add(new Block { Text = Fill(Pick(f, "bad", _copy.GetProperty("badBatch")), f, null, prod).Replace("{bottles}", bottles.ToString(CultureInfo.InvariantCulture)) });
        if (f.BottlesSeized > 0)
            notices.Blocks.Add(new Block { Text = Fill(Pick(f, "seized", _copy.GetProperty("stockSeized")), f, null, productName).Replace("{bottles}", f.BottlesSeized.ToString(CultureInfo.InvariantCulture)) });
        if (f.AdsLapsed)
            notices.Blocks.Add(new Block { Text = Fill(Pick(f, "lapsed", _copy.GetProperty("adsLapsed")), f, null, productName) });
        foreach (var (id, title, description) in f.Unlocks)
            notices.Blocks.Add(new Block
            {
                // the paper reports the trade's gossip about a third party; the cutting tells you
                Text = _copy.GetProperty("unlockNotices").TryGetProperty(id, out var said)
                    ? Fill(said.GetString() ?? "", f, null, productName)
                    : Pick(f, "unlock", _copy.GetProperty("unlock")).Replace("{title}", title.ToUpperInvariant()).Replace("{description}", description),
                Clip = new Clipping { Kicker = "A NEW CAPABILITY", Title = title, Cut = "wc_proprietor", Lines = { description, "Earned this month. It is yours to use from the next." } },
            });
        if (notices.Blocks.Count > 0) e.Left.Add(notices);

        // a quiet month still fills the column: the classifieds
        var used = e.Left.Sum(x => x.Blocks.Count);
        if (used < 6)
        {
            var wants = new Section { Title = S(sec, "classified") };
            var all = _copy.GetProperty("classifieds");
            var n = all.GetArrayLength();
            var start = (int)(Hash(f.Seed, f.Turn, "wants") % (uint)n);
            for (var i = 0; i < Math.Min(n, 6 - used + 1); i++)
                wants.Blocks.Add(new Block { Text = all[(start + i) % n].GetString() ?? "" });
            e.Left.Add(wants);
        }

        // ---- under the lead: the trade, and the era ----
        var trade = new Section { Title = S(sec, "trade") };
        var books = BooksClip(f);
        if (f.Sales.Count == 0 && !f.FirstEdition)
            trade.Blocks.Add(new Block { Text = Pick(f, "nosales", _copy.GetProperty("tradeNoSales")), Clip = books });
        foreach (var s in f.Sales)
        {
            var price = f.Products.FirstOrDefault(p => p.Name == s.Product)?.PriceCents ?? 0;
            trade.Blocks.Add(new Block
            {
                Text = Pick(f, "sales" + s.Product, _copy.GetProperty("tradeSales"))
                    .Replace("{product}", s.Product)
                    .Replace("{bottles}", s.Bottles.ToString(CultureInfo.InvariantCulture))
                    .Replace("{price}", Money(price))
                    // selling out is money left on the counter: say so
                    + (f.Products.FirstOrDefault(p => p.Name == s.Product) is { Stock: 0 } && s.Bottles > 0
                        ? " The shelf was bare before the month was out, and callers went away empty-handed."
                        : ""),
                Clip = books,
            });
        }
        if (f.TurnedAwayByPrice > 0)
            trade.Blocks.Add(new Block { Text = $"Callers who asked the price and went away: {Words(f.TurnedAwayByPrice)}.", Clip = books });
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
                Text = product.ClaimNames.Count > 0 ? string.Join(" · ", product.ClaimNames).ToUpperInvariant() : "FOR ALL COMPLAINTS",
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
        var rivals = _copy.GetProperty("rivals").EnumerateArray().ToList();
        var first = (int)(Hash(f.Seed, f.Turn, "rivals") % (uint)rivals.Count);
        for (var i = 0; i < 2; i++)
        {
            var r = rivals[(first + i) % rivals.Count];
            ads.Blocks.Add(new Block
            {
                Kind = BlockKind.Ad,
                Head = S(r, "head"),
                Mid = S(r, "mid"),
                Text = S(r, "body"),
                Fine = S(r, "fine"),
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
                Headline = S(x, "headline"),
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
                Headline = "THE CORONER NAMES A REMEDY",
                Decks = new List<string> { "A Verdict Against the Bottle.", $"THE JURY FINDS THAT {Who(f, named.Person).ToUpperInvariant()}: \u201c{named.Verdict.ToUpperInvariant()}.\u201d", "The Board of Health Is Informed." },
                Cut = "wc_death",
                Cutline = "A GENTLEMAN SEEN AT THE INQUEST.",
                Body = $"The bottle found at the bedside bore the label of {named.Product ?? product?.Name ?? "a proprietary medicine"}. Whatever its proprietor had to say for the purity of his preparations did not detain the jury long. Physicians observe that the remedy is purely vegetable, and that so is hemlock.",
                Clip = PersonClip(f, named.Person, "THE INQUEST", $"Verdict: \u201c{named.Verdict}.\u201d", "Your product is named in the findings. The Board of Health will take an interest, and the ward will talk."),
            };
        }
        // An advertisement dressed as news, when the player is paying for space.
        if (product != null && f.CanAdvertise && AdSpace.TierFor(f.AdSpendCents) == AdSpace.Front)
        {
            var rn = _copy.GetProperty("readingNotice");
            var subject = f.Enthusiasts.FirstOrDefault();
            var headline = product.ClaimNames.Count > 0 ? product.ClaimNames[0].ToUpperInvariant() + "!" : "NEVER FELT BETTER!";
            var decks = rn.GetProperty("decks").EnumerateArray().Select(x => x.GetString() ?? "").ToList();
            if (subject == null) decks = new List<string> { decks[0], decks[2] };
            var body = subject != null ? Pick(f, "rn", rn.GetProperty("bodies")) : S(rn, "fallbackBody");
            var street = Home(f, subject);
            string Sub(string s) => Fill(s, f, subject, product.Name)
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
        if (f.Deaths.Count >= 3)
        {
            return new Story
            {
                Headline = "A SICKLY SEASON",
                Decks = new List<string> { "The Ward Mourns.", $"{Words(f.Deaths.Count).ToUpperInvariant()} DEATHS IN THE WARD THIS MONTH; UNDERTAKERS REPORT A BRISK TRADE." },
                Cut = "wc_death",
                Cutline = "A GENTLEMAN SEEN IN THE NEIGHBORHOOD.",
                Body = "The deaths column runs long this month. Physicians attribute the trouble to the weather, the water and the foreign element, in that order. Nobody has thought to look at the bottles.",
                Clip = BooksClip(f),
            };
        }
        if (f.Deaths.Count > 0)
        {
            var d = f.Deaths[0];
            var street = Home(f, d);
            return new Story
            {
                Headline = d.Anonymous ? $"FOUND DEAD IN {street.ToUpperInvariant().Replace("THE ", "")}"
                    : d.Infant ? $"THE {SplitName(d.Name).Surname.ToUpperInvariant()} INFANT"
                    : $"DEATH OF {ShortName(d).ToUpperInvariant()}",
                Decks = new List<string>
                {
                    d.Infant && !d.Anonymous ? "An Infant Taken." : "In the Ward.",
                    d.Anonymous ? "A GREEN BOTTLE WAS FOUND NEAR THE BODY. THE CORONER HAS BEEN INFORMED." : $"{Who(f, d).ToUpperInvariant()}, WELL KNOWN AT THE SHOP ON MULBERRY STREET.",
                },
                Cut = d.Anonymous ? "wc_death" : CutFor(d),
                Cutline = d.Anonymous ? "A GENTLEMAN SEEN IN THE NEIGHBORHOOD." : d.Infant ? $"THE MOTHER, {ShortName(d).ToUpperInvariant()}.—From a stock likeness." : $"{ShortName(d).ToUpperInvariant()}.—From a stock likeness.",
                Body = d.Anonymous ? Pick(f, "unknownLead" + d.Id, _copy.GetProperty("unknownLead"))
                    : d.Infant ? Pick(f, "infantLead" + d.Id, _copy.GetProperty("infantLead"))
                    : Fill(Pick(f, "deathLead" + d.Id, _copy.GetProperty("deathLead")), f, d, product?.Name ?? "the remedy"),
                Clip = PersonClip(f, d, "IN MEMORIAM", d.Infant && !d.Anonymous ? "The medicine was for an infant in the family's care." : "See the deaths column for the notice."),
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
        var delta = f.CashCents - f.CashStartCents;
        var bribed = f.Inquests.FirstOrDefault(q => q.Outcome == "bribed");
        var lost = f.Left.Where(l => l.Reason != "bereaved").ToList();
        // every lead the month could carry, most pressing first
        var soldOut = sold > 0 && f.Products.Count > 0 && f.Products.All(p => p.Stock == 0);
        var record = f.SoldLastMonth >= 6 && sold * 10 >= f.SoldLastMonth * 13 && delta > 0;
        var slump = delta < 0 || (f.SoldLastMonth >= 6 && sold * 3 < f.SoldLastMonth * 2);
        var options = new List<string>();
        if (bribed != null) options.Add("bribed");
        if (f.CashCents < 0) options.Add("arrears");
        if (sold == 0) options.Add("nothing");
        if (soldOut) options.Add("shortage");
        // the price drove callers off: say so, rather than blame the weather
        if (f.TurnedAwayByPrice >= 5 && f.TurnedAwayByPrice * 4 >= sold) options.Add("dear");
        if (record) options.Add("record");
        if (slump && !soldOut) options.Add("dull");
        if (f.Joined.Count >= 3 || (f.Joined.Count >= 2 && f.Joined.Count > lost.Count)) options.Add("growing");
        if (lost.Count >= 2 && lost.Count > f.Joined.Count) options.Add("losing");
        if (delta >= 0) options.Add("brisk");
        if (options.Count == 0) options.Add("dull");
        // never the same lead two editions running if the month offers another; the pressing
        // ones (a bought verdict, arrears, an idle shop, a bare shelf) follow up instead
        _leadKinds.TryGetValue(f.Turn - 1, out var last);
        var kind = options[0];
        if (kind == last && kind is not ("bribed" or "arrears" or "nothing" or "shortage"))
            kind = options.FirstOrDefault(o => o != last) ?? kind;
        var again = kind == last;
        _leadKinds[f.Turn] = kind;
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
        var bodies = t.GetProperty("bodies").EnumerateArray().Select(x => x.GetString() ?? "").ToList();
        // a body that names someone needs someone to name
        if (subject == null) bodies = bodies.Where(b => !b.Contains("{name}")).ToList();
        var body = bodies[(int)(Hash(f.Seed, f.Turn, "trade" + kind) % (uint)bodies.Count)];
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
                .Replace("{price}", Money(product.PriceCents));
        }
        return new Story
        {
            Headline = S(head, "headline"),
            Decks = head.GetProperty("decks").EnumerateArray().Select(x => Sub(x.GetString() ?? "")).ToList(),
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
        var people = new List<string>();
        if (f.Deaths.Count > 0) people.Add($"{Words(f.Deaths.Count)} dead");
        if (f.Inquests.Count > 0) people.Add($"{Words(f.Inquests.Count)} inquest{(f.Inquests.Count == 1 ? "" : "s")}");
        if (f.Joined.Count > 0) people.Add($"{Words(f.Joined.Count)} new customer{(f.Joined.Count == 1 ? "" : "s")}");
        if (f.Left.Count > 0) people.Add($"{Words(f.Left.Count)} {(f.Left.Count == 1 ? "customer" : "customers")} gone");
        if (people.Count > 0) parts.Add(people.Count == 1 ? people[0] : string.Join(", ", people.Take(people.Count - 1)) + " and " + people[^1]);
        return string.Join("; ", parts) + ".";
    }

    private string WriteMarkets(PaperFacts f, ProductFact? product)
    {
        var m = _copy.GetProperty("markets");
        var parts = new List<string> { S(m, "lead") };
        if (product != null) parts.Add($"{product.Name}, steady at {Money(product.PriceCents)}".TrimEnd('.') + ".");
        foreach (var (name, cost) in f.Markets.Take(4))
            parts.Add($"{name}, {Money(cost)} the ounce.");
        var coffins = m.GetProperty("coffinsBase").GetInt32() + f.Deaths.Count * m.GetProperty("coffinsPerDeath").GetInt32();
        parts.Add($"Coffins, pine, {Money(coffins)}.");
        var lilies = m.GetProperty("lilies");
        parts.Add(lilies[Math.Min(f.Deaths.Count, lilies.GetArrayLength() - 1)].GetString() ?? "");
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
            c.Lines.Add(kicker == "A NEW CUSTOMER" ? "New to the shop this month." : p.Months switch
            {
                < 0 => "Not one of your regulars, so far as the books show.",
                0 => "New to the shop this month.",
                1 => "A regular for a month.",
                _ => $"A regular for {p.Months} months.",
            });
            if (!string.IsNullOrEmpty(p.LastSeen)) c.Lines.Add($"Last seen at the counter: {p.LastSeen}.");
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

    /// <summary>The ward's coroner: one man for the whole game, not a new one every month.</summary>
    private string Coroner(PaperFacts f)
    {
        var c = _copy.GetProperty("coroners");
        return c[(int)(Hash(f.Seed, 0, "coroner") % (uint)c.GetArrayLength())].GetString() ?? "";
    }

    /// <summary>Like Pick, but never the same variant twice in one edition while others remain.</summary>
    private string PickUnused(PaperFacts f, string key, JsonElement options, HashSet<string> used)
    {
        var n = options.GetArrayLength();
        var start = (int)(Hash(f.Seed, f.Turn, key) % (uint)n);
        for (var k = 0; k < n; k++)
        {
            var t = options[(start + k) % n].GetString() ?? "";
            if (used.Add(t)) return t;
        }
        return options[start].GetString() ?? "";
    }

    private string Pick(PaperFacts f, string key, JsonElement options)
    {
        if (options.ValueKind == JsonValueKind.String) return options.GetString() ?? "";
        var n = options.GetArrayLength();
        return n == 0 ? "" : options[(int)(Hash(f.Seed, f.Turn, key) % (uint)n)].GetString() ?? "";
    }

    private static string S(JsonElement e, string name) => e.GetProperty(name).GetString() ?? "";

    private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    /// <summary>"60c.", "$4.19"; a debt is printed as what is owed, "$4.19 owing".</summary>
    public static string Money(long cents) =>
        cents < 0 ? Money(-cents) + " owing"
        : cents < 100 ? $"{cents}c." : "$" + (cents / 100m).ToString("0.00", CultureInfo.InvariantCulture);

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
