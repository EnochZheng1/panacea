using System.Globalization;
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Panacea.Game;
using Panacea.Paper;

namespace Panacea.UI;

/// <summary>
/// The still, as an engraving: copper pot, swan neck, the worm in its tub, the receiver. Cold
/// when idle; lit, with steam rising and drops falling, when a batch is on. The batch is chosen
/// on the shelf beside it: one bottle per mark, click the last bottle you want filled.
/// </summary>
public partial class StillBench : HBoxContainer
{
	private readonly IGame _game;
	private readonly Dictionary<string, int> _counts = new();
	private string? _formula;
	private string? _note;
	private readonly StillDrawing _still = new();
	private readonly VBoxContainer _right = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
	public event Action? Changed;

	public StillBench(IGame game)
	{
		_game = game;
		AddThemeConstantOverride("separation", 14);
		var left = new VBoxContainer();
		left.AddChild(Print.Label("THE STILL", Print.Rye, 22, HorizontalAlignment.Center));
		left.AddChild(_still);
		AddChild(Print.Slip(left, 10, 330));
		_right.AddThemeConstantOverride("separation", 8);
		// the batch card keeps its width whatever it holds, so a short message never collapses it
		_right.CustomMinimumSize = new Vector2(330, 0);
		var card = Print.Slip(_right, 14);
		card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		AddChild(card);
		Refresh();
	}

	public void Refresh()
	{
		var keep = Print.FocusIndex(this);
		RefreshBody();
		Print.RestoreFocus(this, keep);
	}

