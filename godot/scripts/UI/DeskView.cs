using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Panacea.Game;
using Panacea.Paper;

namespace Panacea.UI;

/// <summary>
/// The proprietor's desk: supplies, the formula book, the still, the proof sheet and the
/// morning's letters, laid out as printed slips on a dark desk. Every control submits a
/// simulation action through <see cref="IGame"/>; the desk is rebuilt from the game after each.
/// </summary>
public partial class DeskView : ScrollContainer
{
	private IGame? _game;
	private string _note = "";
	private bool _noteIsError;

	// the formula being composed in the book (not yet part of the game)
	private readonly Dictionary<string, double> _draft = new();
	private string _draftProcess = "simple_mix";
	private string _draftName = "Dr. ——'s Celebrated Panacea";
	private long _draftPrice = 50;
	private readonly Dictionary<string, int> _brewCounts = new();
	private string? _proofFormula;

	public event Action? PressRequested;

	public DeskView()
	{
		HorizontalScrollMode = ScrollMode.Disabled;
		SetAnchorsPreset(LayoutPreset.FullRect);
	}

	public void Bind(IGame game)
	{
		_game = game;
		if (_draft.Count == 0)
		{
			_draft["grain_alcohol"] = 2;
			_draft["sugar_syrup"] = 1;
			_draft["water"] = 4;
			_draft["peppermint_oil"] = 0.5;
		}
		Rebuild();
	}

	private void Act(string? error, string success)
	{
		_note = error ?? success;
		_noteIsError = error != null;
		var scroll = ScrollVertical;
		Rebuild();
		CallDeferred(nameof(RestoreScroll), scroll);
	}

	private void RestoreScroll(int v) => ScrollVertical = v;

	public void Rebuild()
	{
		if (_game == null) return;
		foreach (var c in GetChildren()) { RemoveChild(c); c.QueueFree(); }
		var m = _game.Desk();

		var margin = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		foreach (var side in new[] { "left", "right" }) margin.AddThemeConstantOverride("margin_" + side, 40);
		margin.AddThemeConstantOverride("margin_top", 26);
		margin.AddThemeConstantOverride("margin_bottom", 110);
		AddChild(margin);
		var page = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		page.AddThemeConstantOverride("separation", 16);
		margin.AddChild(page);

		page.AddChild(Header(m));

		var cols = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		cols.AddThemeConstantOverride("separation", 18);
		page.AddChild(cols);
		var c1 = Stack(); var c2 = Stack(); var c3 = Stack();
		cols.AddChild(c1); cols.AddChild(c2); cols.AddChild(c3);

		c1.AddChild(Letters(m));
		c1.AddChild(Supplies(m));
		c2.AddChild(FormulaBook(m));
		c3.AddChild(Still(m));
		c3.AddChild(ProofSheet(m));
		c3.AddChild(Prospects(m));
	}

	// ---------- slips ----------

	private Control Header(DeskModel m)
	{
		var h = new HBoxContainer();
		h.AddThemeConstantOverride("separation", 24);
		var title = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		title.AddChild(Print.Label("THE PROPRIETOR'S DESK", Print.Rye, 34, color: Print.Cream));
		var when = new DateTime(m.Year, m.Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture);
		title.AddChild(Print.Label($"{when}. The next edition goes to press when you pull the lever.", Print.FellIt, 17, color: Print.Cream));
		h.AddChild(title);
		var books = new VBoxContainer();
		books.AddChild(Print.Label("THE BOOKS", Print.Rye, 15, HorizontalAlignment.Center));
		books.AddChild(Print.Label($"Cash in hand: {EditionWriter.Money(m.CashCents)}", Print.Fell, 16));
		books.AddChild(Print.Label($"Standing in the ward: {m.Reputation:0}", Print.Fell, 16));
		books.AddChild(Print.Label($"Talk about the shop: {Talk(m.Heat)}", Print.Fell, 16));
		books.AddChild(Print.Label($"Regulars: {m.Customers} of {m.CustomerCapacity}", Print.Fell, 16));
		h.AddChild(Slip(books, Print.Cream, 230));
		return h;
	}

	private static string Talk(double heat) =>
		heat < 10 ? "none to speak of" : heat < 30 ? "a little" : heat < 55 ? "a good deal" : heat < 80 ? "alarming" : "the Board of Health is asking";

