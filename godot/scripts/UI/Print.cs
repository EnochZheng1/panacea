using Godot;

namespace Panacea.UI;

/// <summary>
/// The Gazette's print shop: shared fonts, inks and small builders for printed-matter controls.
/// Everything the player reads is real text set in these faces (never baked into art).
/// </summary>
public static class Print
{
	// Black ink on white newsprint. Scheele's green is reserved for the product.
	public static readonly Color Ink = new("141212");
	public static readonly Color Paper = new("efece4");
	public static readonly Color Table = new("0d0c0b");
	public static readonly Color Faint = new("141212", 0.55f);
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

	/// <summary>
	/// A printed ticket: woodtype caption inside a ruled border. Hovering inks it in (the colours
	/// invert); "solid" tickets are the main action of a slip and start inked.
	/// </summary>
	public static Button Button(string text, int size = 18, bool solid = false)
	{
		var b = new Button { Text = text, FocusMode = Control.FocusModeEnum.All, MouseDefaultCursorShape = Control.CursorShape.PointingHand };
		b.AddThemeFontOverride("font", Rye);
		b.AddThemeFontSizeOverride("font_size", size);
		var fg = solid ? Paper : Ink;
		var bg = solid ? Ink : Paper;
		b.AddThemeColorOverride("font_color", fg);
		b.AddThemeColorOverride("font_hover_color", bg);
		b.AddThemeColorOverride("font_pressed_color", bg);
		b.AddThemeColorOverride("font_hover_pressed_color", bg);
		b.AddThemeColorOverride("font_focus_color", fg);
		b.AddThemeColorOverride("font_disabled_color", new Color(fg, 0.35f));
		StyleBoxFlat Make(Color fill, Color border, int width)
		{
			var st = Box(fill, border, width, 6);
			st.ContentMarginLeft = st.ContentMarginRight = Mathf.Max(10, size * 0.7f);
			st.ContentMarginTop = st.ContentMarginBottom = Mathf.Max(4, size * 0.3f);
			return st;
		}
		b.AddThemeStyleboxOverride("normal", Make(bg, Ink, 2));
		b.AddThemeStyleboxOverride("hover", Make(fg, Ink, 2));
		b.AddThemeStyleboxOverride("pressed", Make(fg, Ink, 3));
		b.AddThemeStyleboxOverride("hover_pressed", Make(fg, Ink, 3));
		b.AddThemeStyleboxOverride("disabled", Make(bg, new Color(Ink, 0.3f), 1));
		var focus = Make(new Color(0, 0, 0, 0), Ink, 1);
		focus.ExpandMarginLeft = focus.ExpandMarginRight = focus.ExpandMarginTop = focus.ExpandMarginBottom = 3;
		b.AddThemeStyleboxOverride("focus", focus);
		return b;
	}

	/// <summary>A thin paper-white scrollbar that suits the black table.</summary>
	public static void QuietScrollbar(ScrollContainer sc)
	{
		var bar = sc.GetVScrollBar();
		var track = new StyleBoxFlat { BgColor = new Color(Paper, 0.06f) };
		track.ContentMarginLeft = track.ContentMarginRight = 3;
		var grab = new StyleBoxFlat { BgColor = new Color(Paper, 0.45f) };
		grab.SetCornerRadiusAll(3);
		var hot = (StyleBoxFlat)grab.Duplicate();
		hot.BgColor = new Color(Paper, 0.8f);
		bar.AddThemeStyleboxOverride("scroll", track);
		bar.AddThemeStyleboxOverride("grabber", grab);
		bar.AddThemeStyleboxOverride("grabber_highlight", hot);
		bar.AddThemeStyleboxOverride("grabber_pressed", hot);
		bar.CustomMinimumSize = new Vector2(8, 0);
	}

	/// <summary>A sheet of paper with a double rule: an outer ink border and a fine inner one.</summary>
	public static PanelContainer Slip(Control body, int pad = 16, int minWidth = 0)
	{
		var inner = Panel(body, Box(new Color(0, 0, 0, 0), Ink, 1, pad));
		var outer = Panel(inner, Box(Paper, Ink, 3, 4));
		outer.CustomMinimumSize = new Vector2(minWidth, 0);
		return outer;
	}
}