	private void RefreshBody()
	{
		foreach (var c in _right.GetChildren()) { _right.RemoveChild(c); c.QueueFree(); }
		var d = _game.Desk();
		_still.Lit = d.Brewing.Count > 0;
		_still.Stills = Math.Max(1, d.StillsFree + d.Brewing.Count);

		if (d.Formulas.Count == 0)
		{
			_right.AddChild(Print.Label("NOTHING TO BREW", Print.Rye, 20));
			_right.AddChild(Print.Body("Enter a recipe in the formula book first. The still will wait.", 16, justify: false));
			return;
		}
		var f = d.Formulas.FirstOrDefault(x => x.Id == _formula) ?? d.Formulas[0];
		_formula = f.Id;

		if (d.Formulas.Count > 1)
		{
			var tabs = new HFlowContainer();
			tabs.AddThemeConstantOverride("h_separation", 4);
			foreach (var g in d.Formulas)
			{
				var t = Print.Button(g.Name.ToUpperInvariant(), 12, solid: g.Id == f.Id);
				var id = g.Id;
				t.Pressed += () => { _formula = id; Refresh(); };
				tabs.AddChild(t);
			}
			_right.AddChild(tabs);
		}

		_right.AddChild(Print.Label(f.Name, Print.Rye, 20));
		if (d.StillsFree <= 0)
		{
			// every still is lit: one line, and what comes next
			var lit = string.Join("; ", d.Brewing.Select(x => $"{x.Item2} bottles of {x.Item1}"));
			_right.AddChild(Print.Body($"[i]Every still is on the fire ({Print.Escape(lit)}). The bottles will be on the shelf by press time; send the paper to press when you are ready.[/i]", 15, justify: false));
			return;
		}
		var stills = d.StillsFree == 1 ? "One still is free" : d.StillsFree == 2 ? "Two stills are free" : $"{d.StillsFree} stills are free";
		_right.AddChild(Print.Body($"[i]{stills} this month. A batch is bottled by the time the paper goes to press.[/i]", 14, justify: false));
		foreach (var (name, onFire) in d.Brewing)
			_right.AddChild(Print.Label($"On the fire: {onFire} bottles of {name}", Print.FellIt, 15));

		var max = Math.Max(0, f.MaxBrewable);
		// the batch you want may be more than the shelf allows: the order button makes up the rest
		var want = Math.Clamp(_counts.GetValueOrDefault(f.Id, max > 0 ? Math.Min(24, max) : 24), 0, 96);
		_counts[f.Id] = want;
		var n = Math.Min(want, max);

		// the bottling shelf: a mark for every bottle the stock allows; click to choose the batch
		_right.AddChild(Print.Label("THE BATCH", Print.Rye, 14));
		var shelf = new BottleShelf { Count = want, Max = max, Capacity = Math.Min(96, Math.Max(48, ((Math.Max(max, want) + 11) / 12) * 12)) };
		shelf.Picked += k => { _counts[f.Id] = k; Sfx.Play("tick", -6f, 0.1f); Refresh(); };
		_right.AddChild(shelf);
		var bottles = n * d.BottleCostCents;
		// the ingredients were paid for when they were ordered: lighting the fire costs only the glass
		var stuff = Math.Max(0, n * f.UnitCostCents - bottles);
		_right.AddChild(Print.Label(want > max
			? $"You have marked {want}; the ingredients on hand make {(max == 0 ? "none" : max.ToString(CultureInfo.InvariantCulture))}. {(max == 0 ? "Order the rest to light the fire." : $"Order the rest, or light the fire for {max}.")}"
			: $"{n} bottle{(n == 1 ? "" : "s")} · {EditionWriter.Money(bottles)} for bottles and corks when lit · {(stuff > 0 ? $"takes {EditionWriter.Money(stuff)} of stock off the shelf · " : "")}most the stock allows: {max}", Print.Fell, 15));

		// what limits the batch, and the remedy for it
		var stock = d.Ingredients.ToDictionary(i => i.Id, i => i);
		var limits = f.Components.Where(c => c.Ounces > 0).Select(c => (c, can: stock.TryGetValue(c.IngredientId, out var i) ? (int)Math.Floor(i.OnHand / c.Ounces + 1e-9) : 0)).ToList();
		var shortest = limits.OrderBy(x => x.can).FirstOrDefault();
		if (limits.Count > 0 && shortest.can < want)
		{
			var nm = stock.TryGetValue(shortest.c.IngredientId, out var si) ? si.Name : shortest.c.IngredientId;
			_right.AddChild(Print.Body($"[i]The shelf is short of {Print.Escape(nm.ToLowerInvariant())}: {shortest.c.Ounces:0.##} oz a bottle, {(si?.OnHand ?? 0):0.#} oz on hand.[/i]", 14, justify: false));
		}
		var Batch = want > max ? want : 24;
		var needs = new List<(string Id, double Oz)>();
		long cost = 0;
		foreach (var c in f.Components.Where(c => c.Ounces > 0))
		{
			var have = stock.TryGetValue(c.IngredientId, out var i) ? i.OnHand : 0;
			var need = Math.Ceiling(Math.Max(0, c.Ounces * Batch - have));
			if (need <= 0) continue;
			needs.Add((c.IngredientId, need));
			cost += (long)Math.Round(need * (i?.CostPerOunceCents ?? 0));
		}
		// the two actions wrap rather than push the drawer wider than the proof
		var row = new HFlowContainer();
		row.AddThemeConstantOverride("h_separation", 8);
		row.AddThemeConstantOverride("v_separation", 8);
		if (needs.Count > 0 && d.StillsFree > 0)
		{
			var buy = Print.Button($"ORDER ENOUGH FOR {Batch} · {EditionWriter.Money(cost)}", 13);
			buy.TooltipText = "Orders from Pearl street whatever the recipe is short of, for a batch of " + Batch + ".";
			buy.Pressed += () =>
			{
				string? err = null;
				foreach (var (id, oz) in needs) err ??= _game.Buy(id, oz);
				_note = err ?? $"Ordered: enough for {Batch} bottles, {EditionWriter.Money(cost)}.";
				_counts[f.Id] = Batch;
				Refresh();
				Changed?.Invoke();
			};
			row.AddChild(buy);
		}
		var fire = Print.Button(n > 0 ? $"LIGHT THE FIRE · {n} BOTTLES" : "LIGHT THE FIRE", 17, solid: true, sound: "lever");
		fire.Disabled = n <= 0 || d.StillsFree <= 0;
		fire.TooltipText = d.StillsFree <= 0 ? "Every still is already on the fire this month." : n <= 0 ? (want > 0 ? "Not enough stock on the shelf: order first." : "Choose a batch on the shelf.") : "Brews the batch; it is bottled by press time.";
		var fid = f.Id; var count = n; var fname = f.Name;
		fire.Pressed += () =>
		{
			var err = _game.Brew(fid, count);
			_note = err ?? $"The fire is lit: {count} bottles of {fname} will be ready by press time.";
			if (err == null) { _still.Lit = true; _still.Flare(); Sfx.Play("pour", -2f, 0.05f, 0.6f); }
			// the fire button is gone: give the keyboard somewhere to stand
			Callable.From(() => { if (IsInsideTree()) Print.Focusables(this).FirstOrDefault()?.GrabFocus(); }).CallDeferred();
			Refresh();
			Changed?.Invoke();
		};
		row.AddChild(fire);
		_right.AddChild(row);
		if (_note != null) _right.AddChild(Print.Body($"[i]{Print.Escape(_note)}[/i]", 15, justify: false));
	}

