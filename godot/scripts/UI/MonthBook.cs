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
	/// </summary>
	public static List<string> SoFar(IGame game, DeskModel m)
	{
		var lines = new List<string>();
		var acts = ThisMonth(game);
		var f = game.Facts;
		string Name(string id) => m.Formulas.FirstOrDefault(x => x.Id == id)?.Name ?? "the remedy";
		var letters = acts.Count(e => e[0] == "resolve");
		if (letters > 0) lines.Add($"{Cap(EditionWriter.Words(letters))} letter{(letters == 1 ? "" : "s")} answered.");
		foreach (var id in acts.Where(e => e[0] == "invest" && e.Length > 1).Select(e => e[1]).Distinct())
			lines.Add($"Taken up: {m.Investments.FirstOrDefault(i => i.Id == id)?.Name ?? id}.");
		foreach (var id in acts.Where(e => e[0] == "create" && e.Length > 1).Select(e => e[1]).Distinct())
			lines.Add($"Entered in the book: {id}.");
		foreach (var id in acts.Where(e => e[0] == "reformulate" && e.Length > 1).Select(e => e[1]).Distinct())
			lines.Add($"The recipe of {Name(id)} changed.");
		foreach (var id in acts.Where(e => e[0] == "price" && e.Length > 1).Select(e => e[1]).Distinct())
		{
			var was = f.Products.FirstOrDefault(p => p.Id == id)?.PriceCents;
			var now = m.Formulas.FirstOrDefault(x => x.Id == id)?.PriceCents;
			if (was != null && now != null && was != now) lines.Add($"Price of {Name(id)}: {EditionWriter.Money(was.Value)}, now {EditionWriter.Money(now.Value)}".TrimEnd('.') + ".");
		}
		foreach (var id in acts.Where(e => e[0] == "claims" && e.Length > 1).Select(e => e[1]).Distinct())
			lines.Add($"The claims on {Name(id)} reset.");
		if (acts.Any(e => e[0] == "ads") && m.AdSpendCents != f.AdSpendCents)
		{
			var t = AdSpace.Tiers[AdSpace.TierFor(m.AdSpendCents)];
			lines.Add(m.AdSpendCents == 0 ? "The notice in the Gazette stopped." : $"Space in the Gazette: {t.Short.ToLowerInvariant()}, {EditionWriter.Money(t.Cents)} at press.");
		}
		foreach (var g in acts.Where(e => e[0] == "brew" && e.Length > 2).GroupBy(e => e[1]))
		{
			var n = g.Sum(e => int.TryParse(e[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var b) ? b : 0);
			var fires = g.Count();
			lines.Add($"{(fires == 1 ? "A fire" : Cap(EditionWriter.Words(fires)) + " fires")} lit: {n} bottles of {Name(g.Key)}.");
		}
		// what the orders cost at today's prices (the free water from the pump is not an order)
		long ordered = 0;
		foreach (var e in acts.Where(e => e[0] == "buy" && e.Length > 2))
		{
			var i = m.Ingredients.FirstOrDefault(x => x.Id == e[1]);
			if (i == null || i.CostPerOunceCents == 0) continue;
			if (double.TryParse(e[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var oz))
				ordered += (long)Math.Round(Math.Round(oz) * i.CostPerOunceCents);
		}
		if (ordered > 0) lines.Add($"Stock ordered from Pearl street: {EditionWriter.Money(ordered)}".TrimEnd('.') + ".");
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
