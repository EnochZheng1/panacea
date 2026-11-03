using System;
using System.Linq;
using Godot;
using Panacea.Paper;

namespace Panacea.UI;

/// <summary>Renders an <see cref="Edition"/> as a front page of the Gazette.</summary>
public partial class EditionView : ScrollContainer
{
	private const int PageWidth = 1110;

	public EditionView()
	{
		HorizontalScrollMode = ScrollMode.Disabled;
		SetAnchorsPreset(LayoutPreset.FullRect);
		Print.QuietScrollbar(this);
	}

	public void Show(Edition e)
	{
		foreach (var c in GetChildren()) { RemoveChild(c); c.QueueFree(); }
		ScrollVertical = 0;

		var centre = new CenterContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		AddChild(centre);
		var page = new VBoxContainer { CustomMinimumSize = new Vector2(PageWidth, 0) };
		page.AddThemeConstantOverride("separation", 6);
		// the edition is a sheet of newsprint lying on the black table
		var sheetStyle = Print.Box(Print.Paper, new Color(0, 0, 0, 0), 0, 0);
		sheetStyle.ContentMarginLeft = sheetStyle.ContentMarginRight = 34;
		sheetStyle.ContentMarginTop = 26;
		sheetStyle.ContentMarginBottom = 34;
		sheetStyle.ShadowColor = new Color(0, 0, 0, 0.6f);
		sheetStyle.ShadowSize = 18;
		sheetStyle.ShadowOffset = new Vector2(0, 6);
		var sheet = Print.Panel(page, sheetStyle);
		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_top", 28);
		margin.AddThemeConstantOverride("margin_bottom", 110);
		margin.AddChild(sheet);
		centre.AddChild(margin);

		// ---- masthead ----
		page.AddChild(Print.Rule());
		var mast = new HBoxContainer();
		mast.AddThemeConstantOverride("separation", 18);
		mast.AddChild(Ear(e.EarLeftTitle, e.EarLeft));
		var title = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		title.AddThemeConstantOverride("separation", 0);
		title.AddChild(Print.Label(e.Over, Print.FellIt, 17, HorizontalAlignment.Center));
		var name = Print.Label(e.Masthead, Print.Rye, 56, HorizontalAlignment.Center, wrap: false);
		name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		title.AddChild(name);
		title.AddChild(Print.Label(e.Motto, Print.FellIt, 15, HorizontalAlignment.Center));
		mast.AddChild(title);
		mast.AddChild(Ear(e.EarRightTitle, e.EarRight));
		page.AddChild(mast);
		page.AddChild(Print.Rule());
		var dateline = new HBoxContainer();
		dateline.AddChild(Print.Label(e.DatelineLeft, Print.Fell, 14));
		dateline.AddChild(Print.Label(e.DatelineCentre, Print.Fell, 14, HorizontalAlignment.Center));
		dateline.AddChild(Print.Label(e.DatelineRight, Print.Fell, 14, HorizontalAlignment.Right));
		page.AddChild(dateline);
		page.AddChild(Print.Rule(1.6f, 0));

		// ---- three columns ----
		var cols = new HBoxContainer();
		cols.AddThemeConstantOverride("separation", 0);
		page.AddChild(cols);
		var left = Column(270);
		var lead = Column(0, expand: true);
		var right = Column(290);
		cols.AddChild(left);
		cols.AddChild(ColumnRule());
		cols.AddChild(lead);
		cols.AddChild(ColumnRule());
		cols.AddChild(right);

		foreach (var s in e.Left) AddSection(left, s);
		AddLead(lead, e.Lead);
		foreach (var s in e.LeadFoot) AddSection(lead, s);
		foreach (var s in e.Right) AddSection(right, s);

		// ---- the markets ----
		page.AddChild(Print.Rule());
		page.AddChild(Print.Body($"[center][b]THE MARKETS.[/b]  {Print.Escape(e.Markets)}[/center]", 15, justify: false));
		page.AddChild(Print.Rule(1.6f, 0));
	}

	private static Control Ear(string head, string text)
	{
		var v = new VBoxContainer { CustomMinimumSize = new Vector2(170, 0) };
		v.AddThemeConstantOverride("separation", 2);
		v.AddChild(Print.Label(head, Print.Rye, 15, HorizontalAlignment.Center));
		v.AddChild(Print.Label(text, Print.Fell, 13, HorizontalAlignment.Center));
		var p = Print.Panel(v, Print.Box(new Color(0, 0, 0, 0), Print.Ink, 2, 8));
		p.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		return p;
	}

	private static VBoxContainer Column(int width, bool expand = false)
	{
		var v = new VBoxContainer { CustomMinimumSize = new Vector2(width, 0) };
		if (expand) v.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		v.AddThemeConstantOverride("separation", 8);
		var m = new MarginContainer();
		return v;
	}

