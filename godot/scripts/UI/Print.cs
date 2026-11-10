using System.Linq;
using System;
using System.Collections.Generic;
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
	public static readonly Color Faint = new("141212", 0.78f);
	public static readonly Color Green = new("23a862");

	private static FontFile? _rye, _fell, _fellIt;
	public static FontFile Rye => _rye ??= GD.Load<FontFile>("res://assets/fonts/Rye-Regular.ttf");
	// IM Fell with lining figures in place of its old-style ones, whose 1 and 0 read as I and o
	// ("$I9.8o"); see tools/make_lining_figures.py
	public static FontFile Fell => _fell ??= GD.Load<FontFile>("res://assets/fonts/IMFeENrm28P-Lining.ttf");
	public static FontFile FellIt => _fellIt ??= GD.Load<FontFile>("res://assets/fonts/IMFeENit28P-Lining.ttf");

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
	/// <summary>
	/// Soft hyphens in long words, so a justified narrow column can break a word as a compositor
	/// would instead of opening rivers between three words. A plain syllable rule: break after a
	/// vowel that is followed by a consonant, never within three letters of either end.
	/// </summary>
	public static string Hyphenate(string text)
	{
		var sb = new System.Text.StringBuilder(text.Length + 16);
		var word = new System.Text.StringBuilder();
		var inTag = false;
		void Flush()
		{
			var w = word.ToString();
			word.Clear();
			if (w.Length < 6) { sb.Append(w); return; }
			sb.Append(Breaks(w));
		}
		foreach (var ch in text)
		{
			if (inTag) { sb.Append(ch); if (ch == ']') inTag = false; continue; }
			if (ch == '[') { Flush(); inTag = true; sb.Append(ch); continue; }
			if (char.IsLetter(ch)) { word.Append(ch); continue; }
			Flush();
			sb.Append(ch);
		}
		Flush();
		return sb.ToString();
	}

	/// <summary>Soft hyphens wherever the American hyphenation patterns allow (see <see cref="Hyphenator"/>).</summary>
	private static string Breaks(string w)
	{
		var cuts = Hyphenator.Points(w);
		if (cuts.Count == 0) return w;
		var o = new System.Text.StringBuilder(w.Length + cuts.Count);
		for (var k = 0; k < w.Length; k++)
		{
			if (cuts.Contains(k)) o.Append('\u00ad');
			o.Append(w[k]);
		}
		return o.ToString();
	}

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
		var wrapped = centre ? $"[center]{bbcode}[/center]" : justify ? $"[fill]{Hyphenate(bbcode)}[/fill]" : bbcode;
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

	/// <summary>A control that uses the arrow keys itself (the still's bottle shelf): the drawers' arrow
	/// navigation leaves them to it.</summary>
	public interface IArrowKeys { }

	/// <summary>Every node of type T under a node, depth first.</summary>
	public static System.Collections.Generic.IEnumerable<T> Descendants<T>(Node n) where T : Node
	{
		foreach (var c in n.GetChildren())
		{
			if (c is T t) yield return t;
			foreach (var d in Descendants<T>(c)) yield return d;
		}
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

	private static LabelledCut? _remedyCut;

	/// <summary>A stock cut by name. The remedy's bottle (<c>wc_bottle_panacea</c>) comes with the
	/// shop's own remedy set on its label (see <see cref="LabelledCut"/>).</summary>
	public static Texture2D? Cut(string name)
	{
		if (string.IsNullOrEmpty(name) || !ResourceLoader.Exists($"res://assets/art/cuts/{name}.png")) return null;
		var tex = GD.Load<Texture2D>($"res://assets/art/cuts/{name}.png");
		if (name != "wc_bottle_panacea") return tex;
		return _remedyCut ??= new LabelledCut(tex);
	}

	/// <summary>
	/// A printed ticket: woodtype caption inside a ruled border. Hovering inks it in (the colours
	/// invert); "solid" tickets are the main action of a slip and start inked.
	/// </summary>
	public static Button Button(string text, int size = 18, bool solid = false, string? sound = "ticket")
	{
		var b = new Button { Text = text, FocusMode = Control.FocusModeEnum.All, MouseDefaultCursorShape = Control.CursorShape.PointingHand };
		b.AddThemeFontOverride("font", Rye);
		b.AddThemeFontSizeOverride("font_size", size);
		var fg = solid ? Paper : Ink;
		var bg = solid ? Ink : Paper;
		b.AddThemeColorOverride("font_color", fg);
		// hover thickens the rule and washes the ticket; only pressing (and being set) inks it in,
		// so a ticket under the hand never looks like one already chosen
		b.AddThemeColorOverride("font_hover_color", fg);
		b.AddThemeColorOverride("font_pressed_color", bg);
		b.AddThemeColorOverride("font_hover_pressed_color", bg);
		b.AddThemeColorOverride("font_focus_color", fg);
		// a ticket that cannot be pressed is never inked: outlined faintly, whatever its kind
		b.AddThemeColorOverride("font_disabled_color", new Color(Ink, 0.6f));
		StyleBoxFlat Make(Color fill, Color border, int width)
		{
			var st = Box(fill, border, width, 6);
			st.ContentMarginLeft = st.ContentMarginRight = Mathf.Max(10, size * 0.7f);
			st.ContentMarginTop = st.ContentMarginBottom = Mathf.Max(4, size * 0.3f);
			return st;
		}
		b.AddThemeStyleboxOverride("normal", Make(bg, Ink, 2));
		b.AddThemeStyleboxOverride("hover", Make(solid ? new Color("3a3434") : new Color("dcd8ce"), Ink, 3));
		b.AddThemeStyleboxOverride("pressed", Make(fg, Ink, 3));
		b.AddThemeStyleboxOverride("hover_pressed", Make(fg, Ink, 3));
		b.AddThemeStyleboxOverride("disabled", Make(Paper, new Color(Ink, 0.5f), 1));
		Anim.LiftOnHover(b);
		b.MouseEntered += () => { if (!b.Disabled) Sfx.Play("tick", -14f, 0.15f); };
		if (sound != null) b.Pressed += () => Sfx.Play(sound);
		var focus = Make(new Color(0, 0, 0, 0), Ink, 2);
		focus.ExpandMarginLeft = focus.ExpandMarginRight = focus.ExpandMarginTop = focus.ExpandMarginBottom = 4;
		b.AddThemeStyleboxOverride("focus", focus);
		return b;
	}

	/// <summary>A thin paper-white scrollbar that suits the black table.</summary>
	/// <summary>
	/// Where keyboard focus sits inside a panel that is about to be rebuilt: the focused
	/// control's path from the panel (child indices), or null. A rebuilt panel has the same shape,
	/// so the same path finds the same button. Pair with <see cref="RestoreFocus"/> so a keyboard
	/// player does not start again from the top after every action.
	/// </summary>
	public static int[]? FocusIndex(Control root)
	{
		if (!root.IsInsideTree()) return null;
		var f = root.GetViewport().GuiGetFocusOwner();
		if (f == null || !root.IsAncestorOf(f)) return null;
		var path = new List<int>();
		for (Node n = f; n != root; n = n.GetParent()) path.Insert(0, n.GetIndex());
		return path.ToArray();
	}

	/// <summary>Gives focus back to the same place in the rebuilt panel, or the nearest live control.</summary>
	public static void RestoreFocus(Control root, int[]? path)
	{
		if (path == null) return;
		Callable.From(() =>
		{
			if (!GodotObject.IsInstanceValid(root) || !root.IsInsideTree()) return;
			Node n = root;
			foreach (var i in path)
			{
				var kids = n.GetChildren().Where(k => !k.IsQueuedForDeletion()).ToList();
				if (i >= kids.Count) { n = kids.Count > 0 ? kids[^1] : n; break; }
				n = kids[i];
			}
			var list = Focusables(root);
			if (list.Count == 0) return;
			// that control, if it can still take focus; otherwise the next one after it
			var at = n is Control c ? list.IndexOf(c) : -1;
			if (at < 0) at = list.FindIndex(x => n.IsAncestorOf(x) || IsAfter(x, n));
			if (at < 0) at = list.Count - 1;
			for (var k = 0; k < list.Count; k++)
			{
				var cand = list[(at + k) % list.Count];
				if (cand is BaseButton { Disabled: true }) continue;
				cand.GrabFocus();
				return;
			}
		}).CallDeferred();
	}

	private static bool IsAfter(Node a, Node b)
	{
		// tree order: is a after b?
		var pa = new List<int>(); for (var n = a; n != null; n = n.GetParent()) pa.Insert(0, n.GetIndex());
		var pb = new List<int>(); for (var n = b; n != null; n = n.GetParent()) pb.Insert(0, n.GetIndex());
		for (var i = 0; i < Math.Min(pa.Count, pb.Count); i++) if (pa[i] != pb[i]) return pa[i] > pb[i];
		return pa.Count > pb.Count;
	}

	/// <summary>Disables a ticket and takes it out of the Tab order, or brings it back.</summary>
	public static void SetLive(BaseButton b, bool live)
	{
		b.Disabled = !live;
		b.FocusMode = live ? Control.FocusModeEnum.All : Control.FocusModeEnum.None;
	}

	public static List<Control> Focusables(Control root)
	{
		var list = new List<Control>();
		void Walk(Node n)
		{
			foreach (var k in n.GetChildren())
			{
				if (k is Control c && !c.IsQueuedForDeletion())
				{
					if (!c.Visible) continue;
					if (c.FocusMode == Control.FocusModeEnum.All) list.Add(c);
				}
				Walk(k);
			}
		}
		Walk(root);
		return list;
	}

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
