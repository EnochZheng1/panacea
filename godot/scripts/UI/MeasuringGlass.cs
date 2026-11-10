using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Panacea.UI;

/// <summary>
/// A graduated measuring glass, drawn in ink. The mixture is the product, so it is the one thing
/// printed in green: each ingredient is a band of it, paler for thin drugs (water) and denser for
/// strong ones (laudanum), told apart by the engraver's shading. Levels rise and fall smoothly;
/// a pour draws a stream into the glass; an overfull glass trembles.
/// </summary>
public partial class MeasuringGlass : Control
{
	public double Capacity = 8;
	private readonly List<(string Name, double Target, double Shown, double Opacity)> _layers = new();
	private float _pour;
	private float _shake;

	public MeasuringGlass()
	{
		CustomMinimumSize = new Vector2(196, 430);
		MouseFilter = MouseFilterEnum.Ignore;
	}

	/// <summary>Sets the recipe. Bands keep their order of first pouring, like real layers.</summary>
	public void Set(IReadOnlyList<(string Name, double Ounces, double Opacity)> recipe)
	{
		var keep = new List<(string, double, double, double)>();
		foreach (var l in _layers)
		{
			var now = recipe.FirstOrDefault(r => r.Name == l.Name);
			keep.Add((l.Name, now.Name == null ? 0 : now.Ounces, l.Shown, l.Opacity));
		}
		foreach (var r in recipe)
			if (keep.TrueForAll(k => k.Item1 != r.Name)) keep.Add((r.Name, r.Ounces, 0, r.Opacity));
		_layers.Clear();
		foreach (var k in keep) _layers.Add(k);
		QueueRedraw();
	}

	public void Pour() => _pour = 0.35f;

	public override void _Process(double delta)
	{
		var moving = false;
		for (var i = 0; i < _layers.Count; i++)
		{
			var (n, target, shown, op) = _layers[i];
			if (Math.Abs(target - shown) > 0.002)
			{
				shown += (target - shown) * Math.Min(1, delta * 9);
				_layers[i] = (n, target, shown, op);
				moving = true;
			}
		}
		_layers.RemoveAll(l => l.Target <= 0 && l.Shown < 0.01);
		if (_pour > 0) { _pour -= (float)delta; moving = true; }
		var over = Total() > Capacity + 1e-6;
		_shake = over ? (_shake + (float)delta * 40) : 0;
		if (moving || over) QueueRedraw();
	}

	private double Total()
	{
		double t = 0;
		foreach (var l in _layers) t += l.Target;
		return t;
	}