	private static Control ColumnRule()
	{
		var m = new MarginContainer { CustomMinimumSize = new Vector2(30, 0) };
		var line = new ColorRect { Color = Print.Ink, CustomMinimumSize = new Vector2(1.6f, 0), SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
		m.AddChild(line);
		return m;
	}

	private static void AddSection(VBoxContainer col, Section s)
	{
		if (!string.IsNullOrEmpty(s.Title))
		{
			col.AddChild(Print.Label(s.Title, Print.Rye, 21, HorizontalAlignment.Center));
			col.AddChild(Print.Dash(90));
		}
		foreach (var b in s.Blocks) col.AddChild(BlockControl(b));
	}

	private static Control BlockControl(Block b)
	{
		switch (b.Kind)
		{
			case BlockKind.Ad:
			case BlockKind.BlackAd:
			{
				var black = b.Kind == BlockKind.BlackAd;
				var fg = black ? Print.Paper : Print.Ink;
				var v = new VBoxContainer();
				v.AddThemeConstantOverride("separation", 3);
				if (!string.IsNullOrEmpty(b.Mid)) v.AddChild(Print.Label(b.Mid, Print.Fell, 14, HorizontalAlignment.Center, fg));
				v.AddChild(Print.Label(b.Head, Print.Rye, 21, HorizontalAlignment.Center, fg));
				var tex = Print.Cut(b.Cut);
				if (tex != null)
				{
					v.AddChild(new TextureRect
					{
						Texture = tex,
						ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
						StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
						CustomMinimumSize = new Vector2(0, 110),
					});
				}
				v.AddChild(Print.Body($"[i]{Print.Escape(b.Text)}[/i]", 14, fg, centre: true));
				if (!string.IsNullOrEmpty(b.Fine)) v.AddChild(Print.Body($"[i]{Print.Escape(b.Fine)}[/i]", 12, fg, centre: true));
				var inner = Print.Panel(v, Print.Box(new Color(0, 0, 0, 0), fg, 1, 8));
				return Print.Panel(inner, Print.Box(black ? Print.Ink : new Color(0, 0, 0, 0), Print.Ink, 3, 3));
			}
			case BlockKind.Notice:
			default:
			{
				var lead = string.IsNullOrEmpty(b.Lead) ? "" : $"[b]{Print.Escape(b.Lead)}[/b]";
				return Print.Body(lead + Print.Escape(b.Text), 15);
			}
		}
	}

	private static void AddLead(VBoxContainer col, Story s)
	{
		col.AddChild(Print.Label(s.Headline, Print.Rye, 52, HorizontalAlignment.Center));
		for (var i = 0; i < s.Decks.Count; i++)
		{
			col.AddChild(Print.Dash());
			var italic = i % 2 == 0;
			col.AddChild(Print.Label(italic ? s.Decks[i] : s.Decks[i].ToUpperInvariant(), italic ? Print.FellIt : Print.Fell, italic ? 23 : 15, HorizontalAlignment.Center));
		}
		var tex = Print.Cut(s.Cut);
		if (tex != null)
		{
			var frame = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ShrinkCenter, CustomMinimumSize = new Vector2(420, 430) };
			frame.AddThemeStyleboxOverride("panel", Print.Box(new Color(0, 0, 0, 0), Print.Ink, 3, 10));
			var stack = new Control();
			var tint = new ColorRect { Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/tint.gdshader") } };
			tint.SetAnchorsPreset(LayoutPreset.FullRect);
			stack.AddChild(tint);
			var img = new TextureRect
			{
				Texture = tex,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			};
			img.SetAnchorsPreset(LayoutPreset.FullRect);
			stack.AddChild(img);
			frame.AddChild(stack);
			var c = new CenterContainer();
			c.AddChild(frame);
			col.AddChild(c);
			col.AddChild(Print.Label(s.Cutline, Print.FellIt, 14, HorizontalAlignment.Center));
		}
		// body in two sub-columns, split at a sentence near the middle
		var (a, b) = SplitHalf(s.Body);
		var two = new HBoxContainer();
		two.AddThemeConstantOverride("separation", 22);
		two.AddChild(Print.Body(DropCap(Print.Escape(a)), 16));
		if (b.Length > 0) two.AddChild(Print.Body(Print.Escape(b), 16));
		col.AddChild(two);
	}

	private static string DropCap(string s) =>
		s.Length < 2 || !char.IsLetter(s[0]) ? s : $"[font_size=34][b]{s[0]}[/b][/font_size]{s[1..]}";

	private static (string, string) SplitHalf(string text)
	{
		if (text.Length < 260) return (text, "");
		var mid = text.Length / 2;
		var best = -1;
		for (var i = text.IndexOf(". ", StringComparison.Ordinal); i >= 0; i = text.IndexOf(". ", i + 1, StringComparison.Ordinal))
		{
			var word = text[..i].Split(' ').Last();
			if (word is "Dr" or "Mr" or "Mrs" or "St" or "Prof" or "Rev") continue; // not a sentence end
			if (best < 0 || Math.Abs(i - mid) < Math.Abs(best - mid)) best = i;
		}
		if (best < 0) return (text, "");
		return (text[..(best + 1)], text[(best + 2)..]);
	}
}
