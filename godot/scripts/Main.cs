using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Panacea.Game;
using Panacea.Paper;
using Panacea.UI;

namespace Panacea;

/// <summary>
/// The game's root: the edition on the table, and the desk behind it. Reading the paper is the
/// payoff; the desk is where the month's decisions are made; pulling the lever prints the next
/// edition.
///
/// Dev arguments (after `--`): --seed S, --play N (autoplay N months with a scripted proprietor,
/// for screenshots and smoke tests), --screen desk|edition.
/// </summary>
public partial class Main : Control
{
	private IGame _game = null!;
	private EditionWriter _writer = null!;
	private EditionView _edition = null!;
	private ProofView _desk = null!;
	private Button _nav = null!;
	private ColorRect _press = null!;
	private ColorRect _wood = null!;
	private bool _onDesk;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		// tooltips are printed slips too
		var theme = new Theme();
		theme.SetStylebox("panel", "TooltipPanel", Print.Box(Print.Paper, Print.Ink, 2, 7));
		theme.SetFont("font", "TooltipLabel", Print.FellIt);
		theme.SetFontSize("font_size", "TooltipLabel", 15);
		theme.SetColor("font_color", "TooltipLabel", Print.Ink);
		Theme = theme;
		var args = OS.GetCmdlineUserArgs();
		string Arg(string name, string fallback)
		{
			for (var i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
			return fallback;
		}

		var sfx = new Sfx();
		AddChild(sfx);
		Sfx.Loop("room", -16f, 1.5f);
		_game = args.Contains("--fake") ? new FakeGame() : SimGame.Start(Arg("--seed", "gazette-1885"));
		using (var f = FileAccess.Open("res://data/paper.en.json", FileAccess.ModeFlags.Read))
			_writer = new EditionWriter(f.GetAsText());

		AddChild(new ColorRect { Color = Print.Table, AnchorRight = 1, AnchorBottom = 1 });
		_wood = new ColorRect
		{
			Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/desk.gdshader") },
			AnchorRight = 1,
			AnchorBottom = 1,
			Visible = false,
		};
		AddChild(_wood);

		_edition = new EditionView { OffsetBottom = -FooterHeight };
		_edition.ClippingRequested += ShowClipping;
		AddChild(_edition);
		_desk = new ProofView { Visible = false, OffsetBottom = -FooterHeight };
		_desk.PressRequested += GoToPress;
		_desk.Changed += () => UpdateStatus(null);
		AddChild(_desk);

		var layer = new CanvasLayer { Layer = 5 };
		AddChild(layer);
		_press = new ColorRect { MouseFilter = MouseFilterEnum.Ignore, AnchorRight = 1, AnchorBottom = 1, Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/press.gdshader") } };
		layer.AddChild(_press);

		// the footer strip: where you are, and the way to the other side of the table
		var ui = new CanvasLayer { Layer = 10 };
		AddChild(ui);
		var footer = new PanelContainer { AnchorTop = 1, AnchorRight = 1, AnchorBottom = 1, OffsetTop = -FooterHeight };
		var bar = Print.Box(Print.Table, Print.Paper, 0, 0);
		bar.BorderWidthTop = 2;
		bar.ContentMarginLeft = bar.ContentMarginRight = 28;
		footer.AddThemeStyleboxOverride("panel", bar);
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 18);
		_status = Print.Label("", Print.FellIt, 17, color: Print.Paper);
		_status.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		row.AddChild(_status);
		_nav = InvertedButton("TO THE PROOF ▸");
		_nav.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		_nav.CustomMinimumSize = new Vector2(230, 0);
		_nav.Pressed += Navigate;
		var sound = InvertedButton("SOUND: ON");
		_sound = sound;
		sound.CustomMinimumSize = new Vector2(118, 0);
		sound.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		sound.AddThemeFontSizeOverride("font_size", 13);
		sound.Pressed += () => { Sfx.SetEnabled(!Sfx.Enabled); sound.Text = Sfx.Enabled ? "SOUND: ON" : "SOUND: OFF"; };
		row.AddChild(sound);
		row.AddChild(_nav);
		footer.AddChild(row);
		ui.AddChild(footer);
		_seed = Arg("--seed", "gazette-1885");

		var play = int.Parse(Arg("--play", "0"));
		Autoplay.AdCents = long.Parse(Arg("--ads", "300"));
		Autoplay.Laudanum = double.Parse(Arg("--dose", "0.25"), System.Globalization.CultureInfo.InvariantCulture);
		for (var i = 0; i < play && !_game.Ended; i++) Autoplay.OneMonth(_game);
		ShowEdition();
		if (Arg("--screen", "edition") is "desk" or "proof") SetDesk(true);
		if (Arg("--drawer", "") is { Length: > 0 } drawer) _desk.OpenDrawer(drawer);
		if (int.TryParse(Arg("--driver", ""), out var port))
		{
			Engine.MaxFps = 12; // reviewers share the machine
			Driver.Start(this, port, DriverState);
		}
		if (Arg("--pour", "") is { Length: > 0 } pour) _desk.DemoPour(pour.Split(','));
		if (Arg("--hover", "") is { Length: > 0 } hover)
		{
			// dev: move a pretend cursor to x,y after the screen settles (for hover screenshots)
			var xy = hover.Split(',');
			var at = new Vector2(float.Parse(xy[0]), float.Parse(xy[1]));
			GetTree().CreateTimer(0.4).Timeout += () =>
			{
				Input.WarpMouse(at);
				Input.ParseInputEvent(new InputEventMouseMotion { Position = at, GlobalPosition = at, Relative = new Vector2(1, 1) });
			};
		}
		if (Arg("--clip", "") is { Length: > 0 } clipKind)
		{
			// dev: open the first clipping of a kind (death, books, inquest, notice) for review
			var ed = _writer.Write(_game.Facts);
			var all = ed.Left.Concat(ed.LeadFoot).Concat(ed.Right).SelectMany(x => x.Blocks).Select(b => b.Clip).Where(c => c != null).ToList();
			var want = clipKind switch { "death" => "IN MEMORIAM", "books" => "THE LEDGER", "inquest" => "THE INQUEST", _ => "YOUR NOTICE" };
			if (all.FirstOrDefault(c => c!.Kicker == want) is { } found) ShowClipping(found);
		}
		if (float.TryParse(Arg("--autopress", ""), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var after))
			GetTree().CreateTimer(after).Timeout += GoToPress;
	}

