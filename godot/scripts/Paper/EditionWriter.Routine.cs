using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Panacea.Paper;

/// <summary>
/// The routine news of the day, set between the standing jokes: ships in from the year's
/// lines, the forecast (the Signal Service, then the Weather Bureau), the police court, the ball
/// game between the year's clubs, the week's deaths and a fire; and the plain wants of the
/// classified column. Each is made fresh from data/paper.en.json's "routine" lists for the
/// edition's year and month, so its words change with the times and are not reprinted; only its
/// last sentence, the Gazette's remark, comes from a list, and that list is read longest-unused first.
/// </summary>
public sealed partial class EditionWriter
{
    /// <summary>A number from <paramref name="lo"/> to <paramref name="hi"/>, fixed by the seed, the edition and the key.</summary>
    private static int Roll(PaperFacts f, string key, int lo, int hi) =>
        hi <= lo ? lo : lo + (int)(Hash(f.Seed, f.Turn, key) % (uint)(hi - lo + 1));

    /// <summary>
    /// Fills a routine template: a {slot} named in <paramref name="given"/> takes that value; one
    /// named in the kind's "slots" takes the freshest of its entries for the year; {weekday} a day.
    /// </summary>
    private string Slots(PaperFacts f, string key, string template, JsonElement kind, IReadOnlyDictionary<string, string>? given = null, Func<string, bool>? cond = null)
    {
        var slots = kind.TryGetProperty("slots", out var sl) ? sl : default;
        return Regex.Replace(template, @"\{([a-zA-Z0-9_]+)\}", m =>
        {
            var name = m.Groups[1].Value;
            // {place_cap}: the slot's value, capitalized to begin a sentence
            if (name.EndsWith("_cap", StringComparison.Ordinal)) return Cap(Slots(f, key, "{" + name[..^4] + "}", kind, given, cond));
            if (given != null && given.TryGetValue(name, out var v)) return v;
            if (name == "weekday") return Pick(f, key + ".weekday", _copy.GetProperty("weekdays"));
            if (slots.ValueKind == JsonValueKind.Object && slots.TryGetProperty(name, out var list))
                return Slots(f, key + "." + name, Pick(f, key + "." + name, list, cond), kind, given, cond);
            return m.Value;
        });
    }

    /// <summary>A house number a street of the ward could have: a short street has few.</summary>
    private int HouseNumber(PaperFacts f, string key, string street)
    {
        var hi = _copy.TryGetProperty("streetNumbers", out var sn) && sn.TryGetProperty(street, out var n) ? n.GetInt32() : 290;
        return Roll(f, key, Math.Min(3, hi), hi);
    }

    /// <summary>The year's entries of a list of objects (ships, clubs) with from/to years.</summary>
    private static List<JsonElement> OfYear(JsonElement list, PaperFacts f) =>
        list.EnumerateArray().Where(x => Fits(x, f, null)).ToList();

    private static string Num(int n) => n.ToString("N0", CultureInfo.InvariantCulture);

