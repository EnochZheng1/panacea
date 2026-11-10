using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Panacea.Game;
using Panacea.Paper;

namespace Panacea.UI;

/// <summary>
/// The shop's day-book, read from the game's own save (the seed and every action, in order): what
/// has been done since the last edition went to press, and the books month by month. Built only
/// from the public save, so it is the same after a relaunch as before it.
/// </summary>
public static class MonthBook
{
	private static SimGame? _readFor;
	private static int _readAt = -1;
	private static SimGame.SaveFile? _save;

	/// <summary>The save as it stands now, or null for a game that keeps none (the fake shop).</summary>
	private static SimGame.SaveFile? Save(IGame game)
	{
		if (game is not SimGame sg) return null;
		if (!ReferenceEquals(sg, _readFor) || sg.Actions != _readAt)
		{
			_save = System.Text.Json.JsonSerializer.Deserialize<SimGame.SaveFile>(sg.SaveJson());
			_readFor = sg;
			_readAt = sg.Actions;
		}
		return _save;
	}

	/// <summary>The actions taken since the last edition went to press (all of them, in the first month).</summary>
	public static List<string[]> ThisMonth(IGame game)
	{
		var log = Save(game)?.Log;
		if (log == null) return new List<string[]>();
		var last = log.FindLastIndex(e => e.Length > 0 && e[0] == "press");
		return log.Skip(last + 1).ToList();
	}

	/// <summary>Letters answered since the last edition.</summary>
	public static int LettersAnswered(IGame game) => ThisMonth(game).Count(e => e.Length > 0 && e[0] == "resolve");

