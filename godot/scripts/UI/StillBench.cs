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

		var max = Math.Max(0, f.MaxBrewable);
		var n = Math.Clamp(_counts.GetValueOrDefault(f.Id, Math.Min(24, max)), 0, max);
		_counts[f.Id] = n;

		// the bottling shelf: a mark for every bottle the stock allows; click to choose the batch
		_right.AddChild(Print.Label("THE BATCH", Print.Rye, 14));
		var shelf = new BottleShelf { Count = n, Max = max, Capacity = Math.Max(48, ((max + 11) / 12) * 12) };
		shelf.Picked += k => { _counts[f.Id] = k; Sfx.Play("tick", -6f, 0.1f); Refresh(); };
		_right.AddChild(shelf);
		var bottles = n * d.BottleCostCents;
		_right.AddChild(Print.Label($"{n} bottle{(n == 1 ? "" : "s")} · costs {EditionWriter.Money(n * f.UnitCostCents)} to make (bottles and corks {EditionWriter.Money(bottles)}) · most the stock allows: {max}", Print.Fell, 15));

		// what limits the batch, and the remedy for it
		var stock = d.Ingredients.ToDictionary(i => i.Id, i => i);
		var limits = f.Components.Where(c => c.Ounces > 0).Select(c => (c, can: stock.TryGetValue(c.IngredientId, out var i) ? (int)Math.Floor(i.OnHand / c.Ounces + 1e-9) : 0)).ToList();
		var shortest = limits.OrderBy(x => x.can).FirstOrDefault();
		if (limits.Count > 0 && shortest.can < 48)
		{
			var nm = stock.TryGetValue(shortest.c.IngredientId, out var si) ? si.Name : shortest.c.IngredientId;
			_right.AddChild(Print.Body($"[i]The shelf is short of {Print.Escape(nm.ToLowerInvariant())}: {shortest.c.Ounces:0.##} oz a bottle, {(si?.OnHand ?? 0):0.#} oz on hand.[/i]", 14, justify: false));
		}
		const int Batch = 24;
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
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 8);
		if (needs.Count > 0)
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
		row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
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
			return k <= Max ? k : -1;
		}

		public override void _GuiInput(InputEvent e)
		{
			if (e is InputEventMouseMotion m) { var h = At(m.Position); if (h != _hover) { _hover = h; QueueRedraw(); } }
			if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } b && At(b.Position) is var k and > 0) { Picked?.Invoke(k); AcceptEvent(); }
			if (e is InputEventKey { Pressed: true } key)
			{
				var step = key.Keycode switch { Key.Right => 1, Key.Left => -1, Key.Up => -12, Key.Down => 12, _ => 0 };
				if (step != 0) { Picked?.Invoke(Math.Clamp(Count + step, 0, Max)); AcceptEvent(); }
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
				if (filled) DrawRect(body, _hover > 0 && i >= Count ? new Color(Print.Green, 0.45f) : Print.Green);
				var c = possible ? ink : new Color(ink, 0.22f);
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

/// <summary>The copper still, engraved in ink. Steam and fire when lit.</summary>
public partial class StillDrawing : Control
{
	public bool Lit;
	public int Stills = 1;
	private float _t;
	private float _flare;
	private readonly RandomNumberGenerator _rng = new() { Seed = 1885 };

	public StillDrawing()
	{
		CustomMinimumSize = new Vector2(310, 420);
		MouseFilter = MouseFilterEnum.Ignore;
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
		var g = new Color(ink, 0.55f);
		DrawSetTransform(new Vector2(0, -70));
		// the furnace and its grate
		var fx = 30f; var fy = 300f;
		DrawRect(new Rect2(fx, fy, 150, 70), ink);
		// brickwork cut into the furnace
		for (var r = 0; r < 4; r++)
			for (var b = 0; b < 6; b++)
			{
				var bx = fx + 4 + b * 25 + (r % 2) * 12;
				if (bx + 20 > fx + 150) continue;
				DrawRect(new Rect2(bx, fy + 4 + r * 16, 21, 12), new Color(paper, 0.18f), false, 1f);
			}
		DrawRect(new Rect2(fx + 40, fy + 26, 70, 30), Lit ? new Color("2a2422") : paper);
		for (var i = 0; i < 6; i++) DrawLine(new Vector2(fx + 44 + i * 12, fy + 26), new Vector2(fx + 44 + i * 12, fy + 56), ink, 2f);
		if (Lit)
		{
			// flames: ink tongues that never sit still
			for (var i = 0; i < 7; i++)
			{
				var h = 14 + 10 * Mathf.Abs(Mathf.Sin(_t * (5 + i) + i));
				var x = fx + 46 + i * 10;
				DrawColoredPolygon(new[] { new Vector2(x - 5, fy + 56), new Vector2(x, fy + 56 - h), new Vector2(x + 5, fy + 56) }, paper);
				DrawPolyline(new[] { new Vector2(x - 5, fy + 56), new Vector2(x, fy + 56 - h), new Vector2(x + 5, fy + 56) }, ink, 1.2f);
			}
		}
		DrawRect(new Rect2(fx - 6, fy + 70, 162, 8), ink);

		// the pot: a copper belly with gouged shading on the shadow side
		var pc = new Vector2(105, 240);
		DrawCircle(pc, 70, paper);
		// the engraver's shading: lines that thicken into the shadow side of the copper
		for (var y = -66; y < 66; y += 4)
		{
			var half = Mathf.Sqrt(70 * 70 - y * y);
			var start = pc.X - half * 0.2f;
			var mid = pc.X + half * 0.35f;
			DrawLine(new Vector2(start, pc.Y + y), new Vector2(mid, pc.Y + y), new Color(ink, 0.3f), 0.9f);
			DrawLine(new Vector2(mid, pc.Y + y), new Vector2(pc.X + half - 2, pc.Y + y), ink, 1.8f);
		}
		DrawCircle(pc + new Vector2(-30, -28), 9, paper);
		DrawArc(pc, 70, 0, Mathf.Tau, 48, ink, 3f);
		DrawArc(pc, 58, Mathf.Pi * 1.1f, Mathf.Pi * 1.4f, 10, new Color(paper, 0.9f), 3f);
		// rivets
		for (var a = 0; a < 12; a++) DrawCircle(pc + Vector2.FromAngle(Mathf.Pi + a * Mathf.Pi / 11f) * 62, 1.8f, ink);
		// the head and swan neck to the worm tub
		DrawRect(new Rect2(85, 155, 40, 22), paper);
		DrawRect(new Rect2(85, 155, 40, 22), ink, false, 2.6f);
		var neck = new[] { new Vector2(118, 160), new Vector2(160, 120), new Vector2(215, 112), new Vector2(240, 150) };
		DrawPolyline(neck, ink, 12f);
		DrawPolyline(neck, paper, 6f);
		DrawPolyline(neck, new Color(ink, 0.5f), 1f);
		// the worm tub, with the coil showing through the staves
		var tub = new Rect2(220, 150, 70, 120);
		DrawRect(tub, paper);
		for (var x = tub.Position.X + 8; x < tub.End.X; x += 10) DrawLine(new Vector2(x, tub.Position.Y), new Vector2(x, tub.End.Y), g, 1.2f);
		DrawRect(tub, ink, false, 3f);
		DrawRect(new Rect2(tub.Position.X - 4, tub.Position.Y + 20, tub.Size.X + 8, 5), ink);
		DrawRect(new Rect2(tub.Position.X - 4, tub.End.Y - 26, tub.Size.X + 8, 5), ink);
		for (var k = 0; k < 4; k++) DrawArc(new Vector2(255, 175 + k * 24), 22, 0.2f, Mathf.Pi - 0.2f, 10, ink, 2f);
		// the spout and the receiver
		DrawLine(new Vector2(255, 270), new Vector2(255, 300), ink, 5f);
		var rc = new Vector2(255, 345);
		DrawCircle(rc, 28, paper);
		var level = Lit ? 0.35f + 0.15f * Mathf.Sin(_t * 0.3f) : 0.2f;
		DrawRect(new Rect2(rc.X - 25, rc.Y + 25 - 50 * level, 50, 50 * level), Print.Green);
		DrawArc(rc, 28, 0, Mathf.Tau, 32, ink, 3f);
		DrawRect(new Rect2(rc.X - 6, rc.Y - 40, 12, 14), ink);
		if (Lit)
		{
			// the drip from the worm, and steam from the head
			var drip = (_t * 1.4f) % 1f;
			DrawCircle(new Vector2(255, 300 + drip * 18), 2.4f, Print.Green);
			for (var i = 0; i < 4; i++)
			{
				var p = (_t * 0.45f + i * 0.25f) % 1f;
				var c = new Vector2(100 + Mathf.Sin(_t + i * 2f) * 10 - p * 18, 150 - p * 120);
				DrawArc(c, 8 + p * 16, 0, Mathf.Tau, 18, new Color(ink, 0.6f * (1 - p)), 1.6f);
			}
		}
		if (_flare > 0) DrawCircle(new Vector2(fx + 75, fy + 40), 30 * _flare, new Color(paper, 0.35f * _flare));
		var caption = Lit ? "On the fire." : "Cold.";
		DrawSetTransform(Vector2.Zero);
		DrawString(Print.FellIt, new Vector2(30, 380), Stills > 1 ? $"{caption} You have {Stills} stills." : caption, HorizontalAlignment.Left, -1, 15, ink);
	}
}