	/// <summary>Bottles in rows of twelve: the chosen ones printed, the possible ones outlined.</summary>
	private sealed partial class BottleShelf : Control
	{
		public int Count, Max, Capacity = 48;
		public event Action<int>? Picked;
		private int _hover = -1;
		private const float W = 20, H = 40, Gap = 6;

		public BottleShelf()
		{
			MouseFilter = MouseFilterEnum.Stop;
			MouseDefaultCursorShape = CursorShape.PointingHand;
			FocusMode = FocusModeEnum.All;
			TooltipText = "Click a bottle to fill the shelf up to it. Arrow keys change the batch by one.";
		}

		public override Vector2 _GetMinimumSize() => new(12 * (W + Gap), ((Capacity + 11) / 12) * (H + 14));

		private int At(Vector2 p)
		{
			var col = (int)(p.X / (W + Gap));
			var row = (int)(p.Y / (H + 14));
			if (col < 0 || col > 11 || row < 0) return -1;
			var k = row * 12 + col + 1;
			return k <= Capacity ? k : -1;
		}

		public override void _GuiInput(InputEvent e)
		{
			if (e is InputEventMouseMotion m) { var h = At(m.Position); if (h != _hover) { _hover = h; QueueRedraw(); } }
			if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } b && At(b.Position) is var k and > 0) { GrabFocus(); Picked?.Invoke(k); AcceptEvent(); }
			if (e is InputEventKey { Pressed: true } key)
			{
				var step = key.Keycode switch { Key.Right => 1, Key.Left => -1, Key.Up => -12, Key.Down => 12, _ => 0 };
				if (step != 0) { Picked?.Invoke(Math.Clamp(Count + step, 0, Capacity)); AcceptEvent(); }
			}
		}

		public override void _Notification(int what)
		{
			if (what == NotificationMouseExit) { _hover = -1; QueueRedraw(); }
		}

		public override void _Draw()
		{
			var ink = Print.Ink;
			var shown = _hover > 0 ? _hover : Count;
			for (var i = 0; i < Capacity; i++)
			{
				var x = (i % 12) * (W + Gap);
				var y = (i / 12) * (H + 14);
				var possible = i < Max;
				var filled = i < shown;
				// a Boston round in outline: body, shoulder, neck, cork
				var body = new Rect2(x, y + 12, W, H - 12);
				// beyond what the stock allows, a wanted bottle is only pencilled in
				if (filled) DrawRect(body, !possible ? new Color(Print.Green, 0.2f) : _hover > 0 && i >= Count ? new Color(Print.Green, 0.45f) : Print.Green);
				var c = possible ? ink : new Color(ink, 0.5f);
				DrawRect(body, c, false, 1.6f);
				DrawLine(new Vector2(x, y + 12), new Vector2(x + W * 0.35f, y + 5), c, 1.6f);
				DrawLine(new Vector2(x + W, y + 12), new Vector2(x + W * 0.65f, y + 5), c, 1.6f);
				DrawRect(new Rect2(x + W * 0.35f, y, W * 0.3f, 5), possible ? ink : c);
				if (filled) DrawRect(new Rect2(x + 3, y + 20, W - 6, 10), Print.Paper);
				if (i % 12 == 11 || i == Capacity - 1) DrawLine(new Vector2(0, y + H + 2), new Vector2(12 * (W + Gap) - Gap, y + H + 2), ink, 3f);
			}
			if (HasFocus()) DrawRect(new Rect2(-4, -4, Size.X + 8, Size.Y + 8), ink, false, 2f);
		}
	}
}

