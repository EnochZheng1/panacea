using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Panacea.UI;

/// <summary>
/// The text-size setting. At REGULAR the Gazette is set as designed; at LARGE and LARGER IM Fell
/// type is set no smaller than a floor, wherever its frame has room (see the fitting below). The
/// small print the player acts on (the counter cards' health, the works' suspicion, the drawers'
/// sub-labels, the jars' prices) is IM Fell at 11–13 px, which is 5 px of x-height; the floors give
/// it 1.5 and 1.7 times that. Type already above
/// the floor is left alone, and so are the woodtype capitals (Rye), whose 11 px is already taller
/// than the floor's x-height: the page keeps its shape and its buttons their widths.
///
/// The rule is applied to every Label, Button and RichTextLabel as it enters the scene
/// tree (<see cref="Attach"/>), so every screen and drawer gets it however it was built, and to
/// everything on screen when the setting changes (<see cref="Reapply"/>), so it takes effect at
/// once. A control remembers the size it was built with, so the rule can be applied again, or
/// undone, without the sizes creeping.
///
/// Larger type must never cost the player text, so a fitting pass follows each change: a line
/// that would now be cut off by the frame it is printed in (the proof's columns, a card at the
/// counter), or trimmed short, is stepped back towards its designed size until it fits. What it
/// learns is kept, so a slip rebuilt after a click comes back at the fitted size without a flicker.
/// </summary>
public static class LargePrint
{
	/// <summary>0 regular, 1 large, 2 larger.</summary>
	public static int Level { get; private set; }

	/// <summary>The smallest IM Fell type at each level, in pixels. IM Fell's x-height is 0.44–0.47
	/// em, so 17 px gives about 7.5 px of x-height (1.5 times the regular 5 px) and 19 px about 8.5.</summary>
	private static readonly int[] Floors = { 0, 17, 19 };

	/// <summary>The smallest woodtype at each level: none. Rye's capitals at its smallest size (11 px) already stand
	/// 8.4 px tall, above the floors' x-height, and a wider ticket would widen its row and squeeze the proof.</summary>
	private static readonly int[] WoodFloors = { 0, 0, 0 };

	public static readonly string[] Names = { "REGULAR", "LARGE", "LARGER" };

	/// <summary>The size to set IM Fell type at, given the size it was designed at. Text drawn by
	/// hand (DrawString) should pass its size through this as well.</summary>
	public static int Size(int designed) => Level == 0 ? designed : Math.Max(designed, Floors[Level]);

	private static int WoodSize(int designed) => Level == 0 ? designed : Math.Max(designed, WoodFloors[Level]);

	/// <summary>One of three values by the text size: as designed, LARGE, LARGER. For type a screen sizes
	/// itself (woodtype, which the floors leave alone, on a card that must grow with the rest).</summary>
	public static T Pick<T>(T regular, T large, T larger) => Level switch { 0 => regular, 1 => large, _ => larger };

	/// <summary>
	/// Marks a node whose layout is designed for every text size (the proof page, the counter, the
	/// bench, the formula book): the type under it is set at the floor and never stepped back by the
	/// fitting pass. What runs long there scrolls, by design, rather than shrinking. Set it before the
	/// node's children enter the tree.
	/// </summary>
	public static void NoFit(Node n) => n.SetMeta("lp_nofit", true);

	private static bool Unfitted(Node c)
	{
		for (var p = c; p != null; p = p.GetParent()) if (p.HasMeta("lp_nofit")) return true;
		return false;
	}

	/// <summary>Raised after the level changes and every control has been reset.</summary>
	public static event Action? Changed;

	private static SceneTree? _tree;

	/// <summary>Hooks the tree, so each control entering it is set at the current level.</summary>
	public static void Attach(SceneTree tree)
	{
		if (_tree == tree) return;
		_tree = tree;
		tree.NodeAdded += Apply;
	}

	public static void SetLevel(int level)
	{
		level = Math.Clamp(level, 0, Floors.Length - 1);
		if (level == Level) return;
		Level = level;
		if (_tree?.Root != null) Reapply(_tree.Root);
		Changed?.Invoke();
	}

	/// <summary>Resets every control under a node to the current level.</summary>
	public static void Reapply(Node root)
	{
		Apply(root);
		foreach (var c in root.GetChildren()) Reapply(c);
	}

