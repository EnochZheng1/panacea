using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Panacea.Game;
using Panacea.Paper;

namespace Panacea.UI;

/// <summary>
/// The shop's books, opened from THE WORKS: where last month's money went, in plain words; what a
/// bottle earns and how many bottles the fixed charges eat before any of it is profit; and the
/// month-by-month record of bottles, takings, cash and regulars, with a rule of ink for the cash.
/// </summary>
public partial class BooksDrawer : VBoxContainer
{
	private readonly IGame _game;

	public BooksDrawer(IGame game)
	{
		_game = game;
		AddThemeConstantOverride("separation", 12);
		Build();
	}

	private static readonly Dictionary<string, string> Plain = new()
	{
		["Sales to regulars"] = "from the regulars",
		["Sales to walk-in customers"] = "from callers off the street",
		["Sales to druggists out of town"] = "from the druggists out of town",
		["Orders by post"] = "by post",
		["Paid over the price by regulars who crave it"] = "paid over the price by regulars who crave it",
		["Ingredient purchases"] = "drugs and stock from Pearl street",
		["Bottles, corks and labels"] = "bottles, corks and labels",
		["Rent"] = "the rent",
		["Newspaper advertising"] = "space in the Gazette",
		["Upkeep of the ventures (rent, wages)"] = "the ventures' rent and wages",
		["Letters answered"] = "what the letters cost",
		["Trade given up to answer letters"] = "trade given up to answer letters",
		["Outlays on the business"] = "ventures taken up",
	};

	/// <summary>The charges that fall due whether a bottle is sold or not.</summary>
	public static readonly HashSet<string> Fixed = new() { "Rent", "Newspaper advertising", "Upkeep of the ventures (rent, wages)", "Subscriptions and retainers" };

	private static string Say(string label) => Plain.TryGetValue(label, out var p) ? p : label.Length > 0 ? char.ToLowerInvariant(label[0]) + label[1..] : label;

