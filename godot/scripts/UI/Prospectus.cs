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
	/// <summary>The venture whose slip has been pressed once: the next press pays.</summary>
	private string? _asking;
	/// <summary>True while the sheet is being reset, when focus leaves the old slips without the player.</summary>
	private bool _rebuilding;
	public event Action? Changed;

	/// <summary>What each venture waits for before it is offered (the sim's conditions, in words).</summary>
	private static readonly System.Collections.Generic.Dictionary<string, string> Waits = new()
	{
		["branch_shop"] = "Offered once the shop next door has been knocked through.",
		["wholesale_contract"] = "Offered after twelve hundred bottles sold, while the talk in the ward is low.",
		["druggists_order"] = "Offered after four hundred bottles sold, while the talk in the ward is low.",
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
		_rebuilding = true;
		foreach (var c in GetChildren()) { RemoveChild(c); c.QueueFree(); }
		_rebuilding = false;
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
		// one printed sheet, the notices ruled off one from the next, not loose cards
		var sheet = new VBoxContainer();
		sheet.AddThemeConstantOverride("separation", 10);
		var i = 0;
		foreach (var row in d.Investments)
		{
			if (i > 0) sheet.AddChild(Print.Rule(2f, 0.8f, 2f));
			sheet.AddChild(Notice(row, d, i++));
		}
		AddChild(Print.Slip(sheet, 18));
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
		// a contract the druggists can cancel is not "yours for good"
		terms += r.Months is int m ? $"; it runs {EditionWriter.Words(m)} months."
			: r.WholesaleBottles != null ? (r.Id == "druggists_order" ? "; it runs until the druggist cancels, which he will if the talk in the ward turns ugly." : "; it runs until the druggists cancel, which they will if the talk in the ward turns ugly.")
			: "; it is yours for good.";
		v.AddChild(Print.Label(terms, Print.Fell, 14));

		// what it brings and what it risks, in plain words
		var brings = new System.Collections.Generic.List<string>();
		if (r.WalkInsPerMonth > 0) brings.Add($"about {EditionWriter.Words(r.WalkInsPerMonth)} more callers a month");
		if (r.PostBottles is int pb && pb > 0) brings.Add($"about {EditionWriter.Words(pb)} bottles a month sent for by post" + (r.PostPriceCents is long pp ? $" at {EditionWriter.Money(pp)}" : ""));
		if (r.WholesaleBottles is int wb && wb > 0) brings.Add($"orders for about {EditionWriter.Words(wb)} bottles a month" + (r.WholesalePriceCents is long wp ? $" at {EditionWriter.Money(wp)} each" : ""));
		var risks = new System.Collections.Generic.List<string>();
		if (r.HeatPerBoldness > 0) risks.Add("it prints your label's claims in ten thousand kitchens, so the bolder they are, the more suspicion it brings each month");
		if (r.MishapChance is double mc && mc > 0) risks.Add($"about one month in {Math.Max(2, (int)Math.Round(1 / mc))} something catches fire");
		if (brings.Count > 0) v.AddChild(Print.Label(EditionWriter.Set("It brings " + string.Join(", and ", brings) + "."), Print.FellIt, 14));
		if (risks.Count > 0) v.AddChild(Print.Label("Mark well: " + string.Join("; ", risks) + ".", Print.FellIt, 14));

		if (r.Held)
		{
			v.AddChild(Print.Label(r.MonthsLeft is int left ? $"YOURS · {EditionWriter.Words(left)} month{(left == 1 ? "" : "s")} to run" : "YOURS", Print.Rye, 14, HorizontalAlignment.Right));
		}
		else if (!r.CanBuy && r.Refusal is { } why && why.StartsWith("Not yet", StringComparison.Ordinal))
		{
			// not on offer yet: say what it waits for, as the trade would
			v.AddChild(Print.Label(Waits.TryGetValue(r.Id, out var w) ? w : why, Print.FellIt, 14, HorizontalAlignment.Right, new Color(Print.Ink, 0.85f)));
			// and how far the house has come toward it
			if (r.Progress.Count > 0)
				v.AddChild(Print.Label(EditionWriter.Set("So far: " + string.Join("; ", r.Progress) + "."), Print.Fell, 13, HorizontalAlignment.Right));
		}
		else
		{
			var b = Print.Button(r.CanBuy ? $"☞  TAKE IT UP · {EditionWriter.Money(r.CostCents)}" : "NOT TO BE HAD", 14, solid: r.CanBuy, sound: "till");
			b.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
			Print.SetLive(b, r.CanBuy && d.CashCents >= r.CostCents);
			b.TooltipText = r.Refusal ?? (d.CashCents < r.CostCents ? $"It wants {EditionWriter.Money(r.CostCents)}, and you have {EditionWriter.Money(d.CashCents)}." : "Paid at once; it starts this month.");
			var id = r.Id; var name = r.Name; var cost = r.CostCents;
			if (_asking == id)
			{
				b.Text = $"☞  PAY {EditionWriter.Money(cost)} DOWN? PRESS AGAIN";
				b.TooltipText = "Press again to pay and take it up. Escape, or any other control, to think better of it.";
			}
			b.Pressed += () =>
			{
				if (_asking != id)
				{
					// ask first, and give a moment before a second press counts
					_asking = id;
					_note = $"{name}: {EditionWriter.Money(cost)} down. Press again to take it up.";
					Main.Guard(400);
					Refresh();
					return;
				}
				_asking = null;
				var err = _game.Invest(id);
				_note = err != null ? "Refused: " + err : $"Taken up: {name}, for {EditionWriter.Money(cost)}. It will be in the next edition.";
				Main.Guard(800);
				Refresh();
				if (err == null) Changed?.Invoke();
			};
			b.FocusExited += () =>
			{
				if (_asking != id || _rebuilding) return;
				_asking = null;
				_note = null;
				Callable.From(() => { if (IsInstanceValid(this)) Refresh(); }).CallDeferred();
			};
			v.AddChild(b);
			if (r.Refusal != null) v.AddChild(Print.Label(r.Refusal, Print.FellIt, 13, HorizontalAlignment.Right, new Color(Print.Ink, 0.85f)));
			else if (d.CashCents < r.CostCents) v.AddChild(Print.Label($"It wants {EditionWriter.Money(r.CostCents)}; you have {EditionWriter.Money(d.CashCents)}.", Print.FellIt, 13, HorizontalAlignment.Right, new Color(Print.Ink, 0.85f)));
		}
		var s = Print.Panel(v, Print.Box(new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), 2, 8));
		s.FocusMode = FocusModeEnum.All;
		s.TooltipText = r.Name;
		var plain = s.GetThemeStylebox("panel");
		s.FocusEntered += () => s.AddThemeStyleboxOverride("panel", Print.Box(new Color(0, 0, 0, 0), Print.Ink, 3, 8));
		s.FocusExited += () => s.AddThemeStyleboxOverride("panel", plain);
		return s;
	}
}