	private const int FooterHeight = 64;
	private Label _status = null!;
	private Button _sound = null!;

	/// <summary>The keyboard starts on each screen's main action.</summary>
	private void FocusMain()
	{
		if (_clipping != null) return;
		if (_onDesk) _desk.FocusLever();
		else _nav.GrabFocus();
	}
	private string _seed = "";
	private int _restarts;

	/// <summary>On the black footer the ticket is printed white-on-black, inking white on hover.</summary>
	private static Button InvertedButton(string text)
	{
		var b = Print.Button(text, 18, sound: null);
		b.AddThemeColorOverride("font_color", Print.Paper);
		b.AddThemeColorOverride("font_hover_color", Print.Ink);
		b.AddThemeColorOverride("font_pressed_color", Print.Ink);
		b.AddThemeColorOverride("font_focus_color", Print.Paper);
		b.AddThemeStyleboxOverride("normal", Print.Box(Print.Table, Print.Paper, 2, 8));
		b.AddThemeStyleboxOverride("hover", Print.Box(Print.Paper, Print.Paper, 2, 8));
		b.AddThemeStyleboxOverride("pressed", Print.Box(Print.Paper, Print.Paper, 3, 8));
		b.AddThemeStyleboxOverride("focus", Print.Box(new Color(0, 0, 0, 0), Print.Paper, 1, 8));
		return b;
	}

	private void Navigate()
	{
		if (_game.Ended)
		{
			_restarts++;
			_game = SimGame.Start($"{_seed}-{_restarts}");
			ShowEdition();
			return;
		}
		SetDesk(!_onDesk);
	}

	// ---------- the press ----------

	private Control? _pressRun;
	private Tween? _pressTween;
	private bool _pressing;

