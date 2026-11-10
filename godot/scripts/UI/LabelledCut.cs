using System;
using System.Collections.Generic;
using Godot;

namespace Panacea.UI;

/// <summary>
/// The remedy's bottle, with its label set in type. The engraving (<c>wc_bottle_panacea</c>) leaves
/// its label blank, so a notice never shows a bottle of some other remedy's "PANACEA … ONE DOLLAR";
/// this texture draws the engraving and then prints the shop's own remedy on the label, its name
/// in woodtype and its price under it, wherever the cut is printed (the paper's notices and lead
/// stories, the proof's space, the clippings). Set too small to read (a jar on the drug shelf, the
/// shelf behind the counter), the label is left blank, as a printer's stock cut would be.
/// </summary>
public partial class LabelledCut : Texture2D
{
	/// <summary>The remedy the shop sells first, and its price, as the desk last showed them.</summary>
	public static string? Remedy;
	public static long PriceCents;

	private Texture2D? _engraving;
	// the blank label's printable area, as fractions of the cut (inside its ruled border, between
	// the short rules at its head and foot)
	private static readonly Rect2 Area = new(0.315f, 0.528f, 0.37f, 0.245f);

	public LabelledCut() { }

	public LabelledCut(Texture2D engraving) { _engraving = engraving; }

	public override int _GetWidth() => _engraving?.GetWidth() ?? 1;
	public override int _GetHeight() => _engraving?.GetHeight() ?? 1;
	public override bool _HasAlpha() => true;
	public override bool _IsPixelOpaque(int x, int y) => true;

	public override void _Draw(Rid toCanvasItem, Vector2 pos, Color modulate, bool transpose) =>
		_DrawRect(toCanvasItem, new Rect2(pos, GetSize()), false, modulate, transpose);

	public override void _DrawRect(Rid toCanvasItem, Rect2 rect, bool tile, Color modulate, bool transpose)
	{
		if (_engraving == null) return;
		_engraving.DrawRect(toCanvasItem, rect, tile, modulate, transpose);
		if (!tile && !transpose) Letter(toCanvasItem, rect, new Rect2(Vector2.Zero, GetSize()), GetSize(), modulate);
	}

	public override void _DrawRectRegion(Rid toCanvasItem, Rect2 rect, Rect2 srcRect, Color modulate, bool transpose, bool clipUV)
	{
		if (_engraving == null) return;
		_engraving.DrawRectRegion(toCanvasItem, rect, srcRect, modulate, transpose, clipUV);
		if (!transpose) Letter(toCanvasItem, rect, srcRect, GetSize(), modulate);
	}

	/// <summary>Prints the remedy on the label, given where the texture's source region lands.</summary>
	private static void Letter(Rid ci, Rect2 rect, Rect2 src, Vector2 full, Color modulate)
	{
		var name = Remedy;
		if (string.IsNullOrWhiteSpace(name) || src.Size.X <= 0 || src.Size.Y <= 0) return;
		var k = new Vector2(rect.Size.X / src.Size.X, rect.Size.Y / src.Size.Y);
		// the label in the texture's pixels, then where it lands on screen
		var tex = new Rect2(Area.Position * full, Area.Size * full);
		var at = new Rect2(rect.Position + (tex.Position - src.Position) * k, tex.Size * k);
		if (at.Size.Y < 20 || at.Size.X < 24) return;
		var ink = new Color(Print.Ink, modulate.A);
		var wood = Print.Rye;
		var lines = Fit(name.ToUpperInvariant(), wood, at.Size, out var px);
		if (lines.Count == 0) return;
		var price = PriceCents > 0 ? $"{Paper.EditionWriter.Money(PriceCents)} the bottle" : "";
		var small = Mathf.Clamp((int)(at.Size.Y * 0.11f), 6, 22);
		var showPrice = price.Length > 0 && small >= 8 && Print.FellIt.GetStringSize(price, HorizontalAlignment.Left, -1, small).X <= at.Size.X;
		var lineH = wood.GetHeight(px) * 0.92f;
		var block = lineH * lines.Count + (showPrice ? small * 1.6f : 0);
		var y = at.Position.Y + (at.Size.Y - block) / 2f + wood.GetAscent(px);
		foreach (var line in lines)
		{
			var w = wood.GetStringSize(line, HorizontalAlignment.Left, -1, px).X;
			wood.DrawString(ci, new Vector2(at.Position.X + (at.Size.X - w) / 2f, y), line, HorizontalAlignment.Left, -1, px, ink);
			y += lineH;
		}
		if (showPrice)
		{
			var it = Print.FellIt;
			y += small * 0.5f;
			var w = it.GetStringSize(price, HorizontalAlignment.Left, -1, small).X;
			it.DrawString(ci, new Vector2(at.Position.X + (at.Size.X - w) / 2f, y), price, HorizontalAlignment.Left, -1, small, ink);
		}
	}

	/// <summary>The name broken into lines at the largest woodtype size that fits the label (up to four lines).</summary>
	private static List<string> Fit(string text, Font font, Vector2 room, out int px)
	{
		var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		var best = new List<string>();
		px = 0;
		for (var s = Math.Min(40, (int)(room.Y * 0.3f)); s >= 7; s--)
		{
			var lines = new List<string>();
			var cur = "";
			var fits = true;
			foreach (var w in words)
			{
				var next = cur.Length == 0 ? w : cur + " " + w;
				if (font.GetStringSize(next, HorizontalAlignment.Left, -1, s).X <= room.X) { cur = next; continue; }
				if (cur.Length == 0) { fits = false; break; }
				lines.Add(cur);
				cur = w;
				if (font.GetStringSize(cur, HorizontalAlignment.Left, -1, s).X > room.X) { fits = false; break; }
			}
			if (!fits) continue;
			if (cur.Length > 0) lines.Add(cur);
			// leave the foot of the label for the price
			if (lines.Count > 4 || font.GetHeight(s) * 0.92f * lines.Count > room.Y * 0.74f) continue;
			px = s;
			return lines;
		}
		return best;
	}
}
