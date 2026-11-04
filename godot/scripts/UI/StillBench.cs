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
		AddChild(Print.Slip(_right, 14));
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
		var stills = d.StillsFree == 1 ? "One still is free" : d.StillsFree == 0 ? "No still is free" : $"{d.StillsFree} stills are free";
		_right.AddChild(Print.Body($"[i]{stills} this month. A batch is bottled by the time the paper goes to press.[/i]", 14, justify: false));
		foreach (var (name, onFire) in d.Brewing)
			_right.AddChild(Print.Label($"On the fire: {onFire} bottles of {name}", Print.FellIt, 15));

		// every still on the fire: nothing to choose until next month
		if (d.StillsFree <= 0)
		{
			_right.AddChild(Print.Body("[i]Every still is on the fire. The batch will be bottled by press time; come back next month for another.[/i]", 15, justify: false));
			if (_note != null) _right.AddChild(Print.Body($"[i]{Print.Escape(_note)}[/i]", 15, justify: false));
			return;
		}
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
			? $"You have marked {want}; the stock on the shelf makes {(max == 0 ? "none" : max.ToString(CultureInfo.InvariantCulture))}. Order the rest, or light the fire for {(max == 0 ? "nothing" : max.ToString(CultureInfo.InvariantCulture))}."
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
		fire.TooltipText = d.StillsFree <= 0 ? "Every still is already on the fire this month." : n <= 0 ? "Choose a batch on the shelf (and have the stock for it)." : "Brews the batch; it is bottled by press time.";
		var fid = f.Id; var count = n; var fname = f.Name;
		fire.Pressed += () =>
		{
			var err = _game.Brew(fid, count);
			_note = err ?? $"The fire is lit: {count} bottles of {fname} will be ready by press time.";
			if (err == null) { _still.Lit = true; _still.Flare(); Sfx.Play("pour", -2f, 0.05f, 0.6f); }
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
	private static readonly Vector2 Head = new(105, 81);
	private static readonly Vector2 Spout = new(252.5f, 252);
	private static readonly Vector2 Receiver = new(252.5f, 291);
	private const float ReceiverR = 23f;
	private static readonly Rect2 Label = new(241, 300, 23, 11);

	public StillDrawing()
	{
		CustomMinimumSize = new Vector2(310, 372);
		MouseFilter = MouseFilterEnum.Ignore;
		_cut ??= ResourceLoader.Exists("res://assets/art/wc_still.png") ? GD.Load<Texture2D>("res://assets/art/wc_still.png") : null;
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
			// the sprite is the whole 620 x 880 card at 2x; the engraving is its top 620 x 720
			var k = _cut.GetWidth() / 620f;
			DrawTextureRectRegion(_cut, new Rect2(0, 0, 310, 360), new Rect2(0, 0, 620 * k, 720 * k));
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
		// the label on the glass sits over the liquid
		DrawRect(Label, paper);
		DrawRect(Label, ink, false, 1f);
		DrawString(Print.Rye, new Vector2(Label.Position.X + 2, Label.End.Y - 2.5f), "DR. ——", HorizontalAlignment.Left, Label.Size.X - 3, 6, ink);

		if (Lit)
		{
			// flames: paper tongues in the dark of the fire door, never still
			var floor = FireDoor.End.Y - 2;
			for (var i = 0; i < 7; i++)
			{
				var h = 12 + 13 * Mathf.Abs(Mathf.Sin(_t * (5 + i) + i * 1.7f));
				var x = FireDoor.Position.X + 7 + i * 10.5f;
				var lean = 2.5f * Mathf.Sin(_t * 3 + i);
				var flame = new[] { new Vector2(x - 5, floor), new Vector2(x + lean, floor - h), new Vector2(x + 5, floor) };
				DrawColoredPolygon(flame, paper);
				DrawPolyline(flame, ink, 1.2f);
			}
			// the drip from the worm
			var drip = (_t * 1.4f) % 1f;
			DrawCircle(Spout + new Vector2(0, 2 + drip * 12), 2.2f, Print.Green);
			// steam from the head
			for (var i = 0; i < 4; i++)
			{
				var p = (_t * 0.45f + i * 0.25f) % 1f;
				var c = new Vector2(Head.X + Mathf.Sin(_t + i * 2f) * 9 - p * 16, Head.Y - p * 70);
				DrawArc(c, 6 + p * 14, 0, Mathf.Tau, 18, new Color(ink, 0.6f * (1 - p)), 1.6f);
			}
		}
		if (_flare > 0) DrawCircle(FireDoor.GetCenter(), 30 * _flare, new Color(paper, 0.35f * _flare));
		var caption = Lit ? "On the fire." : "Cold.";
		DrawString(Print.FellIt, new Vector2(24, 362), Stills > 1 ? $"{caption} You have {Stills} stills." : caption, HorizontalAlignment.Left, -1, 15, ink);
	}
}