	private static readonly StringName[] LabelSizes = { "font_size" };
	private static readonly StringName[] RichSizes = { "normal_font_size", "italics_font_size", "bold_font_size", "bold_italics_font_size", "mono_font_size" };
	private static readonly StringName[] RichFonts = { "normal_font", "italics_font", "bold_font", "bold_italics_font", "mono_font" };

	private static void Apply(Node n)
	{
		switch (n)
		{
			case RichTextLabel r:
				for (var i = 0; i < RichSizes.Length; i++) Set(r, RichSizes[i], RichFonts[i]);
				break;
			case Label or Button:
				Set((Control)n, LabelSizes[0], "font");
				break;
		}
	}

	private static bool IsWood(Font? f) => f == Print.Rye;

	/// <summary>Fitted sizes learned at each level, by font, designed size and text.</summary>
	private static readonly Dictionary<string, int>[] Fitted = { new(), new(), new() };

	private static string Key(Control c, StringName name, int designed) =>
		$"{name}|{designed}|{c switch { Label l => l.Text, Button b => b.Text, RichTextLabel r => r.Text, LineEdit e => e.Text, _ => "" }}";

	private static void Set(Control c, StringName name, StringName fontName)
	{
		if (!c.HasThemeFontSizeOverride(name)) return;
		var now = c.GetThemeFontSize(name);
		// the size the control was built with: remembered the first time, kept while nothing
		// else has changed the size since
		var baseKey = "lp_base_" + name;
		var setKey = "lp_set_" + name;
		var designed = c.HasMeta(baseKey) && c.HasMeta(setKey) && (int)c.GetMeta(setKey) == now ? (int)c.GetMeta(baseKey) : now;
		var want = IsWood(c.GetThemeFont(fontName)) ? WoodSize(designed) : Size(designed);
		var designedLayout = Unfitted(c);
		if (want > designed && !designedLayout && Fitted[Level].TryGetValue(Key(c, name, designed), out var fit)) want = Math.Max(designed, Math.Min(want, fit));
		c.SetMeta(baseKey, designed);
		c.SetMeta(setKey, want);
		if (c is Label l)
		{
			if (want <= designed) Unhold(l);
			else if (!designedLayout && Holds[Level].Contains(Key(c, name, designed))) Hold(l);
		}
		if (want != now) c.AddThemeFontSizeOverride(name, want);
		if (want > designed && !designedLayout) Grew(c);
	}

	/// <summary>One-line labels that had to be held to their designed width at each level, by key.</summary>
	private static readonly HashSet<string>[] Holds = { new(), new(), new() };

	private static bool CanHold(Label l) =>
		!l.HasMeta("lp_held") && l.AutowrapMode == TextServer.AutowrapMode.Off && !l.ClipText
		&& l.TextOverrunBehavior == TextServer.OverrunBehavior.NoTrimming && l.Text.Length > 0;

	/// <summary>
	/// A one-line label set larger that has squeezed its neighbours in a row (so that their words
	/// break mid-word) keeps the width it had at its designed size and runs on to a second line
	/// instead: "suspicion: the Board of Health is asking (87 of 100)" in THE WORKS. Where the row has
	/// room (a column of a price list), it is left to widen.
	/// </summary>
	private static void Hold(Label l)
	{
		if (!CanHold(l)) return;
		var designed = l.HasMeta("lp_base_font_size") ? (int)l.GetMeta("lp_base_font_size") : l.GetThemeFontSize("font_size");
		var w = l.GetThemeFont("font").GetStringSize(l.Text, HorizontalAlignment.Left, -1, designed).X;
		l.SetMeta("lp_held", l.CustomMinimumSize.X);
		l.CustomMinimumSize = new Vector2(Mathf.Max(l.CustomMinimumSize.X, Mathf.Ceil(w) + 1), l.CustomMinimumSize.Y);
		l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		Holds[Level].Add(Key(l, "font_size", designed));
	}

	private static void Unhold(Label l)
	{
		if (!l.HasMeta("lp_held")) return;
		l.CustomMinimumSize = new Vector2((float)l.GetMeta("lp_held"), l.CustomMinimumSize.Y);
		l.AutowrapMode = TextServer.AutowrapMode.Off;
		l.RemoveMeta("lp_held");
	}

	// ---------- fitting ----------

	private static readonly List<WeakReference<Control>> _grown = new();
	private static int _passes;
	private static bool _queued;

	private static void Grew(Control c)
	{
		_grown.Add(new WeakReference<Control>(c));
		_passes = 0;
		Queue();
	}

	private static void Queue()
	{
		if (_queued || _tree == null) return;
		_queued = true;
		// a frame later, when the containers have laid the new type out
		_tree.ProcessFrame += Pass;
	}