/// <summary>
/// The still, as an engraved cut (art/templates/wc/wc_still.body.svg, drawn at twice these
/// coordinates). The engraving is fixed; what moves is drawn over it: flames in the fire door,
/// steam from the head, the drip from the worm, and the green rising in the receiver.
/// </summary>
public partial class StillDrawing : Control
{
	public bool Lit;
	public int Stills = 1;
	private float _t;
	private float _flare;
	private static Texture2D? _cut;

	// where the engraving leaves room for the game (in this control's coordinates)
	private static readonly Rect2 FireDoor = new(67, 267, 76, 37);
	private static readonly Vector2 Joint = new(250, 104);
	private static readonly Vector2 Spout = new(252.5f, 252);
	private static readonly Vector2 Receiver = new(252.5f, 291);
	private const float ReceiverR = 23f;
	private static readonly Rect2 Label = new(241, 300, 23, 11);

	public StillDrawing()
	{
		CustomMinimumSize = new Vector2(310, 384);
		MouseFilter = MouseFilterEnum.Ignore;
		// the card, not the bare sprite: the cut keeps its ruled ground and frame in the drawer
		_cut ??= ResourceLoader.Exists("res://assets/art/wc_still.card.png") ? GD.Load<Texture2D>("res://assets/art/wc_still.card.png") : null;
	}

	public void Flare() => _flare = 1.2f;

	public override void _Process(double delta)
	{
		_t += (float)delta;
		if (_flare > 0) _flare -= (float)delta;
		if (Lit || _flare > 0) QueueRedraw();
	}

