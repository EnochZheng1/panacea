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
    private string Slots(PaperFacts f, string key, string template, JsonElement kind, IReadOnlyDictionary<string, string>? given = null)
    {
        var slots = kind.TryGetProperty("slots", out var sl) ? sl : default;
        return Regex.Replace(template, @"\{([a-zA-Z0-9_]+)\}", m =>
        {
            var name = m.Groups[1].Value;
            // {place_cap}: the slot's value, capitalized to begin a sentence
            if (name.EndsWith("_cap", StringComparison.Ordinal)) return Cap(Slots(f, key, "{" + name[..^4] + "}", kind, given));
            if (given != null && given.TryGetValue(name, out var v)) return v;
            if (name == "weekday") return Pick(f, key + ".weekday", _copy.GetProperty("weekdays"));
            if (slots.ValueKind == JsonValueKind.Object && slots.TryGetProperty(name, out var list))
                return Slots(f, key + "." + name, Pick(f, key + "." + name, list), kind, given);
            return m.Value;
        });
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
        string Item(string kind, IReadOnlyDictionary<string, string>? given = null)
        {
            var k = r.GetProperty(kind);
            var form = Pick(f, "routine." + kind + ".form", k.GetProperty("forms"), RoutineCond(f));
            var tail = k.TryGetProperty("tails", out var tails) ? Pick(f, "routine." + kind + ".tail", tails, RoutineCond(f)) : "";
            var all = new Dictionary<string, string>(given ?? new Dictionary<string, string>()) { ["tail"] = Slots(f, "routine." + kind + ".t", tail, k, given) };
            return Slots(f, "routine." + kind, form, k, all).Replace("  ", " ").Trim();
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
        }));

        // the police court: magistrates from 1895, police justices before
        var female = Roll(f, "pc.sex", 0, 3) == 0;
        var pc = r.GetProperty("police");
        var judge = f.Year >= 1895 ? "the Magistrate" : "the Justice";
        list.Add(Item("police", new Dictionary<string, string>
        {
            ["given"] = Pick(f, "routine.police.given" + (female ? "f" : "m"), pc.GetProperty(female ? "givenFemale" : "givenMale")),
            ["trade"] = Pick(f, "routine.police.trade" + (female ? "f" : "m"), pc.GetProperty(female ? "tradesFemale" : "tradesMale")),
            ["age"] = Roll(f, "pc.age", 19, 63).ToString(CultureInfo.InvariantCulture),
            ["judge"] = judge,
            ["Judge"] = Cap(judge),
            ["pronoun"] = female ? "she" : "he",
            ["Pronoun"] = female ? "She" : "He",
            ["possessive"] = female ? "her" : "his",
            ["object"] = female ? "her" : "him",
        }));

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
            var basis = f.Year < 1898 ? 640 + years * 10 : 1180 + (f.Year - 1898) * 30;
            var season = f.Month is 6 or 7 or 8 ? 1.35 : f.Month is 1 or 2 or 3 ? 1.12 : 1.0;
            var n = (int)(basis * season * (0.92 + Roll(f, "mort", 0, 16) / 100.0));
            var under5 = (int)(n * ((f.Month is 6 or 7 or 8 ? 0.46 : 0.36) - years * 0.003));
            var consumption = (int)(n * (0.11 + Roll(f, "mort.c", 0, 3) / 100.0));
            extra.Add(Item("mortality", new Dictionary<string, string>
            {
                ["n"] = Num(n), ["under5"] = Num(under5), ["consumption"] = Num(consumption),
            }));
        }
        // a fire, of no great size
        extra.Add(Item("fire", new Dictionary<string, string>
        {
            ["num"] = Roll(f, "fire.no", 3, 290).ToString(CultureInfo.InvariantCulture),
            ["damage"] = Num(Roll(f, "fire.dmg", 4, 120) * 25),
        }));
        // the bills at the theatres and halls of the year
        if (r.TryGetProperty("amusements", out var am))
        {
            var venues = OfYear(am.GetProperty("venues"), f);
            if (venues.Count > 0)
            {
                var v = venues[Roll(f, "venue", 0, venues.Count - 1)];
                extra.Add(Item("amusements", new Dictionary<string, string> { ["venue"] = S(v, "name") }));
            }
        }
        // three of the four a month, turn and turn about (all three out of the ball season)
        var skip = f.Turn % extra.Count;
        list.AddRange(extra.Where((_, i) => extra.Count < 4 || i != skip));
        return list;
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
        return Slots(f, "wantsRoutine." + i, form, w, new Dictionary<string, string>
        {
            ["num"] = Roll(f, "want.no" + i, 4, 340).ToString(CultureInfo.InvariantCulture),
            ["box"] = Roll(f, "want.box" + i, 11, 499).ToString(CultureInfo.InvariantCulture),
            ["age"] = Roll(f, "want.age" + i, 14, 17).ToString(CultureInfo.InvariantCulture),
        });
    }
}
