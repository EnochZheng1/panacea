using System;
using System.Linq;
using Godot;
using Panacea.Game;
using Panacea.Paper;

namespace Panacea.UI;

/// <summary>
/// The long game, as printed matter: a prospectus of improvements a growing house might take up
/// (a branch, an almanac, a wholesale contract, a laboratory). Each is set as a small notice with
/// its terms, what it brings and what it risks, and a slip to take it up.
/// </summary>
public partial class Prospectus : VBoxContainer
{
	private readonly IGame _game;
	private string? _note;
	public event Action? Changed;

	/// <summary>What each venture waits for before it is offered (the sim's conditions, in words).</summary>
	private static readonly System.Collections.Generic.Dictionary<string, string> Waits = new()
	{
		["branch_shop"] = "Offered once the shop next door has been knocked through.",
		["wholesale_contract"] = "Offered after fifteen hundred bottles sold, while the talk in the ward is low.",
		["laboratory"] = "Offered once a percolator is in the back room.",
		["almanac"] = "Offered once the Gazette will sell you space.",
	};

	public Prospectus(IGame game)
	{
		_game = game;
		AddThemeConstantOverride("separation", 12);
		Refresh();
	}

	public void Refresh()
	{
		var keep = Print.FocusIndex(this);
		foreach (var c in GetChildren()) { RemoveChild(c); c.QueueFree(); }
		var d = _game.Desk();

		var top = new HBoxContainer();
		top.AddChild(Print.Label("PROSPECTUS OF IMPROVEMENTS", Print.Rye, 22, color: Print.Paper));
		top.AddChild(Print.Label("for a house in a way of business", Print.FellIt, 16, HorizontalAlignment.Right, Print.Paper));
		AddChild(Print.Panel(top, Print.Box(Print.Ink, Print.Ink, 0, 8)));
		if (_note != null) AddChild(Print.Slip(Print.Body($"[i]{Print.Escape(EditionWriter.Set(_note))}[/i]", 15, justify: false), 10));
		if (d.Investments.Count == 0)
		{
			AddChild(Print.Slip(Print.Body("[center][i]Nothing on offer to a house of your size, yet.[/i][/center]", 15, justify: false), 18));
			Print.RestoreFocus(this, keep);
			return;
		}
		var i = 0;
		foreach (var row in d.Investments) AddChild(Notice(row, d, i++));
		Print.RestoreFocus(this, keep);
	}

	private Control Notice(InvestmentRow r, DeskModel d, int index)
	{
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 4);
		v.AddChild(Print.Label(r.Name.ToUpperInvariant(), Print.Rye, 18));
		v.AddChild(Print.Body(Print.Escape(EditionWriter.Set(r.Description)), 15));

		// the terms, as a price current would set them
		var terms = $"Terms: {EditionWriter.Money(r.CostCents)} down";
		if (r.UpkeepCents > 0) terms += $", and {EditionWriter.Money(r.UpkeepCents)} a month after";
		terms += r.Months is int m ? $"; it runs {EditionWriter.Words(m)} months." : "; it is yours for good.";
		v.AddChild(Print.Label(terms, Print.Fell, 14));

		// what it brings and what it risks, in plain words
		var brings = new System.Collections.Generic.List<string>();
		if (r.WalkInsPerMonth > 0) brings.Add($"about {EditionWriter.Words(r.WalkInsPerMonth)} more callers a month");
		if (r.WholesaleBottles is int wb && wb > 0) brings.Add($"orders for about {EditionWriter.Words(wb)} bottles a month" + (r.WholesalePriceCents is long wp ? $" at {EditionWriter.Money(wp)} each" : ""));
		var risks = new System.Collections.Generic.List<string>();
		if (r.HeatPerBoldness > 0) risks.Add("every claim on your label is read in ten thousand kitchens, and the ward talks of it");
		if (r.MishapChance is double mc && mc > 0) risks.Add($"about one month in {Math.Max(2, (int)Math.Round(1 / mc))} something catches fire");
		if (brings.Count > 0) v.AddChild(Print.Label("It brings " + string.Join(", and ", brings) + ".", Print.FellIt, 14));
		if (risks.Count > 0) v.AddChild(Print.Label("Mark well: " + string.Join("; ", risks) + ".", Print.FellIt, 14));

		if (r.Held)
		{
			v.AddChild(Print.Label(r.MonthsLeft is int left ? $"YOURS · {EditionWriter.Words(left)} month{(left == 1 ? "" : "s")} to run" : "YOURS", Print.Rye, 14, HorizontalAlignment.Right));
		}
		else if (!r.CanBuy && r.Refusal is { } why && why.StartsWith("Not yet", StringComparison.Ordinal))
		{
			// not on offer yet: say what it waits for, as the trade would
			v.AddChild(Print.Label(Waits.TryGetValue(r.Id, out var w) ? w : why, Print.FellIt, 14, HorizontalAlignment.Right, new Color(Print.Ink, 0.85f)));
		}
		else
		{
			var b = Print.Button(r.CanBuy ? $"☞  TAKE IT UP · {EditionWriter.Money(r.CostCents)}" : "NOT TO BE HAD", 14, solid: r.CanBuy, sound: "till");
			b.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
			Print.SetLive(b, r.CanBuy && d.CashCents >= r.CostCents);
			b.TooltipText = r.Refusal ?? (d.CashCents < r.CostCents ? $"It wants {EditionWriter.Money(r.CostCents)}, and you have {EditionWriter.Money(d.CashCents)}." : "Paid at once; it starts this month.");
			var id = r.Id; var name = r.Name;
			b.Pressed += () =>
			{
				var err = _game.Invest(id);
				_note = err != null ? "Refused: " + err : $"Taken up: {name}. It will be in the next edition.";
				Refresh();
				if (err == null) Changed?.Invoke();
			};
			v.AddChild(b);
			if (r.Refusal != null) v.AddChild(Print.Label(r.Refusal, Print.FellIt, 13, HorizontalAlignment.Right, new Color(Print.Ink, 0.85f)));
			else if (d.CashCents < r.CostCents) v.AddChild(Print.Label($"It wants {EditionWriter.Money(r.CostCents)}; you have {EditionWriter.Money(d.CashCents)}.", Print.FellIt, 13, HorizontalAlignment.Right, new Color(Print.Ink, 0.85f)));
		}
		var s = Print.Slip(v, 16);
		s.RotationDegrees = index % 2 == 0 ? -0.4f : 0.3f;
		return s;
	}
}