	public override void _Draw()
	{
		var ink = Print.Ink;
		var paper = Print.Paper;
		if (_cut != null)
		{
			// the card is 620 x 880 at 2x; the framed engraving is its top 620 x 720
			var k = _cut.GetWidth() / 620f;
			DrawTextureRectRegion(_cut, new Rect2(0, 0, 310, 363), new Rect2(0, 0, 620 * k, 726 * k));
			// the frame's foot, which the card's filter leaves ragged at this size
			DrawLine(new Vector2(20, 360), new Vector2(290, 360), ink, 2f);
			DrawLine(new Vector2(23.5f, 356.5f), new Vector2(286.5f, 356.5f), ink, 0.8f);
		}

		// the receiver fills with the product, green, from the bottom up
		var level = Lit ? 0.42f + 0.12f * Mathf.Sin(_t * 0.3f) : 0.22f;
		var top = Receiver.Y + ReceiverR - 2 * ReceiverR * level;
		var pts = new List<Vector2>();
		var a0 = Mathf.Asin(Mathf.Clamp((top - Receiver.Y) / ReceiverR, -1, 1));
		for (var i = 0; i <= 24; i++)
		{
			var a = Mathf.Lerp(a0, Mathf.Pi - a0, i / 24f);
			pts.Add(Receiver + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * ReceiverR);
		}
		DrawColoredPolygon(pts.ToArray(), Print.Green);
		// the liquid is printed, not painted: ruled with ink like the bottle's label
		for (var y = top + 2; y < Receiver.Y + ReceiverR - 1; y += 3)
		{
			var hw = Mathf.Sqrt(Mathf.Max(0, ReceiverR * ReceiverR - (y - Receiver.Y) * (y - Receiver.Y))) - 1;
			DrawLine(new Vector2(Receiver.X - hw, y), new Vector2(Receiver.X + hw, y), new Color(ink, 0.28f), 0.8f);
		}
		// the label on the glass sits over the liquid
		DrawRect(Label, paper);
		DrawRect(Label, ink, false, 1f);
		DrawString(Print.Rye, new Vector2(Label.Position.X + 2, Label.End.Y - 2.5f), "DR. ——", HorizontalAlignment.Left, Label.Size.X - 3, 6, ink);

		if (Lit)
		{
			// the glow, cut white into the brick round the fire door
			var door = new Vector2(FireDoor.GetCenter().X, FireDoor.End.Y);
			for (var i = 0; i < 9; i++)
			{
				var a = Mathf.Pi + (i + 0.5f) * Mathf.Pi / 9f;
				var flick = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(_t * 2.3f + i));
				var dir = Vector2.FromAngle(a);
				DrawLine(door + dir * new Vector2(46, 42), door + dir * new Vector2(46 + 9 * flick, 42 + 8 * flick), new Color(paper, 0.85f), 1.6f);
			}
			// flames: uneven paper tongues with curled tips, never still
			var floor = FireDoor.End.Y - 2;
			for (var i = 0; i < 7; i++)
			{
				var h = 10 + 15 * Mathf.Abs(Mathf.Sin(_t * (4.3f + i * 0.7f) + i * 1.9f)) + (i % 3) * 3;
				var w = 4.5f + (i * 7 % 3);
				var x = FireDoor.Position.X + 8 + i * 10.2f;
				var lean = 3f * Mathf.Sin(_t * 2.7f + i * 1.3f);
				var flame = new[]
				{
					new Vector2(x - w, floor),
					new Vector2(x - w * 0.8f + lean * 0.3f, floor - h * 0.45f),
					new Vector2(x + lean, floor - h),
					new Vector2(x + lean + 3.5f, floor - h + 4),
					new Vector2(x + w * 0.7f + lean * 0.4f, floor - h * 0.5f),
					new Vector2(x + w, floor),
				};
				DrawColoredPolygon(flame, paper);
				DrawPolyline(flame, ink, 1.1f);
			}
			// a drop falls from the worm into the receiver's neck
			var drip = (_t * 1.3f) % 1f;
			DrawCircle(Spout + new Vector2(0, 2 + drip * drip * 16), 2f, Print.Green);
			// vapour escaping at the joint of the arm and the tub, in curling inked wisps
			for (var w = 0; w < 3; w++)
			{
				var p = (_t * 0.35f + w / 3f) % 1f;
				var wisp = new Vector2[9];
				for (var k = 0; k < wisp.Length; k++)
				{
					var q = k / 8f;
					wisp[k] = Joint + new Vector2(-6 - 10 * w + Mathf.Sin(q * 5f + _t * 1.5f + w) * (3 + 6 * q), -p * 38 - q * 22);
				}
				DrawPolyline(wisp, new Color(ink, Mathf.Clamp(1.4f * (1 - p), 0, 1)), 1.8f);
			}
		}
		if (_flare > 0) DrawCircle(FireDoor.GetCenter(), 30 * _flare, new Color(paper, 0.35f * _flare));
		var caption = Lit ? "On the fire." : "Cold.";
		DrawString(Print.FellIt, new Vector2(24, 378), Stills > 1 ? $"{caption} Still No. 1 of {Stills}." : caption, HorizontalAlignment.Left, -1, 15, ink);
	}
}
