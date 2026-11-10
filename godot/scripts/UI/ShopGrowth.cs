using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Panacea.Game;

namespace Panacea.UI;

/// <summary>
/// The house's other stills, printed small in a row under the first in THE STILL drawer, so a
/// shop with two or three coppers shows two or three: each from the same engraving, numbered,
/// and with its fire drawn in when a batch is on it. Add it under <see cref="StillDrawing"/> and
/// call <see cref="Set"/> with the stills beyond the first and how many of them are lit.
/// </summary>
public partial class StillRow : Control
{
	private int _others;
	private int _lit;
	private float _t;
	private static Texture2D? _cut;
	private const float W = 92f, H = 112f, Gap = 8f;

	public StillRow()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		_cut ??= ResourceLoader.Exists("res://assets/art/wc_still.card.png") ? GD.Load<Texture2D>("res://assets/art/wc_still.card.png") : null;
		Set(0, 0);
	}

	/// <summary>The stills beyond the first, and how many of those have a batch on.</summary>
	public void Set(int others, int lit)
	{
		_others = Math.Clamp(others, 0, 3);
		_lit = Math.Clamp(lit, 0, _others);
		Visible = _others > 0;
		CustomMinimumSize = _others > 0 ? new Vector2(_others * (W + Gap) - Gap, H + 22) : Vector2.Zero;
		TooltipText = _others > 0 ? $"The house has {_others + 1} stills; {(_lit == 0 ? "the others are cold" : $"{_lit} more on the fire")}." : "";
		QueueRedraw();
	}

	public override void _Process(double delta)
	{
		_t += (float)delta;
		if (_lit > 0) QueueRedraw();
	}

	public override void _Draw()
	{
		var ink = Print.Ink;
		var paper = Print.Paper;
		var x0 = (Size.X - CustomMinimumSize.X) / 2f;
		for (var i = 0; i < _others; i++)
		{
			var at = new Vector2(x0 + i * (W + Gap), 0);
			if (_cut != null)
			{
				// the framed engraving of the first still (the card's top 620 x 726 at 2x)
				var k = _cut.GetWidth() / 620f;
				DrawTextureRectRegion(_cut, new Rect2(at, new Vector2(W, H)), new Rect2(0, 0, 620 * k, 726 * k));
			}
			else DrawRect(new Rect2(at, new Vector2(W, H)), ink, false, 1.5f);
			var s = W / 310f;
			var door = new Rect2(at + new Vector2(67, 267) * s, new Vector2(76, 37) * s);
			if (i < _lit)
			{
				// a small fire in the door, as the first still's
				var floor = door.End.Y - 1;
				for (var j = 0; j < 4; j++)
				{
					var h = 4 + 5 * Mathf.Abs(Mathf.Sin(_t * (4.1f + j * 0.8f) + j * 1.7f));
					var x = door.Position.X + 3 + j * door.Size.X / 4.2f;
					var flame = new[] { new Vector2(x, floor), new Vector2(x + 1.5f, floor - h), new Vector2(x + 3.5f, floor) };
					DrawColoredPolygon(flame, paper);
					DrawPolyline(flame, ink, 0.8f);
				}
			}
			DrawString(Print.FellIt, at + new Vector2(0, H + 16), $"No. {i + 2}. {(i < _lit ? "On the fire." : "Cold.")}", HorizontalAlignment.Center, W, LargePrint.Size(12), ink);
		}
	}
}

/// <summary>
/// What the shop has grown into, printed behind the counter: a shelf of the remedy's bottles that
/// fills as the regulars do (a second shelf once the house has room for more than its first
/// twenty-five), and a card on the wall for each venture taken up. Add it to the counter strip
/// behind the figures (after the background, before the cuts); it takes the strip's size and
/// never the mouse.
/// </summary>
public partial class CounterDressing : Control
{
	private readonly int _regulars;
	private readonly int _capacity;
	private readonly List<string> _cards;
	/// <summary>Room kept clear at the right for AT THE COUNTER and its line.</summary>
	public float ClearRight = 310f;

	private static readonly Dictionary<string, string> CardFor = new()
	{
		["shop_next_door"] = "THE SHOP NEXT DOOR",
		["branch_shop"] = "THE BRANCH",
		["almanac"] = "THE ALMANAC",
		["druggists_order"] = "SOLD BY DRUGGISTS",
		["wholesale_contract"] = "WHOLESALE",
		["laboratory"] = "THE LABORATORY",
		["larger_copper"] = "THE LARGER COPPER",
		["fourth_still"] = "FOUR STILLS",
	};

	public CounterDressing(DeskModel m)
	{
		_regulars = m.Customers;
		_capacity = m.CustomerCapacity;
		_cards = m.Investments.Where(i => i.Held && CardFor.ContainsKey(i.Id)).Select(i => CardFor[i.Id]).ToList();
		MouseFilter = MouseFilterEnum.Ignore;
		SetAnchorsPreset(LayoutPreset.FullRect);
	}

	public CounterDressing() : this(new DeskModel()) { }

	/// <summary>Bottles on the shelves: two to begin with, one more for every three regulars.</summary>
	public int Bottles => 2 + _regulars / 3;

	public override void _Draw()
	{
		var ink = Print.Ink;
		var right = Size.X - ClearRight;
		if (right < 120) return;
		// shelves in faint ink, behind the people: one, and another once the house has grown
		var shelves = _capacity > 25 ? new[] { 104f, 150f } : new[] { 104f };
		var bottle = Print.Cut("wc_bottle_panacea");
		var faint = new Color(1, 1, 1, 0.5f);
		var left = 18f;
		var pitch = 19f;
		var perShelf = (int)((right - left - 10) / pitch);
		var placed = 0;
		foreach (var y in shelves)
		{
			DrawLine(new Vector2(left - 6, y), new Vector2(right, y), new Color(ink, 0.42f), 3f);
			DrawLine(new Vector2(left - 6, y + 4), new Vector2(right, y + 4), new Color(ink, 0.3f), 1f);
			for (var bx = left + 30; bx < right - 10; bx += 150)
				DrawLine(new Vector2(bx, y + 4), new Vector2(bx + 10, y + 16), new Color(ink, 0.3f), 2f);
			if (bottle == null) continue;
			var h = 34f;
			var w = h * bottle.GetWidth() / bottle.GetHeight();
			for (var i = 0; i < perShelf && placed < Bottles; i++, placed++)
				DrawTextureRect(bottle, new Rect2(left + i * pitch, y - h, w, h), false, faint);
		}
		// the ventures, as cards pinned along the top of the wall
		// (a second row, if the first is full, hangs lower, half behind the tallest hats)
		var x = left;
		var top = 8f;
		foreach (var card in _cards)
		{
			var size = 10;
			var tw = Print.Rye.GetStringSize(card, HorizontalAlignment.Left, -1, size).X;
			var r = new Rect2(x, top, tw + 14, 18);
			if (r.End.X > right)
			{
				if (top > 8f) break;
				top = 30f;
				x = left + 40;
				r = new Rect2(x, top, tw + 14, 18);
			}
			DrawRect(r, Print.Paper);
			DrawRect(r, new Color(ink, 0.7f), false, 1.2f);
			DrawString(Print.Rye, new Vector2(r.Position.X + 7, r.End.Y - 5), card, HorizontalAlignment.Left, -1, size, new Color(ink, 0.75f));
			x = r.End.X + 10;
		}
	}
}
