using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Panacea.Game;
using Panacea.Paper;

namespace Panacea.UI;

/// <summary>
/// The formula book as a bench: the drug shelf on the left (click a jar to pour half an ounce,
/// right-click to take it back), the measuring glass in the middle, and the assay on the right,
/// whose gauges slide to the mixture's properties and say plainly what they mean. The recipe can
/// be entered as a new remedy, or poured quietly into an existing one: the label stays the same.
/// </summary>
public partial class FormulaBench : HBoxContainer
{
	private readonly IGame _game;
	private readonly MeasuringGlass _glass = new();
	private readonly Dictionary<string, double> _recipe = new();
	private readonly List<string> _order = new();
	private string _process = "simple_mix";
	private string? _editing;
	private string _name = "Dr. ——'s Celebrated Panacea";
	private long _price = 60;
	private readonly Dictionary<string, Gauge> _gauges = new();
	private Label _cost = null!;
	private RichTextLabel _verdict = null!;
	private VBoxContainer _actions = null!;
	private GridContainer _shelf = null!;
	private string? _note;
	public event Action? Changed;

	/// <summary>The remedy the book was last opened at, kept between openings.</summary>
	private static string? _lastOpened;

	/// <summary>Opens the book at a remedy: its recipe in the glass, its name and price on the page.</summary>
	private void Load(FormulaRow f)
	{
		_editing = f.Id;
		_lastOpened = f.Id;
		_name = f.Name;
		_price = f.PriceCents;
		_process = f.ProcessId;
		_recipe.Clear(); _order.Clear();
		foreach (var (id, oz) in f.Components) { _recipe[id] = oz; _order.Add(id); }
	}

	public FormulaBench(IGame game)
	{
		_game = game;
		AddThemeConstantOverride("separation", 10);
		var d = game.Desk();
		var f = d.Formulas.FirstOrDefault(x => x.Id == _lastOpened) ?? d.Formulas.FirstOrDefault();
		if (f != null) Load(f);
		else
		{
			// a plain starting mixture with room left in the bottle for whatever you add
			foreach (var (id, oz) in new[] { ("water", 3.0), ("grain_alcohol", 2.0), ("sugar_syrup", 1.0), ("peppermint_oil", 0.5) })
			{ _recipe[id] = oz; _order.Add(id); }
		}
		_glass.Capacity = d.BottleCapacityOunces;

		AddChild(Print.Slip(ShelfColumn(d), 8, 282));
		var mid = new VBoxContainer();
		mid.AddChild(Print.Label("THE GLASS", Print.Rye, 18, HorizontalAlignment.Center));
		mid.AddChild(_glass);
		AddChild(Print.Slip(mid, 6, 0));
		var assay = AssayColumn(d);
		assay.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		AddChild(Print.Slip(assay, 10));
		Refresh(animateFrom: true);
	}

	// ---------- the shelf ----------

