using System.Linq;
using Godot;
using Panacea.Game;
using Panacea.Paper;

namespace Panacea.UI;

/// <summary>
/// THE REGULARS, opened from the counter: every customer who comes back for the bottle, the
/// frailest first, each with how they look across the counter, the bottle's hold on them, their
/// complaint and how long they have come. The counter has room for a few cards; this has them all,
/// in the drawers' large print.
/// </summary>
public partial class RegularsList : VBoxContainer
{
	public RegularsList(IGame game)
	{
		AddThemeConstantOverride("separation", 10);
		var m = game.Desk();
		var top = new HBoxContainer();
		top.AddChild(Print.Label("THE REGULARS", Print.Rye, 22, color: Print.Paper, wrap: false));
		var count = m.Regulars.Count;
		var sub = Print.Label(count == 0 ? "none yet" : $"{EditionWriter.Words(count)} in all, the frailest first", Print.FellIt, 16, HorizontalAlignment.Right, Print.Paper);
		sub.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		top.AddChild(sub);
		AddChild(Print.Panel(top, Print.Box(Print.Ink, Print.Ink, 0, 8)));

		var sheet = new VBoxContainer();
		sheet.AddThemeConstantOverride("separation", 6);
		sheet.AddChild(Print.Body("[i]How each looks across the counter, not what a doctor would find. The gravely ill and the poorly are the likeliest to die; a regular who asks for the bottle by name has come to crave it, one shaking wants it and has not had it, and one ruined by it has spent beyond their means on it.[/i]", 15, justify: false));
		if (count == 0)
			sheet.AddChild(Print.Label("No one has come back for a second bottle yet.", Print.FellIt, 16));
		var first = true;
		foreach (var r in m.Regulars.OrderByDescending(ProofView.Risk).ThenBy(r => r.MonthsAsCustomer).ThenBy(r => r.Name))
		{
			if (!first) sheet.AddChild(Print.Rule(1, 0));
			first = false;
			sheet.AddChild(Row(r));
		}
		AddChild(Print.Slip(sheet, 16));
	}

	private static Control Row(Regular r)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 14);
		var who = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		who.AddThemeConstantOverride("separation", 0);
		who.AddChild(Print.Label(r.Name, Print.Rye, LargePrint.Pick(16, 18, 20)));
		var took = r.BottlesLastMonth > 0 ? $"; {EditionWriter.Words(r.BottlesLastMonth).ToLowerInvariant()} bottle{(r.BottlesLastMonth == 1 ? "" : "s")} last month" : "; none last month";
		who.AddChild(Print.Label($"{(r.Infant ? "For " : "")}{(r.Infant ? r.Ailment.ToLowerInvariant() : Cap(r.Ailment.ToLowerInvariant()))}. {Cap(ProofView.Since(r))}{took}; {(r.MonthsAsCustomer == 0 ? "new" : r.Mood)}.", Print.Fell, 15));
		row.AddChild(who);
		// how they look, and the bottle's hold on them, set as on the cards at the counter
		var marks = new VBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
		marks.AddThemeConstantOverride("separation", 3);
		var looks = ProofView.Health(r.Looks, r.Infant ? "infant " : "");
		looks.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
		marks.AddChild(looks);
		if (ProofView.Habit(r) is { } habit)
		{
			habit.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
			marks.AddChild(habit);
		}
		row.AddChild(marks);
		row.TooltipText = $"{r.Name}: {(r.Infant ? "for " : "")}{r.Ailment.ToLowerInvariant()}. {Cap(ProofView.Since(r))}; {Panacea.Game.SimGame.Describe(r)}.\n{ProofView.LooksKey}";
		row.MouseFilter = MouseFilterEnum.Pass;
		return row;
	}

	private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];
}