	private Control Letters(DeskModel m)
	{
		var v = SlipBody("THE MORNING'S LETTERS");
		if (m.Letters.Count == 0)
			v.AddChild(Print.Body("[i]Nothing in the post but a circular for galvanic belts.[/i]", 15, justify: false));
		foreach (var l in m.Letters)
		{
			v.AddChild(Print.Label(l.Title, Print.Rye, 17));
			v.AddChild(Print.Body(Print.Escape(l.Body), 15));
			foreach (var (id, text) in l.Choices)
			{
				var b = Print.Button(text, 14);
				b.AutowrapMode = TextServer.AutowrapMode.WordSmart;
				b.Alignment = HorizontalAlignment.Left;
				var instance = l.InstanceId;
				var choice = id;
				b.Pressed += () => Act(_game!.Resolve(instance, choice), "Answered.");
				v.AddChild(b);
			}
			v.AddChild(Print.Dash(80));
		}
		return Slip(v, Print.Paper);
	}

	private Control Supplies(DeskModel m)
	{
		var v = SlipBody("SUPPLIES");
		v.AddChild(Print.Body("[i]From the wholesale druggist on Pearl street. Prices by the fluid ounce.[/i]", 14, justify: false));
		foreach (var i in m.Ingredients)
		{
			var row = new HBoxContainer();
			var label = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			label.AddThemeConstantOverride("separation", -2);
			label.AddChild(Print.Label(i.Name, Print.Fell, 16));
			label.AddChild(Print.Label($"{EditionWriter.Money(i.CostPerOunceCents)} · on hand {i.OnHand:0.#} oz", Print.FellIt, 13));
			row.AddChild(label);
			foreach (var amount in new[] { 8, 32 })
			{
				var b = Print.Button($"+{amount}", 13);
				var id = i.Id; var name = i.Name; var a = amount;
				b.Pressed += () => Act(_game!.Buy(id, a), $"Bought {a} oz of {name}.");
				row.AddChild(b);
			}
			v.AddChild(row);
		}
		return Slip(v, Print.Cream);
	}