	private void Build()
	{
		var f = _game.Facts;
		var d = _game.Desk();
		var top = new HBoxContainer();
		top.AddChild(Print.Label("THE BOOKS", Print.Rye, 22, color: Print.Paper));
		top.AddChild(Print.Label("kept by the month, to the cent", Print.FellIt, 16, HorizontalAlignment.Right, Print.Paper));
		AddChild(Print.Panel(top, Print.Box(Print.Ink, Print.Ink, 0, 8)));

		var sheet = new VBoxContainer();
		sheet.AddThemeConstantOverride("separation", 8);

		// ---- where last month's money went ----
		var when = new DateTime(f.Year, f.Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture);
		sheet.AddChild(Print.Label(f.FirstEdition ? "THE MONTH'S MONEY" : $"WHERE {when.ToUpperInvariant()}'S MONEY WENT", Print.Rye, 16));
		if (f.FirstEdition || f.CashLines.Count == 0)
			sheet.AddChild(Print.Body("[i]No month has closed yet. After the first edition the books will say where every cent went.[/i]", 15, justify: false));
		else
		{
			var ins = f.CashLines.Where(x => x.Cents > 0).OrderByDescending(x => x.Cents).ToList();
			var outs = f.CashLines.Where(x => x.Cents < 0).OrderBy(x => x.Cents).ToList();
			var tookIn = ins.Sum(x => x.Cents);
			var paid = -outs.Sum(x => x.Cents);
			var net = tookIn - paid;
			var text = $"The shop took in [b]{EditionWriter.Money(tookIn)}[/b]" + (ins.Count > 0 ? ": " + string.Join(", ", ins.Select(x => $"{EditionWriter.Money(x.Cents)} {Say(x.Label)}")) : "") + ". "
				+ $"It paid out [b]{EditionWriter.Money(paid)}[/b]" + (outs.Count > 0 ? ": " + string.Join(", ", outs.Select(x => $"{EditionWriter.Money(-x.Cents)} for {Say(x.Label)}")) : "") + ". "
				+ (net >= 0 ? $"The till was [b]{EditionWriter.Money(net)} up[/b] on the month." : $"The till was [b]{EditionWriter.Money(-net)} down[/b] on the month.");
			// stock bought and not yet sold is still the shop's: say so when it explains a poor month
			var stock = f.StockValueCents - f.StockValueStartCents;
			if (stock > 50) text += $" Of what was paid out, {EditionWriter.Money(stock)} is still on the shelf as stock.";
			sheet.AddChild(Print.Body(text, 15, justify: false));
		}

		// ---- what a bottle earns, and how many bottles the rent eats ----
		sheet.AddChild(Print.Rule(1.4f, 0));
		sheet.AddChild(Print.Label("WHAT A BOTTLE EARNS", Print.Rye, 16));
		if (d.Formulas.Count == 0)
			sheet.AddChild(Print.Body("[i]No remedy is entered yet.[/i]", 15, justify: false));
		long bestClear = 0;
		foreach (var p in d.Formulas)
		{
			var clear = p.PriceCents - p.UnitCostCents;
			bestClear = Math.Max(bestClear, clear);
			sheet.AddChild(Print.Body(Print.Escape(clear > 0
				? $"{p.Name} sells at {EditionWriter.Money(p.PriceCents)} and costs {EditionWriter.Money(p.UnitCostCents)} to make: {EditionWriter.Money(clear)} clear on every bottle."
				: $"{p.Name} sells at {EditionWriter.Money(p.PriceCents)} and costs {EditionWriter.Money(p.UnitCostCents)} to make: every bottle sold loses money."), 15, justify: false));
		}
		var fixedCents = -f.CashLines.Where(x => Fixed.Contains(x.Label)).Sum(x => x.Cents);
		if (!f.FirstEdition && fixedCents > 0 && bestClear > 0)
		{
			var pays = (int)Math.Ceiling(fixedCents / (double)bestClear);
			var sold = f.Sales.Sum(s => s.Bottles);
			sheet.AddChild(Print.Body(Print.Escape(
				$"The rent, the ventures' upkeep, the subscriptions and the Gazette came to {EditionWriter.Money(fixedCents)} last month, sold or not. At the best margin above, the first {EditionWriter.Words(pays)} bottles a month only pay for them; " +
				(sold > pays ? $"last month sold {EditionWriter.Words(sold)}, so the rest, about {EditionWriter.Words(sold - pays)}, were the profit." : $"last month sold {EditionWriter.Words(sold)}, which did not cover them.") +
				" More trade means more money only when it comes faster than the charges grow."), 15, justify: false));
		}
		if (!f.FirstEdition)
			sheet.AddChild(Print.Body(Print.Escape(f.TurnedAwayByPrice > 0
				? $"Callers who asked the price last month and went away: {EditionWriter.Words(f.TurnedAwayByPrice)}."
				: "Nobody went away over the price last month."), 15, justify: false));

		// ---- month by month ----
		sheet.AddChild(Print.Rule(1.4f, 0));
		sheet.AddChild(Print.Label("BY THE MONTH", Print.Rye, 16));
		var rows = MonthBook.History(_game);
		if (rows.Count == 0)
			sheet.AddChild(Print.Body("[i]Nothing closed yet.[/i]", 15, justify: false));
		else
		{
			sheet.AddChild(Print.Label("Newest first. The rule of ink beside the cash is its height against the best month.", Print.FellIt, 14));
			var grid = new GridContainer { Columns = 7 };
			grid.AddThemeConstantOverride("h_separation", 14);
			grid.AddThemeConstantOverride("v_separation", 2);
			foreach (var (h, right) in new[] { ("MONTH", false), ("SOLD", true), ("TOOK IN", true), ("THE BOOKS", true), ("CASH IN HAND", true), ("", false), ("REGULARS", true) })
				grid.AddChild(Print.Label(h, Print.Rye, 12, right ? HorizontalAlignment.Right : HorizontalAlignment.Left, wrap: false));
			var most = Math.Max(1, rows.Max(r => r.CashCents));
			foreach (var r in rows.Reverse())
			{
				grid.AddChild(Print.Label(new DateTime(r.Year, r.Month, 1).ToString("MMM yyyy", CultureInfo.InvariantCulture), Print.Fell, 15, wrap: false));
				grid.AddChild(Print.Label(r.Bottles.ToString(CultureInfo.InvariantCulture), Print.Fell, 15, HorizontalAlignment.Right, wrap: false));
				grid.AddChild(Print.Label(EditionWriter.Money(r.TakingsCents), Print.Fell, 15, HorizontalAlignment.Right, wrap: false));
				grid.AddChild(Print.Label(r.BooksCents >= 0 ? "+" + EditionWriter.Money(r.BooksCents) : "−" + EditionWriter.Money(-r.BooksCents), Print.Fell, 15, HorizontalAlignment.Right, wrap: false));
				grid.AddChild(Print.Label(EditionWriter.Money(r.CashCents), Print.Fell, 15, HorizontalAlignment.Right, wrap: false));
				var bar = new Control { CustomMinimumSize = new Vector2(140, 14), SizeFlagsVertical = SizeFlags.ShrinkCenter };
				bar.AddChild(new ColorRect { Color = Print.Ink, Position = new Vector2(0, 3), Size = new Vector2(Math.Max(2f, 140f * Math.Max(0, r.CashCents) / most), 8) });
				grid.AddChild(bar);
				grid.AddChild(Print.Label(r.Regulars.ToString(CultureInfo.InvariantCulture) + (r.Deaths > 0 ? $" ({r.Deaths} died)" : ""), Print.Fell, 15, HorizontalAlignment.Right, wrap: false));
			}
			sheet.AddChild(grid);
		}
		AddChild(Print.Slip(sheet, 18));
	}
}
