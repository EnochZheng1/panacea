using Godot;

namespace Panacea.UI;

/// <summary>
/// The Gazette's print shop: shared fonts, inks and small builders for printed-matter controls.
/// Everything the player reads is real text set in these faces (never baked into art).
/// </summary>
public static class Print
{
	public static readonly Color Ink = new("161112");
	public static readonly Color Paper = new("f0c4bd");
	public static readonly Color Cream = new("efe2c6");
	public static readonly Color Green = new("23a862");

	private static FontFile? _rye, _fell, _fellIt;
	public static FontFile Rye => _rye ??= GD.Load<FontFile>("res://assets/fonts/Rye-Regular.ttf");
	public static FontFile Fell => _fell ??= GD.Load<FontFile>("res://assets/fonts/IMFeENrm28P.ttf");
	public static FontFile FellIt => _fellIt ??= GD.Load<FontFile>("res://assets/fonts/IMFeENit28P.ttf");

	public static Label Label(string text, Font font, int size, HorizontalAlignment align = HorizontalAlignment.Left, Color? color = null, bool wrap = true)
	{
		var l = new Label
		{
			Text = text,
			HorizontalAlignment = align,
			AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off,
			SizeFlagsHorizontal = wrap ? Control.SizeFlags.ExpandFill : Control.SizeFlags.ShrinkBegin,
		};
		l.AddThemeFontOverride("font", font);
		l.AddThemeFontSizeOverride("font_size", size);
		l.AddThemeColorOverride("font_color", color ?? Ink);
		l.AddThemeConstantOverride("line_spacing", -2);
		return l;
	}

	/// <summary>Justified body text. BBCode allowed: [font=…], [i] maps to IM Fell italic.</summary>
	public static RichTextLabel Body(string bbcode, int size = 16, Color? color = null, bool justify = true, bool centre = false)
	{
		var r = new RichTextLabel
		{
			BbcodeEnabled = true,
			FitContent = true,
			ScrollActive = false,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			MouseFilter = Control.MouseFilterEnum.Pass,
		};
		r.AddThemeFontOverride("normal_font", Fell);
		r.AddThemeFontOverride("italics_font", FellIt);
		r.AddThemeFontOverride("bold_font", Rye);
		r.AddThemeFontSizeOverride("normal_font_size", size);
		r.AddThemeFontSizeOverride("italics_font_size", size);
		r.AddThemeFontSizeOverride("bold_font_size", size - 2);
		r.AddThemeColorOverride("default_color", color ?? Ink);
		r.AddThemeConstantOverride("line_separation", -1);
		// justify full lines only: never stretch a paragraph's last (or only) line
		r.JustificationFlags = TextServer.JustificationFlag.WordBound | TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.SkipLastLine;
		var wrapped = centre ? $"[center]{bbcode}[/center]" : justify ? $"[fill]{bbcode}[/fill]" : bbcode;
		r.Text = wrapped;
		return r;
	}

	public static string Escape(string s) => s.Replace("[", "[lb]");

	/// <summary>A horizontal rule: thick over thin, or a single line.</summary>
	public static Control Rule(float thick = 5f, float thin = 1.6f, float gap = 3f, Color? color = null)
	{
		var box = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		box.AddThemeConstantOverride("separation", (int)gap);
		box.AddChild(new ColorRect { Color = color ?? Ink, CustomMinimumSize = new Vector2(0, thick) });
		if (thin > 0) box.AddChild(new ColorRect { Color = color ?? Ink, CustomMinimumSize = new Vector2(0, thin) });
		return box;
	}

	/// <summary>The short centred dash printed between headline decks.</summary>
	public static Control Dash(float width = 120f)
	{
		var c = new CenterContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
		c.AddChild(new ColorRect { Color = Ink, CustomMinimumSize = new Vector2(width, 1.4f) });
		return c;
	}

	public static StyleBoxFlat Box(Color bg, Color border, int borderWidth = 2, int pad = 10, int radius = 0)
	{
		var s = new StyleBoxFlat { BgColor = bg, BorderColor = border };
		s.SetBorderWidthAll(borderWidth);
		s.SetContentMarginAll(pad);
		s.SetCornerRadiusAll(radius);
		return s;
	}

	public static PanelContainer Panel(Control child, StyleBox style)
	{
		var p = new PanelContainer();
		p.AddThemeStyleboxOverride("panel", style);
		p.AddChild(child);
		return p;
	}

	public static Texture2D? Cut(string name) =>
		string.IsNullOrEmpty(name) ? null : ResourceLoader.Exists($"res://assets/art/cuts/{name}.png") ? GD.Load<Texture2D>($"res://assets/art/cuts/{name}.png") : null;

	/// <summary>A printed ticket-style button: ink border, woodtype caption.</summary>
	public static Button Button(string text, int size = 18, bool solid = false)
	{
		var b = new Button { Text = text, FocusMode = Control.FocusModeEnum.All };
		b.AddThemeFontOverride("font", Rye);
		b.AddThemeFontSizeOverride("font_size", size);
		var fg = solid ? Paper : Ink;
		var bg = solid ? Ink : Paper;
		b.AddThemeColorOverride("font_color", fg);
		b.AddThemeColorOverride("font_hover_color", solid ? Green : Ink);
		b.AddThemeColorOverride("font_pressed_color", fg);
		b.AddThemeColorOverride("font_focus_color", fg);
		b.AddThemeColorOverride("font_disabled_color", new Color(fg, 0.4f));
		var normal = Box(bg, Ink, 2, 6);
		normal.ContentMarginLeft = normal.ContentMarginRight = 12;
		var hover = (StyleBoxFlat)normal.Duplicate();
		hover.BorderColor = solid ? Green : Ink;
		hover.SetBorderWidthAll(3);
		var pressed = (StyleBoxFlat)normal.Duplicate();
		pressed.BgColor = solid ? new Color("2c2224") : new Color("e2b3ab");
		var focus = (StyleBoxFlat)hover.Duplicate();
		focus.BgColor = new Color(0, 0, 0, 0);
		b.AddThemeStyleboxOverride("normal", normal);
		b.AddThemeStyleboxOverride("hover", hover);
		b.AddThemeStyleboxOverride("pressed", pressed);
		b.AddThemeStyleboxOverride("disabled", normal);
		b.AddThemeStyleboxOverride("focus", focus);
		return b;
	}
}
