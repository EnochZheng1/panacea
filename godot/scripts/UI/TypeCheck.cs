using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Panacea.UI;

/// <summary>
/// Dev tool for the text-size setting: with <c>-- --typecheck</c> (and <c>--shot</c>), a few frames
/// before the screenshot is taken it prints, for every text control on screen, the smallest type
/// in use and anything the type has pushed out of place: text running past the window or past a
/// clipping frame, text trimmed short, a rich-text box too small for its text, and two lines of
/// type laid over each other inside one container.
/// </summary>
public partial class TypeCheck : Node
{
	private int _frames;

	public TypeCheck(int frames) { _frames = Math.Max(1, frames - 3); }

	public override void _Process(double delta)
	{
		if (--_frames != 0) return;
		Report();
	}

	private sealed record Item(Control C, Rect2 R, int Size, string Text);

	private static string TextOf(Control c) => c switch
	{
		Label l => l.Text,
		Button b => b.Text,
		RichTextLabel r => r.GetParsedText(),
		LineEdit e => e.Text,
		_ => "",
	};

	private static int SizeOf(Control c) => c is RichTextLabel ? c.GetThemeFontSize("normal_font_size") : c.GetThemeFontSize("font_size");

	private void Report()
	{
		var view = GetViewport().GetVisibleRect();
		var items = new List<Item>();
		void Walk(Node n)
		{
			foreach (var k in n.GetChildren())
			{
				if (k is CanvasItem ci && !ci.Visible) continue;
				if (k is Control c && c is Label or Button or RichTextLabel or LineEdit && c.IsVisibleInTree())
				{
					var t = TextOf(c).Trim();
					// what shows of it: a scroll or a clipping frame hides the rest on purpose
					var r = c.GetGlobalRect();
					for (var p = c.GetParent(); p is Control pc; p = p.GetParent())
						if (pc is ScrollContainer) r = r.Intersection(pc.GetGlobalRect());
					if (t.Length > 0 && r.Size.X > 0 && r.Size.Y > 0 && Ink(c) > 0.3f && Mathf.IsZeroApprox(c.GetGlobalTransform().Rotation))
						items.Add(new Item(c, r, SizeOf(c), t.Replace("­", "").Replace("\n", " ")));
				}
				Walk(k);
			}
		}
		Walk(GetTree().Root);
		string Short(string s) => s.Length > 48 ? s[..48] + "…" : s;
		var onScreen = items.Where(i => i.R.Intersects(view)).ToList();
		var smallest = onScreen.OrderBy(i => i.Size).Take(6).ToList();
		GD.Print($"TYPECHECK level {LargePrint.Names[LargePrint.Level]}: {onScreen.Count} text controls, smallest type {smallest.FirstOrDefault()?.Size ?? 0} px");
		// x-height by face: IM Fell roman 0.458 em, italic 0.445; Rye 0.57 em (its capitals 0.76)
		var fell = onScreen.Where(i => i.C.GetThemeFont(i.C is RichTextLabel ? "normal_font" : "font") is var f && (f == Print.Fell || f == Print.FellIt)).OrderBy(i => i.Size).FirstOrDefault();
		var rye = onScreen.Where(i => i.C is not RichTextLabel && i.C.GetThemeFont("font") == Print.Rye).OrderBy(i => i.Size).FirstOrDefault();
		if (fell != null) GD.Print($"TYPECHECK   smallest IM Fell {fell.Size}px (x-height about {fell.Size * 0.45f:0.0} px): \"{Short(fell.Text)}\"");
		if (rye != null) GD.Print($"TYPECHECK   smallest woodtype {rye.Size}px (x-height {rye.Size * 0.57f:0.0} px, capitals {rye.Size * 0.76f:0.0} px): \"{Short(rye.Text)}\"");
		foreach (var i in smallest) GD.Print($"TYPECHECK   small {i.Size}px \"{Short(i.Text)}\" at {i.R.Position.Round()}");
		var problems = 0;
		void Problem(string s) { problems++; if (problems <= 40) GD.Print("TYPECHECK   " + s); }
		// type the fitting pass held back from the floor, for want of room
		bool Held(Control c, string name) => c.HasMeta("lp_set_" + name) && (int)c.GetMeta("lp_set_" + name) < LargePrint.Target(c, name, (int)c.GetMeta("lp_base_" + name));
		var held = onScreen.Where(i => Held(i.C, "font_size") || Held(i.C, "normal_font_size")).ToList();
		foreach (var i in held.Take(12)) GD.Print($"TYPECHECK   held back to {i.Size}px: \"{Short(i.Text)}\" at {i.R.Position.Round()}");
		if (held.Count > 12) GD.Print($"TYPECHECK   held back: {held.Count - 12} more");
		foreach (var i in onScreen)
		{
			if (!view.Grow(1).Encloses(i.R)) Problem($"off the window: \"{Short(i.Text)}\" {i.R}");
			if (LargePrint.Escapes(i.C) != null) Problem($"out of its frame: \"{Short(i.Text)}\" {i.R}");
			// a clipping frame that is not a scroll: what runs past it is lost
			for (var p = i.C.GetParent(); p != null; p = p.GetParent())
			{
				if (p is ScrollContainer) break;
				if (p is Control pc && pc.ClipContents)
				{
					var pr = pc.GetGlobalRect().Grow(1);
					if (!pr.Encloses(i.R) && pr.Intersects(i.R)) Problem($"cut off by a frame: \"{Short(i.Text)}\" {i.R} in {pc.GetGlobalRect()}");
					break;
				}
			}
			if (i.C is Label l && (l.ClipText || l.TextOverrunBehavior != TextServer.OverrunBehavior.NoTrimming) && l.AutowrapMode == TextServer.AutowrapMode.Off)
			{
				var w = l.GetThemeFont("font").GetStringSize(l.Text, HorizontalAlignment.Left, -1, i.Size).X;
				if (w > i.R.Size.X + 1) Problem($"trimmed: \"{Short(i.Text)}\" needs {w:0} px, has {i.R.Size.X:0}");
			}
			if (i.C is RichTextLabel { FitContent: false, ScrollActive: false } r && r.GetContentHeight() > i.R.Size.Y + 2)
				Problem($"rich text cut short: \"{Short(i.Text)}\" needs {r.GetContentHeight()} px, has {i.R.Size.Y:0}");
		}
		// two lines of type over each other, where a container laid them out
		for (var a = 0; a < onScreen.Count; a++)
			for (var b = a + 1; b < onScreen.Count; b++)
			{
				var x = onScreen[a]; var y = onScreen[b];
				var both = x.R.Intersection(y.R);
				if (both.Size.X <= 4 || both.Size.Y <= 4) continue;
				if (x.C.IsAncestorOf(y.C) || y.C.IsAncestorOf(x.C)) continue;
				var common = Common(x.C, y.C);
				if (common is not Container || common is ScrollContainer) continue;
				Problem($"overlap: \"{Short(x.Text)}\" {x.R} and \"{Short(y.Text)}\" {y.R}");
			}
		GD.Print($"TYPECHECK {problems} problem{(problems == 1 ? "" : "s")}");
	}

	private static float Ink(Control c)
	{
		var a = 1f;
		for (Node? n = c; n != null; n = n.GetParent()) if (n is CanvasItem ci) a *= ci.Modulate.A * ci.SelfModulate.A;
		return a;
	}

	private static Node? Common(Node a, Node b)
	{
		var up = new HashSet<Node>();
		for (var n = a; n != null; n = n.GetParent()) up.Add(n);
		for (var n = b; n != null; n = n.GetParent()) if (up.Contains(n)) return n;
		return null;
	}
}