	/// <summary>
	/// The month's climax. The proof is taken away; the presses run (sheets thump down, the
	/// count of copies climbs); then the new edition is printed before the player's eyes, the
	/// headline last and hardest, and the books settle to the new figure. A click skips it.
	/// </summary>
	private void GoToPress()
	{
		if (_game.Ended || _pressing) return;
		_pressing = true;
		_nav.Disabled = true;
		_sound.Disabled = true;
		var cashBefore = _game.Desk().CashCents;
		Sfx.Play("lever", 0f, 0.02f);
		Sfx.StopLoop("death", 0.3f);
		_game.GoToPress();
		var edition = _writer.Write(_game.Facts);

		var run = BuildPressRun(edition, out var sheets, out var copies, out var title);
		_pressRun = run;
		var t = CreateTween();
		_pressTween = t;
		// 1. the proof goes off to the composing room
		t.TweenProperty(_desk, "position:y", -90f, 0.3f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
		t.Parallel().TweenProperty(_desk, "modulate:a", 0f, 0.3f);
		// 2. the presses run
		t.TweenCallback(Callable.From(() => Sfx.Loop("press_loop", -6f, 0.4f)));
		t.TweenProperty(run, "modulate:a", 1f, 0.15f);
		var count = 0;
		var target = 1100 + (int)(_game.Facts.Turn * 37 % 400);
		foreach (var sheet in sheets)
		{
			var land = sheet.Position;
			sheet.Position = land + new Vector2(-900, -140);
			t.TweenProperty(sheet, "position", land, 0.34f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
			t.TweenCallback(Callable.From(() =>
			{
				Anim.Jolt(run, 8f);
				Sfx.Play("sheet", -2f, 0.08f);
				count += target / sheets.Count;
				copies.Text = $"{count:N0} COPIES";
			}));
			t.TweenInterval(0.2f);
		}
		t.TweenCallback(Callable.From(() => copies.Text = $"{target:N0} COPIES"));
		t.TweenInterval(0.25f);
		// 3. the impression
		t.TweenCallback(Callable.From(() =>
		{
			_desk.Position = Vector2.Zero;
			_desk.Modulate = Colors.White;
			_edition.Show(edition);
			SetDesk(false, slide: false);
			Sfx.StopLoop("press_loop", 0.8f);
			Sfx.Play("roller", -3f, 0.02f);
			_edition.PlayReveal(1.6f);
			CountCash(cashBefore, _game.Desk().CashCents, 1.4f);
			// the parish bell, once for each death in the paper (three at most: the ward has only one bell)
			var deaths = System.Math.Min(3, _game.Facts.Deaths.Count);
			for (var k = 0; k < deaths; k++) Sfx.After(2.0f + k * 2.4f, "knell", -4f - k * 2f);
		}));
		t.TweenProperty(run, "modulate:a", 0f, 0.25f);
		t.TweenCallback(Callable.From(EndPress));
	}

	private void EndPress()
	{
		if (_pressRun != null && IsInstanceValid(_pressRun)) _pressRun.QueueFree();
		_pressRun = null;
		_pressing = false;
		_nav.Disabled = false;
		_sound.Disabled = false;
		FocusMain();
	}

	/// <summary>A click during the press run goes straight to the printed paper.</summary>
	public override void _UnhandledInput(InputEvent e)
	{
		// Escape puts down whatever is in your hand: a cutting, then a tool slip
		if (!_pressing && e.IsActionPressed("ui_cancel"))
		{
			if (_clipping != null) { CloseClipping(); GetViewport().SetInputAsHandled(); }
			else if (_onDesk && _desk.CloseDrawer()) GetViewport().SetInputAsHandled();
			return;
		}
		// during the press, any click or key skips to the printed edition (never past it)
		var skip = e is InputEventMouseButton { Pressed: true } || e is InputEventKey { Pressed: true, Echo: false };
		if (!_pressing || !skip) return;
		GetViewport().SetInputAsHandled();
		_pressTween?.Kill();
		_desk.Position = Vector2.Zero;
		_desk.Modulate = Colors.White;
		_edition.Show(_writer.Write(_game.Facts));
		SetDesk(false, slide: false);
		_edition.FinishReveal();
		EndPress();
	}

	private Control BuildPressRun(Edition e, out List<Control> sheets, out Label copies, out Label title)
	{
		var run = new Control { Modulate = new Color(1, 1, 1, 0), MouseFilter = MouseFilterEnum.Stop };
		run.SetAnchorsPreset(LayoutPreset.FullRect);
		run.OffsetBottom = -FooterHeight;
		run.AddChild(new ColorRect { Color = Print.Table, AnchorRight = 1, AnchorBottom = 1 });
		title = Print.Label("THE PRESSES ARE RUNNING", Print.Rye, 46, HorizontalAlignment.Center, Print.Paper);
		title.AnchorRight = 1; title.OffsetTop = 70;
		run.AddChild(title);
		var sub = Print.Label(e.DatelineCentre, Print.FellIt, 18, HorizontalAlignment.Center, new Color(Print.Paper, 0.7f));
		sub.AnchorRight = 1; sub.OffsetTop = 130;
		run.AddChild(sub);
		copies = Print.Label("0 COPIES", Print.Rye, 30, HorizontalAlignment.Center, Print.Paper);
		copies.AnchorRight = 1; copies.OffsetTop = 160;
		var skip = Print.Label("click, or press any key, to see the edition", Print.FellIt, 15, HorizontalAlignment.Center, new Color(Print.Paper, 0.75f));
		skip.AnchorTop = skip.AnchorBottom = 1; skip.AnchorRight = 1; skip.OffsetTop = -40; skip.OffsetBottom = -14;
		run.AddChild(skip);
		sheets = new List<Control>();
		var rng = new RandomNumberGenerator { Seed = (ulong)(e.DatelineCentre.GetHashCode() & 0x7fffffff) };
		for (var i = 0; i < 5; i++)
		{
			var sheet = new PanelContainer { Size = new Vector2(520, 330) };
			var st = Print.Box(Print.Paper, Print.Ink, 0, 18);
			st.ShadowColor = new Color(0, 0, 0, 0.6f); st.ShadowSize = 10;
			sheet.AddThemeStyleboxOverride("panel", st);
			var v = new VBoxContainer();
			v.AddThemeConstantOverride("separation", 4);
			v.AddChild(Print.Label(e.Masthead, Print.Rye, 30, HorizontalAlignment.Center));
			v.AddChild(Print.Rule(3, 1, 2));
			v.AddChild(Print.Label(e.Lead.Headline, Print.Rye, 34, HorizontalAlignment.Center));
			for (var k = 0; k < 6; k++) v.AddChild(new ColorRect { Color = new Color(Print.Ink, 0.16f), CustomMinimumSize = new Vector2(0, 7) });
			sheet.AddChild(v);
			sheet.Position = new Vector2(380 + rng.RandfRange(-30, 30), 205 + rng.RandfRange(-14, 14) - i * 3);
			sheet.PivotOffset = new Vector2(260, 165);
			sheet.RotationDegrees = rng.RandfRange(-5, 5);
			sheet.Position += new Vector2(0, 30);
			run.AddChild(sheet);
			sheets.Add(sheet);
		}
		run.AddChild(copies); // above the pile
		AddChild(run);
		return run;
	}

	/// <summary>What the scripted reviewer may know: the screen and the game as the player sees it.</summary>
	private System.Text.Json.Nodes.JsonObject DriverState()
	{
		var d = _game.Desk();
		var f = _game.Facts;
		var o = new System.Text.Json.Nodes.JsonObject
		{
			["screen"] = _pressing ? "press" : _clipping != null ? "clipping" : _onDesk ? "proof" : "edition",
			["ended"] = _game.Ended,
			["date"] = $"{d.Year}-{d.Month:00}",
			["cashCents"] = d.CashCents,
			["reputation"] = Math.Round(d.Reputation, 1),
			["heat"] = Math.Round(d.Heat, 1),
			["regulars"] = d.Customers,
			["stillsFree"] = d.StillsFree,
			["canAdvertise"] = d.CanAdvertise,
			["adSpendCents"] = d.AdSpendCents,
			["letters"] = d.Letters.Count,
			["formulas"] = new System.Text.Json.Nodes.JsonArray(d.Formulas.Select(x => (System.Text.Json.Nodes.JsonNode)new System.Text.Json.Nodes.JsonObject
			{
				["name"] = x.Name, ["priceCents"] = x.PriceCents, ["unitCostCents"] = x.UnitCostCents, ["stock"] = x.Stock, ["claims"] = string.Join(",", x.ClaimIds),
			}).ToArray()),
			["lastEdition"] = new System.Text.Json.Nodes.JsonObject
			{
				["deaths"] = f.Deaths.Count, ["inquests"] = f.Inquests.Count, ["joined"] = f.Joined.Count, ["left"] = f.Left.Count,
				["bottlesSold"] = f.Sales.Sum(x => x.Bottles),
			},
		};
		return o;
	}

	private Control? _clipping;

	private void CloseClipping()
	{
		if (_clipping == null) return;
		Sfx.Play("slip", -4f, 0.04f, 0.85f);
		_clipping.QueueFree();
		_clipping = null;
		((ShaderMaterial)_press.Material).SetShaderParameter("speck", _onDesk ? 0.0f : 0.16f);
		FocusMain();
	}

	/// <summary>The cutting behind a paper item: laid over the edition; any click puts it down.</summary>
	private void ShowClipping(Clipping c)
	{
		_clipping?.QueueFree();
		Sfx.Play("slip");
		var shade = new ColorRect { Color = new Color(0, 0, 0, 0.6f), MouseFilter = MouseFilterEnum.Stop };
		shade.SetAnchorsPreset(LayoutPreset.FullRect);
		shade.OffsetBottom = -FooterHeight;
		var centre = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		centre.SetAnchorsPreset(LayoutPreset.FullRect);
		shade.AddChild(centre);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 22);
		var tex = Print.Cut(c.Cut);
		if (tex != null)
		{
			var img = new TextureRect { Texture = tex, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, CustomMinimumSize = new Vector2(170, 290) };
			var framed = Print.Panel(img, Print.Box(new Color(0, 0, 0, 0), Print.Ink, 2, 8));
			framed.SizeFlagsVertical = SizeFlags.ShrinkCenter;
			row.AddChild(framed);
			Anim.Breathe(img, 0.01f, 2.8f);
		}
		var text = new VBoxContainer { CustomMinimumSize = new Vector2(400, 0) };
		text.AddThemeConstantOverride("separation", 6);
		text.AddChild(Print.Label(c.Kicker, Print.Fell, 14));
		text.AddChild(Print.Label(c.Title, Print.Rye, 30));
		text.AddChild(Print.Rule(3, 1, 2));
		foreach (var line in c.Lines) text.AddChild(Print.Body(Print.Escape(line), 17, justify: false));
		text.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });
		text.AddChild(Print.Label("Click anywhere, or press Escape, to put it down.", Print.FellIt, 14, color: Print.Faint));
		row.AddChild(text);
		var card = Print.Slip(row, 20);
		card.RotationDegrees = -1.2f;
		centre.AddChild(card);
		shade.GuiInput += e => { if (e is InputEventMouseButton { Pressed: true }) CloseClipping(); };
		AddChild(shade);
		_clipping = shade;
		Anim.Settle(card);
		((ShaderMaterial)_press.Material).SetShaderParameter("speck", 0.0f);
	}

	private void CountCash(long from, long to, float seconds)
	{
		// coins counted onto the pile, then the till: a bell if the month paid, a dull drawer if not
		if (to != from)
		{
			var coins = to > from ? 9 : 4;
			var ct = CreateTween();
			for (var k = 0; k < coins; k++)
			{
				var db = -8f - k * 0.8f;
				ct.TweenCallback(Callable.From(() => Sfx.Play("coin", db, 0.12f)));
				ct.TweenInterval(seconds / coins * (0.6f + 0.08f * k));
			}
			ct.TweenCallback(Callable.From(() => { if (to > from) Sfx.Play("till", -3f); else Sfx.Play("ticket", 0f, 0.02f, 0.55f); }));
		}
		var t = CreateTween();
		_monthDelta = to - from;
		t.TweenMethod(Callable.From<float>(v => UpdateStatus((long)Mathf.Round(v))), (float)from, (float)to, seconds)
			.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
	}

	private void ShowEdition()
	{
		_edition.Show(_writer.Write(_game.Facts));
		SetDesk(false);
	}

	private void SetDesk(bool desk, bool slide = true)
	{
		var was = _onDesk;
		_onDesk = desk && !_game.Ended;
		if (_onDesk) _desk.Bind(_game);
		_desk.Visible = _onDesk;
		_wood.Visible = _onDesk;
		_edition.Visible = !_onDesk;
		if (_onDesk && _desk.DeathPresent) Sfx.Loop("death", -14f, 2.0f);
		else Sfx.StopLoop("death", 0.8f);
		// moving across the table: the next thing slides in from the side it lies on
		if (slide && was != _onDesk)
		{
			Sfx.Play("page", -2f);
			var view = _onDesk ? (Control)_desk : _edition;
			view.Position = new Vector2(_onDesk ? 70 : -70, 0);
			view.Modulate = new Color(1, 1, 1, 0);
			var t = view.CreateTween().SetParallel();
			t.TweenProperty(view, "position:x", 0f, 0.24f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
			t.TweenProperty(view, "modulate:a", 1f, 0.18f);
		}
		_nav.Text = _onDesk ? "◂ LAST EDITION" : (_game.Ended ? "BEGIN AGAIN" : "TO THE PROOF ▸");
		UpdateStatus(null);
		if (!_pressing) Callable.From(FocusMain).CallDeferred();
		var mat = (ShaderMaterial)_press.Material;
		mat.SetShaderParameter("speck", _onDesk ? 0.0f : 0.16f);
		mat.SetShaderParameter("wobble", _onDesk ? 0.25f : 0.55f);
	}

	/// <summary>The footer line; <paramref name="cash"/> overrides the figure while it counts.</summary>
	private long? _monthDelta;

	private void UpdateStatus(long? cash)
	{
		var d = _game.Desk();
		var when = new System.DateTime(d.Year, d.Month, 1).ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
		_status.Text = _game.Ended
			? "The business is closed. The paper has the last word."
			: $"{when} · cash in hand {EditionWriter.Money(cash ?? d.CashCents)}{(_monthDelta is long dm && !_onDesk ? $" ({(dm >= 0 ? "+" : "−")}{EditionWriter.Money(Math.Abs(dm))} this month)" : "")} · {d.Customers} regulars · {d.Letters.Count} letter{(d.Letters.Count == 1 ? "" : "s")} waiting";
	}
}