	private static void Pass()
	{
		_tree!.ProcessFrame -= Pass;
		_queued = false;
		var shrink = new HashSet<Control>();
		var frames = new HashSet<Control>();
		var rows = new HashSet<HBoxContainer>();
		var columns = new HashSet<VBoxContainer>();
		for (var i = _grown.Count - 1; i >= 0; i--)
		{
			if (!_grown[i].TryGetTarget(out var c) || !GodotObject.IsInstanceValid(c) || !c.IsInsideTree() || c.IsQueuedForDeletion())
			{
				_grown.RemoveAt(i);
				continue;
			}
			if (!c.IsVisibleInTree() || !Boosted(c)) continue;
			if (Trimmed(c)) shrink.Add(c);
			foreach (var f in FramesOf(c)) frames.Add(f);
			for (var p = c.GetParent(); p is Control and not ScrollContainer; p = p.GetParent())
				if (p is HBoxContainer h) { rows.Add(h); break; }
			for (var p = c.GetParent(); p is Control and not ScrollContainer; p = p.GetParent())
				if (p is VBoxContainer v) columns.Add(v);
		}
		// a row whose words now break mid-word has been squeezed by a one-line label grown wider
		// beside them: hold those to their designed width before stepping anything back
		var held = false;
		foreach (var row in rows)
		{
			var texts = Texts(row).Where(t => t.IsVisibleInTree()).ToList();
			if (!texts.Any(Trimmed)) continue;
			var holdable = texts.OfType<Label>().Where(l => Boosted(l) && CanHold(l)).ToList();
			foreach (var l in holdable) Hold(l);
			if (holdable.Count > 0) held = true;
		}
		// a held label changes the layout around it: look again next frame, before stepping back
		// anything the squeeze only seemed to push out
		if (held)
		{
			if (++_passes < 24) Queue();
			return;
		}
		// a scroll that shares its column with larger type must keep room to be read and scrolled
		// (the type case, beside the works and the warnings): step back the type around it
		foreach (var v in columns)
			foreach (var k in v.GetChildren())
				if (k is ScrollContainer { Visible: true } sc && sc.Size.Y < Math.Min(60f, sc.GetChildOrNull<Control>(0)?.GetCombinedMinimumSize().Y ?? 0f))
					foreach (var other in v.GetChildren())
						if (other != sc) foreach (var b in Texts(other)) if (Boosted(b)) shrink.Add(b);
		// larger type pushes the lines after it down: look at every line in the frame, not only
		// the larger ones, and step back the larger ones in the column that ran out
		foreach (var f in frames)
			foreach (var t in Texts(f))
				if (t.IsVisibleInTree() && Escapes(t) is { } scope)
				{
					foreach (var b in Texts(scope)) if (Boosted(b)) shrink.Add(b);
					// and the type set beside the frame in its own column (a masthead run onto a
					// second line), which takes its height from it
					if (f.ClipContents && f.GetParent() is BoxContainer box)
						foreach (var k in box.GetChildren())
							if (k != f) foreach (var b in Texts(k)) if (Boosted(b)) shrink.Add(b);
				}
		foreach (var c in shrink) Step(c);
		// settle within a few frames; a frame that changes nothing ends it
		if ((shrink.Count > 0 || held) && ++_passes < 24) Queue();
	}

	/// <summary>The frames a control is printed in: its clipping frame, and a box set by hand at a fixed place.</summary>
	private static IEnumerable<Control> FramesOf(Control c)
	{
		Node child = c;
		var clip = false;
		for (var p = c.GetParent(); p is Control pc; child = p, p = p.GetParent())
		{
			if (pc is ScrollContainer) yield break;
			if (pc.ClipContents && !clip) { clip = true; yield return pc; }
			if (Placed(pc, child)) yield return (Control)child;
		}
	}

	private static bool Placed(Control parent, Node child) =>
		parent is not Container && child is Control placed && placed.AnchorLeft == 0 && placed.AnchorRight == 0 && placed.AnchorTop == 0 && placed.AnchorBottom == 0;

	private static bool Boosted(Control c)
	{
		foreach (var name in c is RichTextLabel ? RichSizes : LabelSizes)
		{
			var setKey = "lp_set_" + name;
			if (c.HasMeta(setKey) && (int)c.GetMeta(setKey) > (int)c.GetMeta("lp_base_" + name)) return true;
		}
		return false;
	}