	private Control ShelfColumn(DeskModel d)
	{
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 6);
		v.AddChild(Print.Label("THE DRUG SHELF", Print.Rye, 18, HorizontalAlignment.Center));
		v.AddChild(Print.Body("[i]Click a jar to pour half an ounce (the dear drugs by the eighth); hold Shift for half as much. Right-click to take it back. By keyboard: the arrows walk the shelf, Enter pours, Backspace takes it back.[/i]", 13, centre: true));
		var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsVertical = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 380) };
		Print.QuietScrollbar(scroll);
		_shelf = new GridContainer { Columns = 3, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		_shelf.AddThemeConstantOverride("h_separation", 6);
		_shelf.AddThemeConstantOverride("v_separation", 6);
		scroll.AddChild(_shelf);
		v.AddChild(scroll);
		BuildShelf(d);
		return v;
	}

	private void BuildShelf(DeskModel d)
	{
		foreach (var c in _shelf.GetChildren()) { _shelf.RemoveChild(c); c.QueueFree(); }
		foreach (var i in d.Ingredients)
		{
			var jar = Jar(i);
			_shelf.AddChild(jar);
			if (i.Id == _focusJar) Callable.From(() => { if (IsInstanceValid(jar)) jar.GrabFocus(); }).CallDeferred();
		}
	}

	private static string JarCut(IngredientRow i) => i.DrugClass switch
	{
		"opiate" => "wc_bottle_syrup",
		"alcohol" => "wc_bottle_bitters",
		"cocaine" => "wc_bottle_panacea",
		_ => i.Id is "calomel" or "acetanilide" or "aspirin" ? "wc_pills" : "wc_bottle_panacea",
	};

	private Control Jar(IngredientRow i)
	{
		var inGlass = _recipe.GetValueOrDefault(i.Id);
		var box = new PanelContainer { CustomMinimumSize = new Vector2(84, 108), MouseFilter = MouseFilterEnum.Stop, MouseDefaultCursorShape = CursorShape.PointingHand, FocusMode = FocusModeEnum.All };
		var on = inGlass > 0;
		var plain = Print.Box(on ? new Color(Print.Ink, 0.08f) : new Color(0, 0, 0, 0), Print.Ink, on ? 2 : 1, 4);
		box.AddThemeStyleboxOverride("panel", plain);
		// the keyboard's jar is printed in reverse at the foot and ruled heavily, not one pixel more
		var ring = Print.Box(on ? new Color(Print.Ink, 0.08f) : new Color(0, 0, 0, 0), Print.Ink, 4, 4);
		ring.ExpandMarginLeft = ring.ExpandMarginRight = ring.ExpandMarginTop = ring.ExpandMarginBottom = 3;
		box.FocusEntered += () => box.AddThemeStyleboxOverride("panel", ring);
		box.FocusExited += () => box.AddThemeStyleboxOverride("panel", plain);
		box.TooltipText = $"{i.Name}: {EditionWriter.Money(i.CostPerOunceCents)} the ounce, {i.OnHand:0.#} oz on hand.\n{i.Description}";
		var v = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		v.AddThemeConstantOverride("separation", 0);
		var tex = Print.Cut(JarCut(i));
		if (tex != null)
		{
			var img = new TextureRect { Texture = tex, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, CustomMinimumSize = new Vector2(0, 54), MouseFilter = MouseFilterEnum.Ignore };
			// the shelf is not the product: jars are printed in plain ink; green only once poured
			if (!on) img.Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/ink_only.gdshader") };
			v.AddChild(img);
		}
		var name = Print.Label(i.Name, Print.Fell, 13, HorizontalAlignment.Center);
		name.MouseFilter = MouseFilterEnum.Ignore;
		v.AddChild(name);
		var sub = Print.Label(on ? $"{inGlass:0.###} oz in the glass" : (i.CostPerOunceCents == 0 ? "free" : $"{EditionWriter.Money(i.CostPerOunceCents)}/oz" + (i.CostPerOunceCents >= 200 ? " · by the ⅛" : "")), Print.FellIt, 11, HorizontalAlignment.Center);
		sub.MouseFilter = MouseFilterEnum.Ignore;
		v.AddChild(sub);
		box.AddChild(v);
		Anim.LiftOnHover(box, 1.05f);
		box.MouseEntered += () => Sfx.Play("tick", -14f, 0.15f);
		// a dear, potent drug is measured by the eighth: a half ounce of morphine would empty the ward
		var unit = i.CostPerOunceCents >= 200 ? 0.125 : 0.5;
		var unitText = unit < 0.5 ? "⅛ oz" : "½ oz";
		var fineText = unit < 0.5 ? "1/16 oz" : "¼ oz";
		box.TooltipText += $"\nClick or Enter: pour {unitText} (Shift: {fineText}). Right-click or Backspace: take it back.";
		box.FocusEntered += () => box.AddThemeStyleboxOverride("panel", Print.Box(new Color(Print.Ink, 0.08f), Print.Ink, 3, 4));
		box.FocusExited += () => box.AddThemeStyleboxOverride("panel", Print.Box(on ? new Color(Print.Ink, 0.08f) : new Color(0, 0, 0, 0), Print.Ink, on ? 2 : 1, 4));
		var id = i.Id;
		box.GuiInput += e =>
		{
			if (e is InputEventMouseButton { Pressed: true } mb)
			{
				// shift for the fine measure: half the jar's usual pour
				var step = mb.ShiftPressed ? unit / 2 : unit;
				if (mb.ButtonIndex == MouseButton.Left) Add(id, step);
				else if (mb.ButtonIndex == MouseButton.Right) Add(id, -step);
			}
			else if (e is InputEventKey { Pressed: true } k)
			{
				if (k.Keycode == Key.Backspace || (k.Keycode == Key.Enter && k.ShiftPressed)) { Add(id, -unit); box.AcceptEvent(); }
				else if (k.Keycode is Key.Enter or Key.KpEnter or Key.Space) { Add(id, unit); box.AcceptEvent(); }
			}
		};
		return box;
	}

	/// <summary>Dev: pours the given ingredients one by one, for recordings.</summary>
	public void Demo(IReadOnlyList<string> ids, float every = 0.55f)
	{
		var t = CreateTween();
		t.TweenInterval(0.8f);
		foreach (var id in ids)
		{
			var i = id;
			t.TweenCallback(Callable.From(() => { if (i.StartsWith('-')) Add(i[1..], -0.5); else Add(i, 0.5); }));
			t.TweenInterval(every);
		}
	}

	private string? _focusJar;

	private void Add(string id, double oz)
	{
		_focusJar = GetViewport()?.GuiGetFocusOwner() is Control fc && _shelf.IsAncestorOf(fc) ? id : null;
		var now = Math.Max(0, _recipe.GetValueOrDefault(id) + oz);
		if (now == _recipe.GetValueOrDefault(id)) return;
		_recipe[id] = now;
		if (!_order.Contains(id)) _order.Add(id);
		if (oz > 0) { _glass.Pour(); Sfx.Play("pour", -2f, 0.1f); }
		else Sfx.Play("slip", -8f, 0.05f, 1.2f);
		_note = null;
		Refresh();
	}

	// ---------- the assay ----------

	private static readonly (string Key, string Label)[] Order =
	{
		("kick", "Kick"), ("calm", "Calm"), ("habit", "Habit"), ("acuteToxicity", "Danger"),
		("bitterness", "Taste"), ("accumulation", "Lingers"), ("detectability", "In an assay"),
	};

	private Control AssayColumn(DeskModel d)
	{
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 6);
		v.AddChild(Print.Label("THE ASSAY", Print.Rye, 18, HorizontalAlignment.Center));
		v.AddChild(Print.Rule(2.5f, 1, 2));
		foreach (var (key, label) in Order)
		{
			var g = new Gauge(label);
			_gauges[key] = g;
			v.AddChild(g);
		}
		_cost = Print.Label("", Print.FellIt, 14);
		v.AddChild(_cost);
		_verdict = Print.Body("", 14, justify: false);
		v.AddChild(_verdict);
		v.AddChild(Print.Rule(1.4f, 0));
		_procs = new HFlowContainer();
		_procs.AddThemeConstantOverride("separation", 4);
		v.AddChild(_procs);
		_actions = new VBoxContainer();
		_actions.AddThemeConstantOverride("separation", 5);
		v.AddChild(_actions);
		return v;
	}

	private HFlowContainer _procs = null!;

	private void BuildProcs(DeskModel d)
	{
		foreach (var c in _procs.GetChildren()) { _procs.RemoveChild(c); c.QueueFree(); }
		foreach (var (id, name) in d.Processes)
		{
			var b = Print.Button(name.ToUpperInvariant(), 11, solid: id == _process);
			var pid = id;
			b.Pressed += () => { _process = pid; Refresh(); };
			_procs.AddChild(b);
		}
	}

	private List<(string, double)> Recipe() => _order.Where(id => _recipe.GetValueOrDefault(id) > 0).Select(id => (id, _recipe[id])).ToList();

	/// <summary>The price button that had the keyboard before the book was redrawn (0: none).</summary>
	private int _focusPrice;

	private void Refresh(bool animateFrom = false)
	{
		var d = _game.Desk();
		var names = d.Ingredients.ToDictionary(i => i.Id, i => i);
		_glass.Set(Recipe().Select(r => (names.TryGetValue(r.Item1, out var n) ? n.Name : r.Item1, r.Item2, names.TryGetValue(r.Item1, out var m) ? m.Opacity : 0.5)).ToList());
		BuildShelf(d);
		BuildProcs(d);

		var p = _game.Preview(Recipe(), _process);
		// an impossible mixture can't be assayed: the gauges keep the last reading, faded
		foreach (var (key, _) in Order)
		{
			var g = _gauges[key];
			g.Modulate = new Color(1, 1, 1, p.Error == null ? 1f : 0.35f);
			if (p.Error != null) continue;
			var value = p.Properties.GetValueOrDefault(key);
			g.Set(value, Words(key, value), animateFrom);
		}
		_cost.Text = p.Error == null ? $"Costs {EditionWriter.Money(p.UnitCostCents)} a bottle to make." : "";
		_verdict.Text = p.Error != null ? $"[b]Not yet.[/b] [i]{Print.Escape(Plain(p.Error))}[/i]" : Verdict(p.Properties);

		foreach (var c in _actions.GetChildren()) { _actions.RemoveChild(c); c.QueueFree(); }
		// one page in the book for each remedy, and a blank page for a new one
		var deskNow = _game.Desk();
		if (deskNow.Formulas.Count > 1)
		{
			var tabs = new HFlowContainer();
			tabs.AddThemeConstantOverride("h_separation", 4);
			tabs.AddThemeConstantOverride("v_separation", 4);
			foreach (var g in deskNow.Formulas)
			{
				var t = Print.Button(g.Name.ToUpperInvariant(), 10, solid: g.Id == _editing);
				t.TooltipText = $"Open the book at {g.Name}: its recipe, price and label.";
				var row = g;
				t.Pressed += () => { Load(row); _note = null; Refresh(); };
				tabs.AddChild(t);
			}
			_actions.AddChild(tabs);
		}
		var nameEdit = new LineEdit { Text = _name, SelectAllOnFocus = true, PlaceholderText = "Name your remedy", TooltipText = "The name on the label and in the Gazette. Choose with care: it cannot be changed once entered." };
		nameEdit.AddThemeFontOverride("font", Print.Fell);
		nameEdit.AddThemeFontSizeOverride("font_size", 15);
		nameEdit.AddThemeColorOverride("font_color", Print.Ink);
		nameEdit.AddThemeStyleboxOverride("normal", Print.Box(Print.Paper, Print.Ink, 1, 5));
		// a field being typed in is ruled heavily, with a caret that shows
		var typing = Print.Box(Print.Paper, Print.Ink, 4, 5);
		typing.ExpandMarginLeft = typing.ExpandMarginRight = typing.ExpandMarginTop = typing.ExpandMarginBottom = 2;
		nameEdit.AddThemeStyleboxOverride("focus", typing);
		nameEdit.AddThemeColorOverride("caret_color", Print.Ink);
		nameEdit.AddThemeConstantOverride("caret_width", 2);
		nameEdit.CaretBlink = true;
		nameEdit.TextChanged += t => _name = t;
		var editing = d.Formulas.FirstOrDefault(f => f.Id == _editing);
		if (editing == null) _actions.AddChild(nameEdit);
		var priceRow = new HBoxContainer();
		priceRow.AddThemeConstantOverride("separation", 4);
		var shown = editing?.PriceCents ?? _price;
		// say whose price the buttons change: the remedy on the shelf, or the one being entered
		var priceLabel = Print.Label(editing != null ? $"{editing.Name}, on the shelf at {EditionWriter.Money(shown)}" : $"The new remedy, to sell at {EditionWriter.Money(shown)}", Print.Fell, 15);
		priceLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		priceRow.AddChild(priceLabel);
		foreach (var step in new[] { -5, 5 })
		{
			var b = Print.Button(step > 0 ? $"+{step}c" : $"{step}c", 11);
			b.TooltipText = editing != null ? $"Changes the price of {editing.Name} on the shelf, from this month." : "Changes the price the new remedy will be entered at.";
			var s = step;
			b.Pressed += () =>
			{
				if (editing != null) _note = _game.SetPrice(editing.Id, Math.Max(1, editing.PriceCents + s));
				else _price = Math.Max(1, _price + s);
				// the keyboard stays on the button, so the next press steps the price again
				_focusPrice = s;
				Refresh();
				Changed?.Invoke();
			};
			priceRow.AddChild(b);
			if (_focusPrice == step)
			{
				_focusPrice = 0;
				Callable.From(() => { if (IsInstanceValid(b) && b.IsInsideTree()) b.GrabFocus(); }).CallDeferred();
			}
		}
		_actions.AddChild(priceRow);
		if (editing != null)
		{
			var re = Print.Button($"CHANGE THE RECIPE OF {editing.Name.ToUpperInvariant()}", 13, solid: true, sound: "lever");
			re.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			re.TooltipText = "Change what goes in the bottle. The label stays the same; stock already brewed is unaffected.";
			re.Disabled = p.Error != null;
			re.Pressed += () => { _note = _game.Reformulate(editing.Id, Recipe(), _process) ?? "Reformulated. The next batch will be the new recipe; the customers will not be told."; Refresh(); Changed?.Invoke(); };
			_actions.AddChild(re);
			var fresh = Print.Button("ENTER IT AS A NEW REMEDY", 12);
			fresh.TooltipText = "Enters this recipe as a second remedy, with its own name, price and label. The one on the shelf is left as it is.";
			fresh.Pressed += () => { _editing = null; _name = "A New Remedy"; _price = editing.PriceCents; Refresh(); };
			_actions.AddChild(fresh);
		}
		else
		{
			var create = Print.Button("ENTER IT IN THE BOOK", 15, solid: true, sound: "lever");
			create.Disabled = p.Error != null;
			create.Pressed += () =>
			{
				var err = _game.CreateFormula(_name, Recipe(), _process, _price);
				_note = err ?? $"“{_name}” is entered in the book.";
				if (err == null) _editing = _game.Desk().Formulas.LastOrDefault()?.Id;
				Refresh();
				Changed?.Invoke();
			};
			_actions.AddChild(create);
		}
		var empty = Print.Button("EMPTY THE GLASS", 11, sound: "slip");
		empty.Pressed += () => { _recipe.Clear(); _order.Clear(); Refresh(); };
		_actions.AddChild(empty);
		if (_note != null) _actions.AddChild(Print.Body($"[i]{Print.Escape(_note)}[/i]", 13, justify: false));
	}

	private static string Short(string name) => (name.Length > 22 ? name[..20].TrimEnd() + "…" : name).ToUpperInvariant();

	/// <summary>What each reading means, in the words a druggist would use.</summary>
	public static string Words(string key, double v) => key switch
	{
		"kick" => v < 2 ? "does nothing" : v < 5 ? "you feel it" : v < 9 ? "a warm wallop" : "knocks them flat",
		"calm" => v < 2 ? "no comfort" : v < 6 ? "soothing" : v < 11 ? "they sleep" : "they sleep very soundly",
		"habit" => v < 1 ? "none" : v < 3 ? "they may come back" : v < 6 ? "most will come back" : "hard to give up",
		// danger climbs in steps a reader can feel, so a doubled dose never reads the same
		"acuteToxicity" => v < 0.8 ? "harmless" : v < 1.6 ? "a risk to infants" : v < 2.8 ? "a risk to the frail"
			: v < 4.5 ? "dangerous to anyone weak" : v < 7 ? "fatal in a large dose" : v < 11 ? "fatal in an ordinary dose" : "a spoonful may kill",
		"bitterness" => v < 1 ? "sweet" : v < 3 ? "medicinal" : v < 6 ? "bitter" : "undrinkable",
		"accumulation" => v < 0.3 ? "passes through" : v < 1.5 ? "builds up slowly" : "builds up in the body",
		"detectability" => v < 2 ? "hard to find" : v < 5 ? "a chemist might find it" : "any chemist would find it",
		_ => "",
	};

	private static string Verdict(Dictionary<string, double> p)
	{
		var habit = p.GetValueOrDefault("habit");
		var danger = p.GetValueOrDefault("acuteToxicity");
		var kick = p.GetValueOrDefault("kick");
		if (danger >= 4) return "[i]It will sell. Some of the customers may not be back to buy it again.[/i]";
		if (habit >= 6) return "[i]Most who start on this bottle will find it hard to stop, whether they like it or not.[/i]";
		if (kick < 2) return "[i]Perfectly safe and perfectly useless. The ward will say so.[/i]";
		if (habit >= 3) return "[i]A good, steady seller. The regulars will grow fond of it.[/i]";
		return "[i]An honest mixture, by the standards of the trade.[/i]";
	}

	/// <summary>An engraved gauge: a bar that slides to its reading, and the reading in words.</summary>
	private sealed partial class Gauge : VBoxContainer
	{
		private readonly ColorRect _bar = new() { Color = Print.Ink, CustomMinimumSize = new Vector2(2, 9) };
		private readonly Label _words;
		private readonly Label _value;
		private Tween? _t;
		private const float Track = 190f;

		public Gauge(string label)
		{
			AddThemeConstantOverride("separation", 0);
			var top = new HBoxContainer();
			top.AddChild(Print.Label(label, Print.Rye, 13, wrap: false));
			_words = Print.Label("", Print.FellIt, 13, HorizontalAlignment.Right, wrap: false);
			_words.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			top.AddChild(_words);
			AddChild(top);
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 6);
			var track = new Control { CustomMinimumSize = new Vector2(Track, 9) };
			track.AddChild(new ColorRect { Color = new Color(Print.Ink, 0.12f), Size = new Vector2(Track, 9) });
			var frame = new Panel { Size = new Vector2(Track, 9), MouseFilter = MouseFilterEnum.Ignore };
			frame.AddThemeStyleboxOverride("panel", Print.Box(new Color(0, 0, 0, 0), new Color(Print.Ink, 0.7f), 1, 0));
			track.AddChild(frame);
			track.AddChild(_bar);
			row.AddChild(track);
			_value = Print.Label("", Print.Fell, 12, wrap: false);
			row.AddChild(_value);
			AddChild(row);
		}

		public void Set(double value, string words, bool fromZero)
		{
			var target = (float)Math.Clamp(value / 15.0, 0, 1) * Track;
			if (fromZero) _bar.Size = new Vector2(2, 9);
			_t?.Kill();
			_t = CreateTween();
			_t.TweenProperty(_bar, "size:x", Math.Max(2, target), 0.35f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
			if (_words.Text != words && !fromZero) Anim.Punch(_words, 1.12f);
			_words.Text = words;
			// the figure is the chemist's pencil note, not print: on hover only
			_value.Text = "";
			TooltipText = $"The chemist's figure: {value:0.0} on a scale of 15.";
			MouseFilter = MouseFilterEnum.Pass;
		}
	}

	/// <summary>The sim's refusals, in the bench's own words.</summary>
	private static string Plain(string err)
	{
		var m = System.Text.RegularExpressions.Regex.Match(err, @"volume ([\d.]+) oz exceeds bottle capacity ([\d.]+)");
		if (m.Success) return $"That is {m.Groups[1].Value} oz, and the bottle holds {m.Groups[2].Value}. It is running over the lip: take some back.";
		return err.Length > 0 ? char.ToUpperInvariant(err[0]) + err[1..] : err;
	}
}