    /// <summary>The routine news for an edition, in the order the make-up should prefer it.</summary>
    private List<string> Routine(PaperFacts f)
    {
        if (!_copy.TryGetProperty("routine", out var r)) return new List<string>();
        var list = new List<string>();
        var routineCond = RoutineCond(f);
        string Item(string kind, IReadOnlyDictionary<string, string>? given = null, Func<string, bool>? facts = null)
        {
            var k = r.GetProperty(kind);
            Func<string, bool> cond = facts == null ? routineCond : c => facts(c) || routineCond(c);
            var form = Pick(f, "routine." + kind + ".form", k.GetProperty("forms"), cond);
            // the Gazette's remark closes the item only if it has not been made in ten years, and
            // only as often as the list of remarks can bear over the game: a plain item is better
            // than a joke told twice
            var tail = k.TryGetProperty("tails", out var tails) ? PickSpared(f, "routine." + kind + ".tail", tails, cond) ?? "" : "";
            var all = new Dictionary<string, string>(given ?? new Dictionary<string, string>()) { ["tail"] = Slots(f, "routine." + kind + ".t", tail, k, given, cond) };
            return Slots(f, "routine." + kind, form, k, all, cond).Replace("  ", " ").Trim();
        }
        var years = f.Year - 1885;

        // ships in: two of the year's steamers, with the steerage the age brought
        var ships = OfYear(r.GetProperty("shipping").GetProperty("ships"), f);
        if (ships.Count >= 2)
        {
            var a = Roll(f, "ship1", 0, ships.Count - 1);
            var b = (a + 1 + Roll(f, "ship2", 0, ships.Count - 2)) % ships.Count;
            int Steerage(string key) => Roll(f, key, 180 + years * 25, 700 + years * 55) / 5 * 5 + Roll(f, key + "u", 0, 4);
            list.Add(Item("shipping", new Dictionary<string, string>
            {
                ["ship"] = S(ships[a], "ship"), ["line"] = S(ships[a], "line"), ["port"] = S(ships[a], "port"), ["n"] = Num(Steerage("n1")),
                ["ship2"] = S(ships[b], "ship"), ["line2"] = S(ships[b], "line"), ["port2"] = S(ships[b], "port"), ["n2"] = Num(Steerage("n2")),
            }));
        }

        // the forecast: the Army's Signal Service until the Weather Bureau took it over in July 1891
        var bureau = f.Year * 12 + f.Month >= 1891 * 12 + 7;
        var forecasts = r.GetProperty("weather").GetProperty("forecasts").GetProperty(f.Month.ToString(CultureInfo.InvariantCulture));
        list.Add(Item("weather", new Dictionary<string, string>
        {
            ["agency"] = bureau ? "the Weather Bureau" : "the Signal Service",
            ["Agency"] = bureau ? "The Weather Bureau" : "The Signal Service",
            ["forecast"] = Pick(f, "routine.weather.forecast", forecasts),
            ["forecast_cap"] = Cap(Pick(f, "routine.weather.forecast", forecasts)),
            // the glass at noon, as the month allows
            ["temp"] = Roll(f, "weather.temp", new[] { 8, 12, 25, 38, 50, 62, 70, 68, 58, 45, 30, 15 }[f.Month - 1], new[] { 40, 42, 55, 68, 82, 92, 98, 96, 86, 72, 58, 45 }[f.Month - 1]).ToString(CultureInfo.InvariantCulture),
        }));

        // the police court: magistrates from 1895, police justices before
        var female = Roll(f, "pc.sex", 0, 3) == 0;
        var pc = r.GetProperty("police");
        var judge = f.Year >= 1895 ? "the Magistrate" : "the Justice";
        // a given name and a surname from the same parish: the names come in families
        var families = OfYear(pc.GetProperty("names"), f)
            .SelectMany(x => Enumerable.Repeat(x, x.TryGetProperty("weight", out var w) ? w.GetInt32() : 1)).ToList();
        var family = families[Roll(f, "pc.family", 0, families.Count - 1)];
        var origin = S(family, "origin");
        // the case hangs together: a charge the prisoner's trade could bring (a truck driver drives
        // the truck, a fruit vender sells the fruit), a disposition the charge could earn (bail for a
        // pistol or a ham, a fine or the Island for the drink), and a remark that fits both (no "the
        // matter went no further than the fine" for a man held for trial)
        var offences = OfYear(pc.GetProperty("offences"), f).Where(o => !female || !o.TryGetProperty("male", out var m) || !m.GetBoolean()).ToList();
        var offence = offences.First(o => S(o, "text") == Fresh(f, "routine.police.offence", offences.Select(o => S(o, "text")).ToList()));
        var dispositions = offence.GetProperty("dispositions").EnumerateArray().Select(x => x.GetString() ?? "fine").ToList();
        var disposition = dispositions[Roll(f, "pc.disposition", 0, dispositions.Count - 1)];
        var kinds = offence.TryGetProperty("kinds", out var ks) ? ks.EnumerateArray().Select(x => x.GetString() ?? "").ToHashSet() : new HashSet<string>();
        var trades = Opts(pc.GetProperty(female ? "tradesFemale" : "tradesMale"), f);
        if (offence.TryGetProperty("trades", out var only))
        {
            var fit = only.EnumerateArray().Select(x => x.GetString() ?? "").Where(trades.Contains).ToList();
            if (fit.Count > 0) trades = fit;
        }
        Func<string, bool> caseCond = c => c == disposition || kinds.Contains(c) || (c == "female" && female);
        list.Add(Item("police", new Dictionary<string, string>
        {
            ["given"] = Pick(f, "routine.police.given." + origin + (female ? "f" : "m"), family.GetProperty(female ? "female" : "male")),
            ["surname"] = Pick(f, "routine.police.surname." + origin, family.GetProperty("surnames")),
            // the spouse in the dock's story, and what the prisoner was before the bottle
            ["spouse"] = female ? "husband" : "wife",
            ["spouseObject"] = female ? "him" : "her",
            ["man"] = female ? "woman" : "man",
            ["trade"] = Fresh(f, "routine.police.trade" + (female ? "f" : "m"), trades),
            ["offence"] = S(offence, "text"),
            ["sentence"] = Pick(f, "routine.police.sentence." + disposition, pc.GetProperty("sentences").GetProperty(disposition), caseCond),
            ["age"] = Roll(f, "pc.age", 19, 63).ToString(CultureInfo.InvariantCulture),
            ["judge"] = judge,
            ["Judge"] = Cap(judge),
            ["pronoun"] = female ? "she" : "he",
            ["Pronoun"] = female ? "She" : "He",
            ["possessive"] = female ? "her" : "his",
            ["object"] = female ? "her" : "him",
        }, caseCond));

        var extra = new List<string>();
        // the ball game, in the season, between the year's clubs
        var sp = r.GetProperty("sporting");
        if (sp.GetProperty("months").EnumerateArray().Any(m => m.GetInt32() == f.Month))
        {
            var clubs = OfYear(sp.GetProperty("clubs"), f);
            if (clubs.Count > 0)
            {
                var club = clubs[Roll(f, "club", 0, clubs.Count - 1)];
                var opps = OfYear(sp.GetProperty("opponents"), f).Where(o => S(o, "league") == S(club, "league")).ToList();
                if (opps.Count > 0)
                {
                    var opp = opps[Roll(f, "opp", 0, opps.Count - 1)];
                    var hi = Roll(f, "runs.hi", 3, 14);
                    var lo = Roll(f, "runs.lo", 0, hi - 1);
                    var won = Roll(f, "won", 0, 4) < 3;
                    var home = Roll(f, "home", 0, 1) == 0 && club.TryGetProperty("home", out var h);
                    extra.Add(Item("sporting", new Dictionary<string, string>
                    {
                        ["winner"] = won ? S(club, "name") : S(opp, "name"),
                        ["loser"] = won ? S(opp, "name") : S(club, "name"),
                        ["Winner"] = Cap(won ? S(club, "name") : S(opp, "name")),
                        ["club"] = S(club, "name"),
                        ["at"] = home ? " " + S(club, "home") : "",
                        ["hi"] = Words(hi),
                        ["lo"] = lo == 0 ? "nothing" : Words(lo),
                    }));
                }
            }
        }
        // the week's deaths, as the Health Department gives them: the city grows, and in 1898
        // takes in Brooklyn; the summer kills the infants
        {
            // (the same figure THE MARKETS prices the coffins by)
            var (n, _) = CityDeaths(f);
            var under5 = (int)(n * ((f.Month is 6 or 7 or 8 ? 0.46 : 0.36) - years * 0.003));
            var consumption = (int)(n * (0.11 + Roll(f, "mort.c", 0, 3) / 100.0));
            extra.Add(Item("mortality", new Dictionary<string, string>
            {
                ["n"] = Num(n), ["under5"] = Num(under5), ["consumption"] = Num(consumption),
            }));
        }
        // a fire, of no great size, in a street the slot chooses, at a number the street has
        {
            var fs = r.GetProperty("fire").GetProperty("slots").GetProperty("street");
            var street = Pick(f, "routine.fire.street", fs);
            var cold = f.Month is >= 10 or <= 4;
            extra.Add(Item("fire", new Dictionary<string, string>
            {
                ["street"] = street,
                ["num"] = HouseNumber(f, "fire.no", street).ToString(CultureInfo.InvariantCulture),
                ["damage"] = Num(Roll(f, "fire.dmg", 4, 120) * 25),
            }, c => c == "cold" && cold));
        }
        // the bills at the theatres and halls of the year: each house's own kind of bill (no
        // strong man at Daly's, no comic opera at the dime museum)
        if (r.TryGetProperty("amusements", out var am))
        {
            var venues = OfYear(am.GetProperty("venues"), f);
            if (venues.Count > 0)
            {
                var v = venues[Roll(f, "venue", 0, venues.Count - 1)];
                var house = v.TryGetProperty("kind", out var hk) ? hk.GetString() ?? "" : "";
                extra.Add(Item("amusements", new Dictionary<string, string> { ["venue"] = S(v, "name"), ["n"] = Num(Roll(f, "amuse.n", 6, 48) * 50) }, c => c.Split('|').Contains(house)));
            }
        }
        // the street accidents of the day: a horse-car, a truck, the Elevated, the bicycle, the
        // trolley and the automobile, each in its years
        if (r.TryGetProperty("accident", out _))
        {
            var street = Pick(f, "routine.accident.street", r.GetProperty("accident").GetProperty("slots").GetProperty("street"));
            extra.Add(Item("accident", new Dictionary<string, string>
            {
                ["street"] = street,
                ["num"] = HouseNumber(f, "acc.no", street).ToString(CultureInfo.InvariantCulture),
                ["age"] = Roll(f, "acc.age", 6, 71).ToString(CultureInfo.InvariantCulture),
            }));
        }
        // the Board of Health's week: milk spilled, a tenement placarded, a cellar cleared
        if (r.TryGetProperty("health", out _))
        {
            var street = Pick(f, "routine.health.street", r.GetProperty("health").GetProperty("slots").GetProperty("street"));
            extra.Add(Item("health", new Dictionary<string, string>
            {
                ["street"] = street,
                ["num"] = HouseNumber(f, "health.no", street).ToString(CultureInfo.InvariantCulture),
                ["cans"] = Num(Roll(f, "health.cans", 3, 60) * 10),
                ["quarts"] = Num(Roll(f, "health.quarts", 20, 240) * 10),
                ["families"] = Words(Roll(f, "health.families", 6, 31)),
            }));
        }
        // plans filed at the Building Department: the tenements going up
        if (r.TryGetProperty("building", out _))
        {
            var street = Pick(f, "routine.building.street", r.GetProperty("building").GetProperty("slots").GetProperty("street"));
            var stories = Roll(f, "bld.stories", f.Year >= 1897 ? 5 : 4, f.Year >= 1897 ? 7 : 6);
            extra.Add(Item("building", new Dictionary<string, string>
            {
                ["street"] = street,
                ["num"] = HouseNumber(f, "bld.no", street).ToString(CultureInfo.InvariantCulture),
                ["stories"] = Words(stories),
                ["cost"] = Num(Roll(f, "bld.cost", 14, 48) * 1000 + (f.Year - 1885) * 500),
                ["families"] = Words(stories * 4),
            }));
        }
        // the routine news is set in a different order each edition, so the page does not open
        // with the ships every month
        var all = list.Concat(extra).ToList();
        var shift = all.Count == 0 ? 0 : (int)(Hash(f.Seed, f.Turn, "routine.order") % (uint)all.Count);
        return all.Skip(shift).Concat(all.Take(shift)).ToList();
    }