	private Control FormulaBook(DeskModel m)
	{
		var v = SlipBody("THE FORMULA BOOK");
		foreach (var f in m.Formulas)
		{
			v.AddChild(Print.Label(f.Name, Print.Rye, 19));
			v.AddChild(Print.Label($"{f.Stock} bottles on the shelf · costs {EditionWriter.Money(f.UnitCostCents)} to make", Print.FellIt, 14));
			var price = new HBoxContainer();
			price.AddChild(Print.Label($"Sold at {EditionWriter.Money(f.PriceCents)}", Print.Fell, 16));
			foreach (var step in new[] { -10, -5, 5, 10 })
			{
				var b = Print.Button(step > 0 ? $"+{step}c" : $"{step}c", 12);
				var id = f.Id; var target = Math.Max(1, f.PriceCents + step);
				b.Pressed += () => Act(_game!.SetPrice(id, target), $"Price set at {EditionWriter.Money(target)}.");
				price.AddChild(b);
			}
			v.AddChild(price);
			v.AddChild(Gauges(f.Properties));
			v.AddChild(Print.Dash(80));
		}

		v.AddChild(Print.Label("A NEW RECIPE", Print.Rye, 17));
		v.AddChild(Print.Body($"[i]Fluid ounces per bottle. A bottle holds {m.BottleCapacityOunces:0} oz.[/i]", 14, justify: false));
		var total = _draft.Values.Sum();
		foreach (var i in m.Ingredients)
		{
			var row = new HBoxContainer();
			_draft.TryGetValue(i.Id, out var oz);
			row.AddChild(Print.Label(i.Name, Print.Fell, 15));
			var minus = Print.Button("−", 12);
			var plus = Print.Button("+", 12);
			var id = i.Id;
			minus.Pressed += () => { _draft[id] = Math.Max(0, (_draft.GetValueOrDefault(id)) - 0.5); Act(null, ""); };
			plus.Pressed += () => { _draft[id] = _draft.GetValueOrDefault(id) + 0.5; Act(null, ""); };
			row.AddChild(minus);
			row.AddChild(Print.Label($"{oz:0.0}", Print.Fell, 15, HorizontalAlignment.Center));
			row.AddChild(plus);
			v.AddChild(row);
		}
		v.AddChild(Print.Label($"In the bottle: {total:0.0} of {m.BottleCapacityOunces:0} oz", Print.FellIt, 14));

		var procs = new HBoxContainer();
		foreach (var (id, name) in m.Processes)
		{
			var b = Print.Button(name, 12, solid: id == _draftProcess);
			var pid = id;
			b.Pressed += () => { _draftProcess = pid; Act(null, ""); };
			procs.AddChild(b);
		}
		v.AddChild(procs);

		var nameEdit = new LineEdit { Text = _draftName, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		nameEdit.AddThemeFontOverride("font", Print.Fell);
		nameEdit.AddThemeFontSizeOverride("font_size", 16);
		nameEdit.TextChanged += t => _draftName = t;
		v.AddChild(nameEdit);

		var comps = _draft.Where(kv => kv.Value > 0).Select(kv => (kv.Key, kv.Value)).ToList();
		var preview = _game!.Preview(comps, _draftProcess);
		if (preview.Error != null) v.AddChild(Print.Body($"[i]{Print.Escape(preview.Error)}[/i]", 14, justify: false));
		else
		{
			v.AddChild(Print.Label($"Would cost {EditionWriter.Money(preview.UnitCostCents)} a bottle to make.", Print.FellIt, 14));
			v.AddChild(Gauges(preview.Properties));
		}
		var priceRow = new HBoxContainer();
		priceRow.AddChild(Print.Label($"To sell at {EditionWriter.Money(_draftPrice)}", Print.Fell, 16));
		foreach (var step in new[] { -10, 10 })
		{
			var b = Print.Button(step > 0 ? $"+{step}c" : $"{step}c", 12);
			var s = step;
			b.Pressed += () => { _draftPrice = Math.Max(1, _draftPrice + s); Act(null, ""); };
			priceRow.AddChild(b);
		}
		v.AddChild(priceRow);
		var record = Print.Button("ENTER IT IN THE BOOK", 16, solid: true);
		record.Pressed += () => Act(_game!.CreateFormula(_draftName, comps, _draftProcess, _draftPrice), $"\"{_draftName}\" is entered in the book.");
		v.AddChild(record);
		return Slip(v, Print.Cream);
	}

	private Control Still(DeskModel m)
	{
		var v = SlipBody("THE STILL");
		v.AddChild(Print.Body($"[i]{m.StillsFree} still{(m.StillsFree == 1 ? "" : "s")} free this month. A batch is bottled by month's end.[/i]", 14, justify: false));
		foreach (var (formula, bottles) in m.Brewing)
			v.AddChild(Print.Label($"Brewing: {bottles} bottles of {formula}", Print.FellIt, 14));
		if (m.Formulas.Count == 0)
			v.AddChild(Print.Body("Nothing to brew. Enter a recipe in the formula book first.", 15));
		foreach (var f in m.Formulas)
		{
			var n = Math.Clamp(_brewCounts.GetValueOrDefault(f.Id, Math.Min(24, f.MaxBrewable)), 0, Math.Max(0, f.MaxBrewable));
			v.AddChild(Print.Label(f.Name, Print.Fell, 16));
			var row = new HBoxContainer();
			foreach (var step in new[] { -12, -1, 1, 12 })
			{
				var b = Print.Button(step > 0 ? $"+{step}" : $"{step}", 12);
				var id = f.Id; var s = step; var cur = n;
				b.Pressed += () => { _brewCounts[id] = Math.Max(0, cur + s); Act(null, ""); };
				row.AddChild(b);
			}
			row.AddChild(Print.Label($"{n} bottles (most you can: {f.MaxBrewable})", Print.FellIt, 14));
			v.AddChild(row);
			var brew = Print.Button($"FIRE THE STILL: {n} BOTTLES", 15, solid: true);
			brew.Disabled = n <= 0 || m.StillsFree <= 0;
			var fid = f.Id; var count = n; var fname = f.Name;
			brew.Pressed += () => Act(_game!.Brew(fid, count), $"{count} bottles of {fname} are on the still.");
			v.AddChild(brew);
		}
		return Slip(v, Print.Paper);
	}

	private Control ProofSheet(DeskModel m)
	{
		var v = SlipBody("THE PROOF SHEET");
		if (m.Formulas.Count == 0)
		{
			v.AddChild(Print.Body("[i]Nothing to advertise yet.[/i]", 15, justify: false));
			return Slip(v, Print.Cream);
		}
		var f = m.Formulas.FirstOrDefault(x => x.Id == _proofFormula) ?? m.Formulas[0];
		_proofFormula = f.Id;
		v.AddChild(Print.Body($"What the label and the notices will claim for [i]{Print.Escape(f.Name)}[/i] (up to {m.MaxClaims}). Bolder claims sell more and draw more talk.", 15));
		foreach (var (id, name) in m.Claims)
		{
			var on = f.ClaimIds.Contains(id);
			var b = Print.Button((on ? "☞ " : "   ") + name, 13, solid: on);
			b.Alignment = HorizontalAlignment.Left;
			b.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			var claim = id; var fid = f.Id;
			b.Pressed += () =>
			{
				var next = on ? f.ClaimIds.Where(c => c != claim).ToList() : f.ClaimIds.Append(claim).ToList();
				Act(_game!.SetClaims(fid, next), on ? "Claim struck out." : "Claim set in type.");
			};
			v.AddChild(b);
		}
		v.AddChild(Print.Dash(80));
		if (!m.CanAdvertise)
		{
			v.AddChild(Print.Body("[i]The Gazette will not take your money yet. Save a little and get yourself a testimonial.[/i]", 14, justify: false));
		}
		else
		{
			v.AddChild(Print.Label($"Space in the Gazette: {EditionWriter.Money(m.AdSpendCents)} a month", Print.Fell, 16));
			var row = new HBoxContainer();
			foreach (var step in new[] { -500, -100, 100, 500 })
			{
				var b = Print.Button(step > 0 ? $"+{EditionWriter.Money(step)}" : $"−{EditionWriter.Money(-step)}", 12);
				var target = Math.Max(0, m.AdSpendCents + step);
				b.Pressed += () => Act(_game!.SetAdSpend(target), target > 0 ? "The Gazette will run your notice." : "Notice withdrawn.");
				row.AddChild(b);
			}
			v.AddChild(row);
		}
		return Slip(v, Print.Cream);
	}

	private Control Prospects(DeskModel m)
	{
		var v = SlipBody("PROSPECTS");
		foreach (var (name, progress) in m.NextUnlocks.Take(3))
		{
			v.AddChild(Print.Label(name, Print.Fell, 15));
			var bar = new HBoxContainer();
			bar.AddChild(new ColorRect { Color = Print.Ink, CustomMinimumSize = new Vector2((float)(200 * Math.Clamp(progress, 0, 1)), 8) });
			bar.AddChild(new ColorRect { Color = new Color(Print.Ink, 0.18f), CustomMinimumSize = new Vector2((float)(200 * (1 - Math.Clamp(progress, 0, 1))), 8) });
			v.AddChild(bar);
		}
		var press = Print.Button("GO TO PRESS ▸", 26, solid: true);
		press.CustomMinimumSize = new Vector2(0, 64);
		press.Pressed += () => PressRequested?.Invoke();
		v.AddChild(press);
		if (!string.IsNullOrEmpty(_note))
			v.AddChild(Print.Body($"[i]{Print.Escape(_note)}[/i]", 15, _noteIsError ? new Color("8e2f22") : Print.Ink));
		return Slip(v, Print.Paper);
	}

	// ---------- helpers ----------

	private static readonly (string Key, string Label)[] GaugeOrder =
	{
		("kick", "Kick"), ("calm", "Calm"), ("habit", "Habit"), ("bitterness", "Bitterness"),
		("acuteToxicity", "Danger"), ("accumulation", "Lingers in the body"), ("detectability", "Shows in an assay"),
	};

	private static Control Gauges(Dictionary<string, double> props)
	{
		var g = new GridContainer { Columns = 2 };
		g.AddThemeConstantOverride("h_separation", 8);
		g.AddThemeConstantOverride("v_separation", 1);
		foreach (var (key, label) in GaugeOrder)
		{
			if (!props.TryGetValue(key, out var value)) continue;
			g.AddChild(Print.Label(label, Print.FellIt, 13, wrap: false));
			var bar = new HBoxContainer();
			var w = (float)Math.Clamp(value / 10.0, 0, 1) * 140;
			bar.AddChild(new ColorRect { Color = key is "acuteToxicity" or "habit" ? Print.Green : Print.Ink, CustomMinimumSize = new Vector2(Math.Max(2, w), 9), SizeFlagsVertical = SizeFlags.ShrinkCenter });
			bar.AddChild(Print.Label($"{value:0.0}", Print.Fell, 12, wrap: false));
			g.AddChild(bar);
		}
		return g;
	}

	private static VBoxContainer Stack()
	{
		var v = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		v.AddThemeConstantOverride("separation", 16);
		return v;
	}

	private static VBoxContainer SlipBody(string title)
	{
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 5);
		v.AddChild(Print.Label(title, Print.Rye, 22, HorizontalAlignment.Center));
		v.AddChild(Print.Rule(3, 1, 2));
		return v;
	}

	private static Control Slip(Control body, Color paper, int minWidth = 0)
	{
		var p = Print.Panel(body, Print.Box(paper, new Color("2a1e18"), 1, 16));
		p.CustomMinimumSize = new Vector2(minWidth, 0);
		return p;
	}
}