	/// <summary>A step back towards the designed size, and remember it.</summary>
	private static void Step(Control c)
	{
		foreach (var name in c is RichTextLabel ? RichSizes : LabelSizes)
		{
			var setKey = "lp_set_" + name;
			var baseKey = "lp_base_" + name;
			if (!c.HasMeta(setKey)) continue;
			var set = (int)c.GetMeta(setKey);
			var designed = (int)c.GetMeta(baseKey);
			if (set <= designed) continue;
			// big steps while far from the designed size, single pixels near it
			var next = set - Math.Max(1, (set - designed) / 3);
			c.SetMeta(setKey, next);
			c.AddThemeFontSizeOverride(name, next);
			Fitted[Level][Key(c, name, designed)] = next;
		}
	}

	private static IEnumerable<Control> Texts(Node scope)
	{
		if (scope is Label or Button or RichTextLabel or LineEdit) yield return (Control)scope;
		foreach (var k in scope.GetChildren())
			foreach (var t in Texts(k)) yield return t;
	}

	/// <summary>A one-line label whose text no longer fits its box (it would be cut with an ellipsis).</summary>
	internal static bool Trimmed(Control c)
	{
		// a word too wide for its box is broken mid-word ("Pepperm / int"), or a box squeezed to
		// nothing sets a letter a line: never for the sake of size
		if (c is Label { AutowrapMode: not TextServer.AutowrapMode.Off } || c is RichTextLabel { AutowrapMode: not TextServer.AutowrapMode.Off })
		{
			var text = c is Label lw ? lw.Text : ((RichTextLabel)c).GetParsedText();
			if (string.IsNullOrWhiteSpace(text)) return false;
			if (c.Size.X <= 4) return true;
			var font = c.GetThemeFont(c is Label ? "font" : "normal_font");
			var px = c.GetThemeFontSize(c is Label ? "font_size" : "normal_font_size");
			foreach (var word in text.Split(new[] { ' ', '\n', '­', '-', '—', '/' }, StringSplitOptions.RemoveEmptyEntries))
				if (font.GetStringSize(word, HorizontalAlignment.Left, -1, px).X > c.Size.X + 0.5f) return true;
			return false;
		}
		if (c is not Label { AutowrapMode: TextServer.AutowrapMode.Off } l) return false;
		if (!l.ClipText && l.TextOverrunBehavior == TextServer.OverrunBehavior.NoTrimming) return false;
		var w = l.GetThemeFont("font").GetStringSize(l.Text, HorizontalAlignment.Left, -1, l.GetThemeFontSize("font_size")).X;
		return w > l.Size.X + 1;
	}

	/// <summary>
	/// Whether the type now runs out of the frame it is printed in, and if so the type to step back:
	/// cut off by a clipping frame (the proof's columns), the lines of its own column; past the
	/// edge of a box set by hand at a fixed place (a card at the counter), everything on that box.
	/// A scroll is not a frame: what runs long there is scrolled to.
	/// </summary>
	internal static Node? Escapes(Control c)
	{
		if (!Mathf.IsZeroApprox(c.GetGlobalTransform().Rotation)) return null;
		var r = c.GetGlobalRect().Grow(-1);
		Node child = c;
		VBoxContainer? column = null;
		for (var p = c.GetParent(); p is Control pc; child = p, p = p.GetParent())
		{
			if (pc is ScrollContainer) return null;
			var pr = pc.GetGlobalRect().Grow(2);
			if (pc.ClipContents && pr.Size.X > 0 && pr.Size.Y > 0 && !pr.Encloses(r))
				return column ?? (Node)c;
			// a box laid at a fixed place in a plain (not laid-out) parent: its edge is the parent's,
			// and its width the one it was given (a card at the counter pushed wider sits on the next)
			if (Placed(pc, child) && pr.Size.X > 0 && pr.Size.Y > 0 && child is Control placed
				&& (!pr.Encloses(r) || placed != c && placed.CustomMinimumSize.X > 0 && placed.Size.X > placed.CustomMinimumSize.X + 1))
				return placed;
			if (pc is VBoxContainer v) column = v;
		}
		return null;
	}

	/// <summary>The size a control's type is set at on this level before any fitting (for the dev check).</summary>
	internal static int Target(Control c, string name, int designed)
	{
		var font = c.GetThemeFont(c is RichTextLabel ? RichFonts[Array.IndexOf(RichSizes, (StringName)name)] : "font");
		return IsWood(font) ? WoodSize(designed) : Size(designed);
	}
}
