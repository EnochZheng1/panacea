using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Panacea.Game;
using Panacea.Paper;

namespace Panacea.UI;

/// <summary>
/// Supplies, ordered the way a druggist did: a printed order form from the wholesale house on
/// Pearl street. Mark quantities against each article (they are written in by hand), see the
/// amount and the total, and send the order in one go. The form comes back stamped.
/// </summary>
public partial class OrderForm : VBoxContainer
{
	private readonly IGame _game;
	private readonly Dictionary<string, int> _order = new();
	private string? _note;
	public event Action? Changed;

	public OrderForm(IGame game)
	{
		_game = game;
		AddThemeConstantOverride("separation", 0);
		Refresh();
	}

	public void Refresh()
	{
		var keep = Print.FocusIndex(this);
		RefreshBody();
		Print.RestoreFocus(this, keep);
	}

	private void RefreshBody()
	{
		foreach (var c in GetChildren()) { RemoveChild(c); c.QueueFree(); }
		var d = _game.Desk();
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 4);

		// the house's heading
		var head = new HBoxContainer();
		var names = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		names.AddThemeConstantOverride("separation", -2);
		names.AddChild(Print.Label("J. & W. HOBART & CO.", Print.Rye, 26));
		names.AddChild(Print.Label("Wholesale Druggists & Importers of Drugs · 81 Pearl Street, New-York", Print.FellIt, 14));
		// the house's seal, a mortar and pestle, beside its name
		var seal = LettersDesk.Seal("seal_hobart", 76);
		seal.RotationDegrees = 0;
		head.AddChild(seal);
		head.AddThemeConstantOverride("separation", 12);
		head.AddChild(names);
		var no = new VBoxContainer();
		no.AddChild(Print.Label("PRICE CURRENT", Print.Rye, 15, HorizontalAlignment.Right, wrap: false));
		no.AddChild(Print.Label("AND ORDER BLANK", Print.Fell, 11, HorizontalAlignment.Right, wrap: false));
		no.AddChild(Print.Label($"No. {4100 + d.Turn * 7}", Print.Fell, 14, HorizontalAlignment.Right, wrap: false));
		head.AddChild(no);
		v.AddChild(head);
		v.AddChild(Print.Rule(3, 1, 2));
		v.AddChild(Print.Label($"Sold to the Proprietor, Mulberry Street.   {new DateTime(d.Year, d.Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture)}.   Terms: cash on delivery.", Print.FellIt, 14));