    /// <summary>Conditions a routine form can ask after: "magistrate" (1895 on), "bureau" (July 1891 on).</summary>
    private static Func<string, bool> RoutineCond(PaperFacts f) => c => c switch
    {
        "magistrate" => f.Year >= 1895,
        "bureau" => f.Year * 12 + f.Month >= 1891 * 12 + 7,
        "greater" => f.Year >= 1898,
        _ => false,
    };

    /// <summary>A plain want of the day, as the classified column ran them between the Gazette's own.</summary>
    private string PlainWant(PaperFacts f, int i)
    {
        var w = _copy.GetProperty("wantsRoutine");
        var form = Pick(f, "wantsRoutine.form" + i, w.GetProperty("forms"), RoutineCond(f));
        // the number is one the street has: Doyers street is two hundred feet long
        var streets = w.GetProperty("slots");
        var street = Pick(f, "wantsRoutine." + i + ".street", streets.GetProperty("street"));
        var street2 = Pick(f, "wantsRoutine." + i + ".street2", streets.GetProperty("street2"));
        var numbered = form.Contains("{street2}") ? street2 : form.Contains("{street}") ? street : "";
        return Slots(f, "wantsRoutine." + i, form, w, new Dictionary<string, string>
        {
            ["street"] = street,
            ["street2"] = street2,
            ["num"] = (numbered.Length > 0 ? HouseNumber(f, "want.no" + i, numbered) : Roll(f, "want.no" + i, 4, 340)).ToString(CultureInfo.InvariantCulture),
            ["box"] = Roll(f, "want.box" + i, 11, 499).ToString(CultureInfo.InvariantCulture),
            ["age"] = Roll(f, "want.age" + i, 14, 17).ToString(CultureInfo.InvariantCulture),
        });
    }
}