	/// <summary>
	/// What has been done this month, one short line each, for a proprietor coming back to the
	/// bench: replies sent, money laid out, prices and recipes changed, fires lit, space bought.
	/// After the money, the lines run in the order things were done, and a fire lit in a month
	/// whose recipe changed says which recipe it was.
	/// </summary>
	public static List<string> SoFar(IGame game, DeskModel m)
	{
		var acts = ThisMonth(game);
		var f = game.Facts;
		string Name(string id) => m.Formulas.FirstOrDefault(x => x.Id == id)?.Name ?? "the remedy";
		// each line with the place in the month's log it was done at
		var dated = new List<(int At, string Line)>();
		int First(Func<string[], bool> p) => acts.FindIndex(e => e.Length > 0 && p(e));
		int Last(Func<string[], bool> p) => acts.FindLastIndex(e => e.Length > 0 && p(e));

		var letters = acts.Count(e => e[0] == "resolve");
		if (letters > 0) dated.Add((First(e => e[0] == "resolve"), $"{Cap(EditionWriter.Words(letters))} letter{(letters == 1 ? "" : "s")} answered."));
		foreach (var id in acts.Where(e => e[0] == "invest" && e.Length > 1).Select(e => e[1]).Distinct())
			dated.Add((First(e => e[0] == "invest" && e.Length > 1 && e[1] == id), $"Taken up: {m.Investments.FirstOrDefault(i => i.Id == id)?.Name ?? id}."));
		foreach (var id in acts.Where(e => e[0] == "create" && e.Length > 1).Select(e => e[1]).Distinct())
			dated.Add((First(e => e[0] == "create" && e.Length > 1 && e[1] == id), $"Entered in the book: {id}."));
		var changed = acts.Where(e => e[0] == "reformulate" && e.Length > 1).Select(e => e[1]).ToHashSet();
		foreach (var id in changed)
			dated.Add((First(e => e[0] == "reformulate" && e.Length > 1 && e[1] == id), $"The recipe of {Name(id)} changed."));
		foreach (var id in acts.Where(e => e[0] == "price" && e.Length > 1).Select(e => e[1]).Distinct())
		{
			var was = f.Products.FirstOrDefault(p => p.Id == id)?.PriceCents;
			var now = m.Formulas.FirstOrDefault(x => x.Id == id)?.PriceCents;
			if (was != null && now != null && was != now)
				dated.Add((Last(e => e[0] == "price" && e.Length > 1 && e[1] == id), $"Price of {Name(id)}: {EditionWriter.Money(was.Value)}, now {EditionWriter.Money(now.Value)}".TrimEnd('.') + "."));
		}
		foreach (var id in acts.Where(e => e[0] == "claims" && e.Length > 1).Select(e => e[1]).Distinct())
			dated.Add((Last(e => e[0] == "claims" && e.Length > 1 && e[1] == id), $"The claims on {Name(id)} reset."));
		if (acts.Any(e => e[0] == "ads") && m.AdSpendCents != f.AdSpendCents)
		{
			var t = AdSpace.Tiers[AdSpace.TierFor(m.AdSpendCents)];
			dated.Add((Last(e => e[0] == "ads"), m.AdSpendCents == 0 ? "The notice in the Gazette stopped." : $"Space in the Gazette: {t.Short.ToLowerInvariant()}, {EditionWriter.Money(t.Cents)} at press."));
		}
		// fires, run together while the recipe stayed the same; a change of recipe starts a new line
		var recipe = new Dictionary<string, int>();
		var fires = new List<(int At, string Formula, int Recipe, int Fires, int Bottles)>();
		for (var k = 0; k < acts.Count; k++)
		{
			var e = acts[k];
			if (e.Length > 1 && e[0] == "reformulate") recipe[e[1]] = recipe.GetValueOrDefault(e[1]) + 1;
			if (e.Length <= 2 || e[0] != "brew") continue;
			var n = int.TryParse(e[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var b) ? b : 0;
			var r = recipe.GetValueOrDefault(e[1]);
			var at = fires.FindIndex(x => x.Formula == e[1] && x.Recipe == r);
			if (at < 0) fires.Add((k, e[1], r, 1, n));
			else fires[at] = fires[at] with { Fires = fires[at].Fires + 1, Bottles = fires[at].Bottles + n };
		}
		foreach (var g in fires)
		{
			var which = !changed.Contains(g.Formula) ? ""
				: g.Recipe == 0 ? ", to the old recipe"
				: g.Recipe == recipe.GetValueOrDefault(g.Formula) ? ", to the new recipe"
				: ", to a recipe since changed";
			dated.Add((g.At, $"{(g.Fires == 1 ? "A fire" : Cap(EditionWriter.Words(g.Fires)) + " fires")} lit: {g.Bottles} bottles of {Name(g.Formula)}{which}."));
		}
		// what the orders cost, as the books record it (the free water from the pump is not an order)
		var ordered = m.StockOrderedCents;
		if (game is not SimGame)
			foreach (var e in acts.Where(e => e[0] == "buy" && e.Length > 2))
			{
				var i = m.Ingredients.FirstOrDefault(x => x.Id == e[1]);
				if (i == null || i.Free) continue;
				if (double.TryParse(e[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var oz)) ordered += i.OrderCents(oz);
			}
		if (ordered > 0) dated.Add((Math.Max(0, First(e => e[0] == "buy" && e.Length > 1 && !(m.Ingredients.FirstOrDefault(i => i.Id == e[1])?.Free ?? false))), $"Stock ordered from {m.Supplier.Street}: {EditionWriter.Money(ordered)}".TrimEnd('.') + "."));

		var lines = dated.OrderBy(x => x.At).Select(x => x.Line).ToList();
		var laid = f.CashCents - m.CashCents;
		if (laid > 0) lines.Insert(0, $"Laid out so far: {EditionWriter.Money(laid)} ({EditionWriter.Money(f.CashCents)} when the month opened).");
		else if (laid < 0) lines.Insert(0, $"Taken in so far: {EditionWriter.Money(-laid)}".TrimEnd('.') + ".");
		return lines;
	}

	private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

	// ---------- the books by the month ----------

	/// <summary>One closed month, as the ledger keeps it.</summary>
	public sealed record MonthRow(int Year, int Month, int Bottles, long TakingsCents, long BooksCents, long CashCents, int Regulars, int Deaths, int TurnedAway);

	private static string? _histSeed;
	private static SimGame? _hist;
	private static int _histApplied;
	private static List<string[]>? _histLog;
	private static readonly List<MonthRow> _rows = new();

	/// <summary>
	/// Every month closed so far, rebuilt by playing the save over again in a second, private copy
	/// of the game (as loading a save does). The copy is kept, and only the new actions are played
	/// each time the books are opened.
	/// </summary>
	public static IReadOnlyList<MonthRow> History(IGame game)
	{
		var save = Save(game);
		if (save == null) return Array.Empty<MonthRow>();
		var log = save.Log;
		var same = _hist != null && _histSeed == save.Seed && _histLog != null && _histApplied <= log.Count
			&& Enumerable.Range(0, _histApplied).All(i => log[i].SequenceEqual(_histLog[i]));
		if (!same)
		{
			_hist = (SimGame)SimGame.Start(save.Seed);
			_histSeed = save.Seed;
			_histApplied = 0;
			_rows.Clear();
		}
		var g = _hist!;
		for (var k = _histApplied; k < log.Count; k++)
		{
			var e = log[k];
			if (e.Length == 0) continue;
			try
			{
				switch (e[0])
				{
					case "buy": g.Buy(e[1], double.Parse(e[2], CultureInfo.InvariantCulture)); break;
					case "create": g.CreateFormula(e[1], Unflat(e, 4), e[2], long.Parse(e[3], CultureInfo.InvariantCulture)); break;
					case "price": g.SetPrice(e[1], long.Parse(e[2], CultureInfo.InvariantCulture)); break;
					case "reformulate": g.Reformulate(e[1], Unflat(e, 3), e[2]); break;
					case "claims": g.SetClaims(e[1], e.Skip(2).ToList()); break;
					case "ads": g.SetAdSpend(long.Parse(e[1], CultureInfo.InvariantCulture)); break;
					case "brew": g.Brew(e[1], int.Parse(e[2], CultureInfo.InvariantCulture)); break;
					case "resolve": g.Resolve(e[1], e[2]); break;
					case "invest": g.Invest(e[1]); break;
					case "press":
					{
						g.GoToPress();
						var f = g.Facts;
						var d = g.Desk();
						_rows.Add(new MonthRow(
							f.Year, f.Month,
							f.Sales.Sum(s => s.Bottles),
							f.CashLines.Where(x => x.Cents > 0).Sum(x => x.Cents),
							f.CashCents - f.CashStartCents,
							f.CashCents,
							d.Customers,
							f.Deaths.Count,
							f.TurnedAwayByPrice));
						break;
					}
				}
			}
			catch (Exception)
			{
				// a save the copy cannot follow: show what it has
				break;
			}
		}
		_histApplied = log.Count;
		_histLog = log.Select(x => x.ToArray()).ToList();
		return _rows;
	}

	private static List<(string, double)> Unflat(string[] e, int from)
	{
		var list = new List<(string, double)>();
		for (var i = from; i + 1 < e.Length; i += 2) list.Add((e[i], double.Parse(e[i + 1], CultureInfo.InvariantCulture)));
		return list;
	}
}