		// the ruled table
		var grid = new GridContainer { Columns = 5 };
		grid.AddThemeConstantOverride("h_separation", 16);
		grid.AddThemeConstantOverride("v_separation", 2);
		foreach (var (h, align) in new[] { ("ARTICLE", HorizontalAlignment.Left), ("PER OZ.", HorizontalAlignment.Right), ("ON HAND", HorizontalAlignment.Right), ("ORDERED", HorizontalAlignment.Center), ("AMOUNT", HorizontalAlignment.Right) })
		{
			var l = Print.Label(h, Print.Fell, 12, align, wrap: false);
			grid.AddChild(l);
		}
		long total = 0;
		foreach (var i in d.Ingredients)
		{
			var oz = _order.GetValueOrDefault(i.Id);
			var amount = oz * i.CostPerOunceCents;
			total += amount;
			var name = Print.Label(i.Name, Print.Fell, 15, wrap: false);
			name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			name.TooltipText = i.Description;
			name.MouseFilter = MouseFilterEnum.Pass;
			grid.AddChild(name);
			grid.AddChild(Print.Label(i.CostPerOunceCents == 0 ? "free" : EditionWriter.Money(i.CostPerOunceCents), Print.Fell, 14, HorizontalAlignment.Right, wrap: false));
			grid.AddChild(Print.Label($"{i.OnHand:0.#} oz", Print.Fell, 14, HorizontalAlignment.Right, wrap: false));
			var q = new HBoxContainer();
			q.AddThemeConstantOverride("separation", 3);
			// the quantity is written in, by hand, between the marks
			var written = Print.Label(oz == 0 ? "" : $"{oz} oz", Print.FellIt, 16, HorizontalAlignment.Center, wrap: false);
			written.CustomMinimumSize = new Vector2(62, 0);
			foreach (var step in new[] { -8, 8, 32 })
			{
				var b = Print.Button(step < 0 ? "−" : $"+{step}", 11, sound: "tick");
				b.TooltipText = step < 0 ? "Strike out 8 oz" : $"{step} oz: {EditionWriter.Money(step * i.CostPerOunceCents)}";
				var id = i.Id; var s = step;
				b.Pressed += () => { _order[id] = Math.Max(0, _order.GetValueOrDefault(id) + s); _note = null; Refresh(); };
				if (step < 0) { b.Disabled = oz == 0; q.AddChild(b); q.AddChild(written); }
				else q.AddChild(b);
			}
			grid.AddChild(q);
			grid.AddChild(Print.Label(amount == 0 ? "" : EditionWriter.Money(amount), Print.FellIt, 15, HorizontalAlignment.Right, wrap: false));
		}
		v.AddChild(grid);
		v.AddChild(Print.Rule(1.4f, 0));
		var tot = new HBoxContainer();
		tot.AddChild(Print.Label("Cash in hand " + EditionWriter.Money(d.CashCents), Print.FellIt, 15));
		tot.AddChild(Print.Label("TOTAL  " + EditionWriter.Money(total), Print.Rye, 20, HorizontalAlignment.Right));
		v.AddChild(tot);
		v.AddChild(Print.Rule(3, 1, 2));

		// helpers: fill the form for the remedy in the book, or start again
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 8);
		var f = d.Formulas.FirstOrDefault();
		if (f != null)
		{
			var fill = Print.Button("FILL IN FOR 24 BOTTLES", 12);
			fill.TooltipText = $"Writes in whatever {f.Name} is short of, for a batch of 24.";
			fill.Pressed += () =>
			{
				foreach (var (id, per) in f.Components)
				{
					var have = d.Ingredients.FirstOrDefault(x => x.Id == id)?.OnHand ?? 0;
					var need = (int)Math.Ceiling(Math.Max(0, per * 24 - have));
					if (need > 0) _order[id] = need;
				}
				_note = null;
				Refresh();
			};
			row.AddChild(fill);
		}
		var clear = Print.Button("STRIKE IT ALL OUT", 12, sound: "slip");
		clear.Pressed += () => { _order.Clear(); Refresh(); };
		row.AddChild(clear);
		row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
		var send = Print.Button("SEND THE ORDER ▸", 17, solid: true, sound: null);
		send.Disabled = total == 0 && _order.Values.All(x => x == 0);
		send.TooltipText = total > d.CashCents ? "The house wants cash on delivery, and you haven't enough." : "Posts the order; the goods arrive at once.";
		send.Pressed += () => Send(d, total);
		row.AddChild(send);
		v.AddChild(row);
		if (_note != null) v.AddChild(Print.Body($"[i]{Print.Escape(_note)}[/i]", 15, justify: false));

		var sheet = Print.Slip(v, 18);
		sheet.RotationDegrees = -0.4f;
		AddChild(sheet);
	}

	private void Send(DeskModel d, long total)
	{
		string? err = null;
		var bought = 0;
		foreach (var (id, oz) in _order.Where(x => x.Value > 0).ToList())
		{
			err = _game.Buy(id, oz);
			if (err != null) break;
			bought++;
			_order.Remove(id);
		}
		Sfx.Play(err == null ? "till" : "ticket", -4f);
		_note = err != null
			? $"The house returned part of the order: {err}"
			: $"Received with thanks: {EditionWriter.Money(total)}, paid on delivery. The goods are on your shelf.";
		Refresh();
		if (bought > 0) Changed?.Invoke();
	}

	private static string Short(string name) => (name.Length > 18 ? name[..16].TrimEnd() + "…" : name).ToUpperInvariant();
}