	public override void _Draw()
	{
		var ink = Print.Ink;
		var w = Size.X;
		var top = 46f;
		var bottom = Size.Y - 26f;
		var left = 30f;
		var right = 96f;
		var off = Total() > Capacity + 1e-6 ? Mathf.Sin(_shake) * 2.2f : 0f;
		left += off; right += off;
		var height = bottom - top;
		// the liquid cannot stand above the lip: what doesn't fit runs down the outside
		float Y(double oz) => Math.Max(top - 6, bottom - (float)(oz / (Capacity * 1.1) * height));

		// the stream from the shelf
		if (_pour > 0)
		{
			var a = Mathf.Clamp(_pour / 0.35f, 0, 1);
			var x = (left + right) / 2 + 6;
			DrawLine(new Vector2(x, 0), new Vector2(x, Y(Shown())), new Color(Print.Green, a), 3.2f);
			DrawCircle(new Vector2(x - 6, Y(Shown()) + 6), 2.2f * a, new Color(Print.Paper, a));
			DrawCircle(new Vector2(x + 5, Y(Shown()) + 12), 1.6f * a, new Color(Print.Paper, a));
		}

		// the liquid, band by band
		double level = 0;
		var font = Print.FellIt;
		for (var i = 0; i < _layers.Count; i++)
		{
			var (name, _, shown, op) = _layers[i];
			if (shown <= 0.001) continue;
			var y0 = Y(level);
			var y1 = Y(level + shown);
			var tone = Print.Paper.Lerp(Print.Green, (float)Mathf.Clamp(0.22 + 0.78 * op, 0, 1));
			var r = new Rect2(left + 3, y1, right - left - 6, y0 - y1);
			DrawRect(r, tone);
			Shade(r, i % 4);
			DrawLine(new Vector2(left + 3, y1), new Vector2(right - 3, y1), new Color(ink, 0.55f), 1f);
			if (y0 - y1 > 11)
				DrawString(font, new Vector2(right + 8, (y0 + y1) / 2 + 5), $"{name} {Fmt(shown)}", HorizontalAlignment.Left, w - right - 10, LargePrint.Size(12), ink);
			level += shown;
		}
		// the meniscus
		if (level > 0.01)
		{
			var y = Y(level);
			DrawArc(new Vector2((left + right) / 2, y - 8), (right - left) / 2 - 3, 0.25f, Mathf.Pi - 0.25f, 16, new Color(ink, 0.6f), 1.2f);
		}

		// the glass: lip, walls, foot, and the graduations
		DrawLine(new Vector2(left, top - 8), new Vector2(left, bottom), ink, 2.6f);
		DrawLine(new Vector2(right, top - 8), new Vector2(right, bottom), ink, 2.6f);
		DrawLine(new Vector2(left - 6, top - 8), new Vector2(left, top - 8), ink, 2.6f);
		DrawLine(new Vector2(left - 6, top - 8), new Vector2(left - 2, top - 2), ink, 2.2f);
		DrawRect(new Rect2(left - 14, bottom, right - left + 28, 8), ink);
		DrawLine(new Vector2(left + 5, top), new Vector2(left + 5, bottom - 6), new Color(Print.Paper, 0.85f), 2.5f);
		for (var oz = 1; oz <= Capacity; oz++)
		{
			var y = Y(oz);
			DrawLine(new Vector2(left, y), new Vector2(left + 14, y), ink, 1.6f);
			DrawString(Print.Fell, new Vector2(left - 26, y + 5), oz.ToString(), HorizontalAlignment.Right, 20, LargePrint.Size(13), ink);
			if (oz < Capacity) DrawLine(new Vector2(left, Y(oz + 0.5)), new Vector2(left + 7, Y(oz + 0.5)), ink, 1f);
		}
		var full = Y(Capacity);
		DrawDashedLine(new Vector2(left, full), new Vector2(right, full), ink, 1.4f, 5);
		DrawString(Print.FellIt, new Vector2(left - 6, full - 8), "a full bottle", HorizontalAlignment.Left, -1, LargePrint.Size(12), ink);
		if (Total() > Capacity + 1e-6)
		{
			DrawString(Print.Rye, new Vector2(left - 10, top - 18), "IT OVERFLOWS", HorizontalAlignment.Left, -1, LargePrint.Size(15), ink);
			var g = Print.Paper.Lerp(Print.Green, 0.55f);
			DrawLine(new Vector2(right + 1, top - 6), new Vector2(right + 3, bottom - 30), g, 3f);
			DrawLine(new Vector2(left - 1, top - 6), new Vector2(left - 4, bottom - 60), g, 2.4f);
			DrawRect(new Rect2(left - 18, bottom - 4, right - left + 40, 4), g);
		}
	}

	private double Shown()
	{
		double t = 0;
		foreach (var l in _layers) t += l.Shown;
		return t;
	}

	/// <summary>The engraver tells the bands apart with shading, not colour.</summary>
	private void Shade(Rect2 r, int pattern)
	{
		var c = new Color(Print.Ink, 0.5f);
		switch (pattern)
		{
			case 1:
				for (var y = r.Position.Y + 3; y < r.End.Y; y += 5) DrawLine(new Vector2(r.Position.X, y), new Vector2(r.End.X, y), c, 1f);
				break;
			case 2:
				for (var x = r.Position.X + 3; x < r.End.X; x += 6)
					for (var y = r.Position.Y + 3; y < r.End.Y; y += 6) DrawCircle(new Vector2(x, y), 0.9f, c);
				break;
			case 3:
				for (var x = r.Position.X + 3; x < r.End.X; x += 5) DrawLine(new Vector2(x, r.Position.Y), new Vector2(x, r.End.Y), c, 1f);
				break;
		}
	}

	private static string Fmt(double oz) => oz switch
	{
		< 0.3 => "¼ oz",
		_ when Math.Abs(oz - Math.Round(oz)) < 0.05 => $"{Math.Round(oz):0} oz",
		_ when Math.Abs(oz * 2 - Math.Round(oz * 2)) < 0.05 => $"{Math.Floor(oz):0}½ oz".Replace("0½", "½"),
		_ => $"{oz:0.##} oz",
	};
}
