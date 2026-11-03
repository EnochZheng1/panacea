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

        var date = PublicationDate(f.Year, f.Month);
        e.DatelineLeft = $"VOL. {Roman(f.Year - 1871 + 1)}.—NO. {600 + f.Turn}.";
        e.DatelineCentre = $"NEW-YORK, {date.DayOfWeek.ToString().ToUpperInvariant()}, {date.ToString("MMMM", CultureInfo.InvariantCulture).ToUpperInvariant()} {date.Day}, {date.Year}.";
        e.DatelineRight = S(mast, "yearly");

        var product = f.Products.FirstOrDefault();
        var productName = product?.Name ?? "Dr. ——'s remedies";
        var sec = _copy.GetProperty("sections");

        // ---- the lead: your advertisement dressed as news, or the news itself ----
        e.Lead = WriteLead(f, product);

        // ---- left column: the consequences ----
        if (f.Deaths.Count > 0)
        {
            var deaths = new Section { Title = S(sec, "deaths") };
            foreach (var d in f.Deaths)
            {
                var key = d.Anonymous ? "deathAnonymous" : d.Infant ? "deathInfant" : "deathSelf";
                var text = Fill(Pick(f, key + d.Id, _copy.GetProperty(key)), f, d, productName);
                SplitLead(text, out var lead, out var rest);
                deaths.Blocks.Add(new Block { Kind = BlockKind.Notice, Lead = lead, Text = rest });
            }
            e.Left.Add(deaths);
        }
        if (f.Inquests.Count > 0)
        {
            var court = new Section { Title = S(sec, "coroner") };
            foreach (var q in f.Inquests)
            {
                var key = q.Outcome switch { "bribed" => "inquestBribed", "implicated" => "inquestImplicated", _ => "inquestCleared" };
                var text = Fill(Pick(f, key + q.Person.Id, _copy.GetProperty(key)), f, q.Person, q.Product ?? productName)
                    .Replace("{verdict}", q.Verdict);
                court.Blocks.Add(new Block { Text = text });
            }
            e.Left.Add(court);
        }
        var personal = new Section { Title = S(sec, "personal") };
        foreach (var p in f.Joined)
        {
            var template = f.FirstEdition ? S(_copy, "regularsIntro") : Pick(f, "joined" + p.Id, _copy.GetProperty("joined"));
            personal.Blocks.Add(new Block { Text = Fill(template, f, p, productName) });
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
            personal.Blocks.Add(new Block { Text = Fill(Pick(f, key + l.Person.Id, _copy.GetProperty(key)), f, l.Person, productName) });
        }
        if (personal.Blocks.Count > 0) e.Left.Add(personal);

        var notices = new Section { Title = S(sec, "notices") };
        if (f.ShutdownMonthsLeft > 0)
            notices.Blocks.Add(new Block { Kind = BlockKind.Notice, Text = Fill(Pick(f, "shutdown", _copy.GetProperty("shutdown")), f, null, productName).Replace("{months}", f.ShutdownMonthsLeft.ToString(CultureInfo.InvariantCulture)) });
        foreach (var (prod, bottles) in f.BadBatches)
            notices.Blocks.Add(new Block { Text = Fill(Pick(f, "bad", _copy.GetProperty("badBatch")), f, null, prod).Replace("{bottles}", bottles.ToString(CultureInfo.InvariantCulture)) });
        if (f.BottlesSeized > 0)
            notices.Blocks.Add(new Block { Text = Fill(Pick(f, "seized", _copy.GetProperty("stockSeized")), f, null, productName).Replace("{bottles}", f.BottlesSeized.ToString(CultureInfo.InvariantCulture)) });
        if (f.AdsLapsed)
            notices.Blocks.Add(new Block { Text = Fill(Pick(f, "lapsed", _copy.GetProperty("adsLapsed")), f, null, productName) });
        foreach (var (title, description) in f.Unlocks)
            notices.Blocks.Add(new Block { Text = Pick(f, "unlock", _copy.GetProperty("unlock")).Replace("{title}", title.ToUpperInvariant()).Replace("{description}", description) });
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
        if (f.Sales.Count == 0 && !f.FirstEdition)
            trade.Blocks.Add(new Block { Text = Pick(f, "nosales", _copy.GetProperty("tradeNoSales")) });
        foreach (var s in f.Sales)
        {
            var price = f.Products.FirstOrDefault(p => p.Name == s.Product)?.PriceCents ?? 0;
            trade.Blocks.Add(new Block
            {
                Text = Pick(f, "sales" + s.Product, _copy.GetProperty("tradeSales"))
                    .Replace("{product}", s.Product)
                    .Replace("{bottles}", s.Bottles.ToString(CultureInfo.InvariantCulture))
                    .Replace("{price}", Money(price)),
            });
        }
        trade.Blocks.Add(new Block { Text = Pick(f, "books", _copy.GetProperty("tradeBooks")).Replace("{cash}", Money(f.CashCents)) });
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
        if (product != null)
        {
            ads.Blocks.Add(new Block
            {
                Kind = BlockKind.Ad,
                Mid = "THE WONDER OF THE AGE!",
                Head = product.Name.ToUpperInvariant(),
                Text = product.ClaimNames.Count > 0 ? string.Join(" · ", product.ClaimNames).ToUpperInvariant() : "FOR ALL COMPLAINTS",
                Fine = $"{Money(product.PriceCents)} the bottle, at the shop on Mulberry street.",
                Cut = "wc_bottle_panacea",
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
        // An advertisement dressed as news, when the player is paying for space.
        if (product != null && f.CanAdvertise && f.AdSpendCents > 0)
        {
            var rn = _copy.GetProperty("readingNotice");
            var subject = f.Enthusiasts.FirstOrDefault();
            var headline = product.ClaimNames.Count > 0 ? product.ClaimNames[0].ToUpperInvariant() + "!" : "NEVER FELT BETTER!";
            var decks = rn.GetProperty("decks").EnumerateArray().Select(x => x.GetString() ?? "").ToList();
            if (subject == null) decks = new List<string> { decks[0], decks[2] };
            var body = subject != null ? Pick(f, "rn", rn.GetProperty("bodies")) : S(rn, "fallbackBody");
            var street = Pick(f, "street" + (subject?.Id ?? ""), _copy.GetProperty("streets"));
            string Sub(string s) => Fill(s, f, subject, product.Name)
                .Replace("{street_upper}", street.ToUpperInvariant())
                .Replace("{product_upper}", product.Name.ToUpperInvariant())
                .Replace("{price}", Money(product.PriceCents));
            return new Story
            {
                Headline = headline,
                Decks = decks.Select(Sub).ToList(),
                Cut = subject == null ? "wc_bottle_panacea" : CutFor(subject),
                Cutline = subject == null ? "THE REMEDY.—Actual size." : $"{subject.Name.ToUpperInvariant()}.—From a stock likeness.",
                Body = Sub(body),
                IsAdvertisement = true,
            };
        }
        if (f.Era.Count > 0)
        {
            var (headline, body) = f.Era[0];
            return new Story { Headline = headline.ToUpperInvariant(), Body = body };
        }
        if (f.Deaths.Count >= 3)
        {
            return new Story
            {
                Headline = "A SICKLY SEASON",
                Decks = new List<string> { "The Ward Mourns.", "UNDERTAKERS REPORT A BRISK MONTH." },
                Cut = "wc_death",
                Cutline = "A GENTLEMAN SEEN IN THE NEIGHBOURHOOD.",
                Body = $"The deaths column runs long this month. Physicians attribute the trouble to the weather, the water and the foreign element, in that order. Nobody has thought to look at the bottles.",
            };
        }
        return new Story
        {
            Headline = "ALL QUIET IN THE WARD",
            Decks = new List<string> { "Nothing to Report.", "Coughs Steady." },
            Cut = "wc_clerk",
            Cutline = "A RESIDENT.",
            Body = "Our reporter walked the length of Mulberry street and found nothing worth printing. The editor has sent him back.",
        };
    }

    private string WriteMarkets(PaperFacts f, ProductFact? product)
    {
        var m = _copy.GetProperty("markets");
        var parts = new List<string> { S(m, "lead") };
        if (product != null) parts.Add($"{product.Name}, steady at {Money(product.PriceCents)}.");
        foreach (var (name, cost) in f.Markets.Take(4))
            parts.Add($"{name}, {Money(cost)} the ounce.");
        var coffins = m.GetProperty("coffinsBase").GetInt32() + f.Deaths.Count * m.GetProperty("coffinsPerDeath").GetInt32();
        parts.Add($"Coffins, pine, {Money(coffins)}.");
        var lilies = m.GetProperty("lilies");
        parts.Add(lilies[Math.Min(f.Deaths.Count, lilies.GetArrayLength() - 1)].GetString() ?? "");
        return string.Join(" ", parts);
    }

    // ---------- helpers ----------

    private static string CutFor(PersonFact p) => p.Segment switch
    {
        "mother" => "wc_mother",
        "labourer" or "dockworker" => "wc_longshoreman",
        "well_to_do" => p.Female ? "wc_lady" : "wc_proprietor",
        _ => "wc_clerk",
    };

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
                .Replace("{age}", (22 + (int)(Hash(f.Seed, 0, p.Id) % 48)).ToString(CultureInfo.InvariantCulture))
                .Replace("{months}", (2 + (int)(Hash(f.Seed, 1, p.Id) % 14)).ToString(CultureInfo.InvariantCulture))
                .Replace("{pronounCap}", Cap(pronoun))
                .Replace("{pronoun}", pronoun)
                .Replace("{object}", p.Female ? "her" : "him")
                .Replace("{possessive}", p.Female ? "her" : "his");
        }
        s = s.Replace("{street}", Pick(f, "street" + (p?.Id ?? ""), _copy.GetProperty("streets")))
            .Replace("{weekday}", Pick(f, "day" + (p?.Id ?? ""), _copy.GetProperty("weekdays")))
            .Replace("{coroner}", Pick(f, "coroner", _copy.GetProperty("coroners")))
            .Replace("{cash}", Money(f.CashCents));
        return s;
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
        return (parts.Length >= 3 ? parts[1] : parts[0], parts[^1]);
    }

    /// <summary>Splits "SURNAME.—rest" so the surname can be set in small capitals.</summary>
    private static void SplitLead(string text, out string lead, out string rest)
    {
        var m = Regex.Match(text, "^([A-Z'’ ]+\\.—)(.*)$", RegexOptions.Singleline);
        if (m.Success) { lead = m.Groups[1].Value; rest = m.Groups[2].Value; }
        else { lead = ""; rest = text; }
    }

    private string Pick(PaperFacts f, string key, JsonElement options)
    {
        if (options.ValueKind == JsonValueKind.String) return options.GetString() ?? "";
        var n = options.GetArrayLength();
        return n == 0 ? "" : options[(int)(Hash(f.Seed, f.Turn, key) % (uint)n)].GetString() ?? "";
    }

    private static string S(JsonElement e, string name) => e.GetProperty(name).GetString() ?? "";

    private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    public static string Money(long cents) =>
        cents < 100 ? $"{cents}c." : "$" + (cents / 100m).ToString("0.00", CultureInfo.InvariantCulture);

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
