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
		["shop_next_door"] = "Offered once the shop has twenty-five regulars.",
		["larger_copper"] = "Offered once the second still is in.",
		["fourth_still"] = "Offered once the third still is in and two thousand bottles are sold.",
		["laboratory"] = "Offered once a percolator is in the back room.",
		["almanac"] = "Offered once the Gazette will sell you space.",
	};

	/// <summary>
	/// What a venture waits on, in two kinds of line: "Waits on: a second still, 9 of 200 bottles
	/// sold" (the condition) and "Its price: $150.00 of the $200.00 in hand" (the money).
	/// </summary>
	private static System.Collections.Generic.IEnumerable<string> ProgressLines(System.Collections.Generic.List<string> progress)
	{
		foreach (var raw in progress)
		{
			var p = raw.Replace("for the shop next door, the shop next door taken (in VENTURES)", "the shop next door, which is not yet taken up (it is above)")
				.Replace("the shop next door taken (in VENTURES)", "the shop next door, not yet taken up (above)");
			if (p.Contains(" in hand", StringComparison.Ordinal)) yield return $"Its price: {p}.";
			else if (p.StartsWith("for ", StringComparison.Ordinal)) yield return $"Waits on {p[4..]}.";
			else if (p.StartsWith("waits on ", StringComparison.Ordinal)) yield return $"Waits on {p[9..]}.";
			else yield return $"So far: {p}.";
		}
	}

	public Prospectus(IGame game)
	{
		_game = game;
		AddThemeConstantOverride("separation", 12);
		Refresh();
		// the prospectus opens at its head: the drawer's first focus (and its slide in) must not leave
		// it scrolled past the first notice
		TreeEntered += () =>
		{
			if (_opened) return;
			_opened = true;
			GetTree().CreateTimer(0.3).Timeout += () =>
			{
				if (!IsInstanceValid(this)) return;
				Node? frame = GetParent();
				while (frame != null && frame is not ScrollContainer) frame = frame.GetParent();
				var focus = GetViewport()?.GuiGetFocusOwner();
				// a keyboard already on a slip's button keeps its place
				if (frame is ScrollContainer sc && focus is not BaseButton) sc.ScrollVertical = 0;
			};
		};
	}

	private bool _opened;

	public void Refresh()
	{
		var keep = Print.FocusIndex(this);
		// the sheet keeps its place in the drawer when it is reset: the slip pressed stays under the hand
		var frame = GetParent();
		while (frame != null && frame is not ScrollContainer) frame = frame.GetParent();
		var scroll = (frame as ScrollContainer)?.ScrollVertical ?? 0;
		if (frame is ScrollContainer sc0)
		{
			Callable.From(() => { if (IsInstanceValid(sc0)) sc0.ScrollVertical = scroll; }).CallDeferred();
			// and once more after the layout has settled, for the frame that follows focus
			GetTree().CreateTimer(0.05).Timeout += () => { if (IsInstanceValid(sc0)) sc0.ScrollVertical = scroll; };
		}
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

	private static readonly System.Text.RegularExpressions.Regex MonthlySum = new(
		@"(?:\$\d+(?:\.\d\d)?|\b(?:a|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|fifteen|twenty|thirty|forty|fifty) dollars?)(?=(?: more)? a month)",
		System.Text.RegularExpressions.RegexOptions.IgnoreCase);

	/// <summary>
	/// The venture's description, with any sum it names "a month" set to the figure the terms
	/// below it charge: the terms come from the numbers, and the prose may not contradict them.
	/// </summary>
	public static string AgreeWithTerms(InvestmentRow r)
	{
		if (r.UpkeepCents <= 0) return r.Description;
		var sum = r.UpkeepCents % 100 == 0
			? (r.UpkeepCents == 100 ? "a dollar" : $"{EditionWriter.Words((int)(r.UpkeepCents / 100))} dollars")
			: EditionWriter.Money(r.UpkeepCents);
		return MonthlySum.Replace(r.Description, m =>
		{
			var said = m.Value;
			var keep = said.StartsWith('$') || char.IsLower(said[0]) ? sum : char.ToUpperInvariant(sum[0]) + sum[1..];
			return keep;
		});
	}

	private Control Notice(InvestmentRow r, DeskModel d, int index)
	{
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 4);
		v.AddChild(Print.Label(r.Name.ToUpperInvariant(), Print.Rye, 18));
		v.AddChild(Print.Body(Print.Escape(EditionWriter.Set(AgreeWithTerms(r))), 15));

		// the terms, as a price current would set them
		var terms = r.CostCents == 0 ? "Terms: nothing down" : $"Terms: {EditionWriter.Money(r.CostCents)} down";
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
		// a bigger copper or another still pays only once the stills the shop has are kept full
		if (!r.Held && r.Id is "larger_copper" or "fourth_still" && d.Stills > 0 && !_game.Facts.FirstEdition)
		{
			var can = d.Stills * d.StillBottles;
			var sold = _game.Facts.SoldLastMonth;
			if (sold < can * 0.85)
				v.AddChild(Print.Label(EditionWriter.Set($"Mark well: your stills are not yet full. Last month the shop sold {EditionWriter.Words(sold)} bottle{(sold == 1 ? "" : "s")}, and the stills it has can make {EditionWriter.Words(can)}; this pays only once they are kept busy."), Print.FellIt, 14));
		}

		// and more room at the counter pays only once the room the shop has is taken
		if (!r.Held && r.Id is "shop_next_door" or "branch_shop" && !(r.Refusal?.StartsWith("Not yet", StringComparison.Ordinal) ?? false) && d.CustomerCapacity > 0 && d.Customers < d.CustomerCapacity * 0.85)
			v.AddChild(Print.Label(EditionWriter.Set($"Mark well: the shop has room for {EditionWriter.Words(d.CustomerCapacity)} regulars and has {EditionWriter.Words(d.Customers)}; more room pays only once the room it has is taken."), Print.FellIt, 14));

		if (r.Held)
		{
			v.AddChild(Print.Label(r.MonthsLeft is int left ? $"YOURS · {EditionWriter.Words(left)} month{(left == 1 ? "" : "s")} to run" : "YOURS", Print.Rye, 14, HorizontalAlignment.Right));
		}
		else if (!r.CanBuy && r.Refusal is { } why && why.StartsWith("Not yet", StringComparison.Ordinal))
		{
			// not on offer yet: say what it waits for, as the trade would
			// (a description that already says what it waits for is not told twice)
			if (!r.Description.Contains("Offered", StringComparison.Ordinal))
				v.AddChild(Print.Label(Waits.TryGetValue(r.Id, out var w) ? w : why, Print.FellIt, 14, HorizontalAlignment.Right, new Color(Print.Ink, 0.85f)));
			// and how far the house has come toward it
			// the thing it waits on, and its price, set apart: a price is not a condition of the thing before it
			foreach (var line in ProgressLines(r.Progress))
				v.AddChild(Print.Label(EditionWriter.Set(line), Print.Fell, 14, HorizontalAlignment.Right));
		}
		else
		{
			// short only of cash: it is on offer, at a price not yet in hand
			var shortOfCash = !r.CanBuy && r.Refusal != null && r.Refusal.Contains("cash", StringComparison.OrdinalIgnoreCase);
			var price = r.CostCents == 0 ? "NOTHING DOWN" : EditionWriter.Money(r.CostCents);
			var b = Print.Button(r.CanBuy || shortOfCash ? $"☞  TAKE IT UP · {price}" : "NOT TO BE HAD", 14, solid: r.CanBuy, sound: "till");
			b.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
			Print.SetLive(b, r.CanBuy && d.CashCents >= r.CostCents);
			b.TooltipText = r.Refusal ?? (d.CashCents < r.CostCents ? $"It wants {EditionWriter.Money(r.CostCents)}, and you have {EditionWriter.Money(d.CashCents)}." : "Paid at once; it starts this month.");
			var id = r.Id; var name = r.Name; var cost = r.CostCents;
			if (_asking == id)
			{
				b.Text = cost == 0 ? "☞  TAKE IT UP? PRESS AGAIN" : $"☞  PAY {EditionWriter.Money(cost)} DOWN? PRESS AGAIN";
				b.TooltipText = "Press again to pay and take it up. Escape, or any other control, to think better of it.";
			}
			b.Pressed += () =>
			{
				if (_asking != id)
				{
					// ask first, and give a moment before a second press counts
					// (no note above the sheet: it would push the button out from under the hand)
					_asking = id;
					_note = null;
					Main.Guard(400);
					Refresh();
					return;
				}
				_asking = null;
				var err = _game.Invest(id);
				var upkeep = r.UpkeepCents > 0 ? $", and {EditionWriter.Money(r.UpkeepCents)} a month after" : "";
				_note = err != null ? "Refused: " + err
					: cost == 0 ? $"Taken up: {name}, nothing down{upkeep}. The next edition will report it."
					: $"Taken up: {name}, for {EditionWriter.Money(cost)}{upkeep}. The next edition will report it.";
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
			if (r.Refusal != null && !shortOfCash) v.AddChild(Print.Label(r.Refusal, Print.FellIt, 13, HorizontalAlignment.Right, new Color(Print.Ink, 0.85f)));
			else if (d.CashCents < r.CostCents) v.AddChild(Print.Label($"It wants {EditionWriter.Money(r.CostCents)}; you have {EditionWriter.Money(d.CashCents)}.", Print.FellIt, 13, HorizontalAlignment.Right, new Color(Print.Ink, 0.85f)));
		}
		var s = Print.Panel(v, Print.Box(new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), 2, 8));
		s.FocusMode = FocusModeEnum.All;
		var plain = s.GetThemeStylebox("panel");
		s.FocusEntered += () => s.AddThemeStyleboxOverride("panel", Print.Box(new Color(0, 0, 0, 0), Print.Ink, 3, 8));
		s.FocusExited += () => s.AddThemeStyleboxOverride("panel", plain);
		return s;
	}
}