/// <summary>A plain scripted proprietor, for screenshots and end-to-end smoke tests.</summary>
public static class Autoplay
{
	/// <summary>Ounces of laudanum per bottle in the scripted recipe (--dose).</summary>
	public static double Laudanum = 0.25;
	public static long AdCents = 300;

	private static (string, double)[] Recipe(double capacity) =>
		new (string, double)[] { ("grain_alcohol", 2), ("sugar_syrup", 1), ("peppermint_oil", 0.5), ("laudanum", Laudanum), ("water", capacity - 3.5 - Laudanum) };

	private static void Try(string what, string? error)
	{
		if (error != null) GD.Print($"autoplay: {what} refused: {error}");
	}

	public static void OneMonth(IGame game)
	{
		var d = game.Desk();
		foreach (var l in d.Letters) Try("resolve", game.Resolve(l.InstanceId, l.Choices[0].Id));
		if (d.Formulas.Count == 0)
		{
			var recipe = Recipe(d.BottleCapacityOunces);
			foreach (var (id, oz) in recipe) Try("buy " + id, game.Buy(id, oz * 40));
			Try("create formula", game.CreateFormula("Dr. ——'s Celebrated Panacea", recipe, "simple_mix", 60));
			d = game.Desk();
			if (d.Formulas.Count == 0) { game.GoToPress(); return; }
		}
		var f = d.Formulas[0];
		if (f.ClaimIds.Count == 0)
		{
			var claims = d.Claims.Select(c => c.Id).Where(c => c is "soothes_nerves" or "cures_consumption").ToList();
			Try("claims", game.SetClaims(f.Id, claims));
		}
		if (d.CanAdvertise && d.AdSpendCents != AdCents) Try("ads", game.SetAdSpend(AdCents));
		var recipeNow = Recipe(d.BottleCapacityOunces);
		if (f.MaxBrewable < 30)
			foreach (var (id, oz) in recipeNow)
				Try("restock " + id, game.Buy(id, oz * 30));
		d = game.Desk();
		var max = d.Formulas[0].MaxBrewable;
		if (d.StillsFree > 0 && max > 0) Try("brew", game.Brew(f.Id, max));
		GD.Print($"autoplay: {d.Year}-{d.Month:00} cash {EditionWriter.Money(d.CashCents)}, regulars {d.Customers}, stock {d.Formulas[0].Stock}, brewed {max}");
		game.GoToPress();
	}
}
