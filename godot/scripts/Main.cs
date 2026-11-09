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
		// and for the footer and the press, which sit on their own canvas layers
		GetTree().Root.Theme = theme;
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
		_desk.Changed += () => { UpdateStatus(null); if (_onDesk) Callable.From(WireDesk).CallDeferred(); };
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
		// the menu: the title page, the settings, a way out (Escape opens it too)
		var menu = InvertedButton("☰ MENU");
		menu.AddThemeFontSizeOverride("font_size", 13);
		menu.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		menu.FocusMode = FocusModeEnum.Click;
		menu.TooltipText = "The title page, the settings, and a way out. (Escape opens it too.)";
		menu.Pressed += () => { if (!_pressing && _card == null) ShowPause(); };
		row.AddChild(menu);
		_status = Print.Label("", Print.FellIt, 17, color: Print.Paper);
		_status.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		row.AddChild(_status);
		_nav = InvertedButton("TO THE PROOF ☞");
		_nav.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		_nav.CustomMinimumSize = new Vector2(230, 0);
		_nav.Pressed += Navigate;
		var sound = InvertedButton(Band(true));
		_sound = sound;
		_edition.ChainFocus(_sound, _nav);
		sound.CustomMinimumSize = new Vector2(214, 0);
		sound.TooltipText = "Sound is on. Press to silence it.";
		sound.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		sound.AddThemeFontSizeOverride("font_size", 13);
		sound.Pressed += () => { Settings.SetSound(!Sfx.Enabled); SyncSound(); };
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
		// a player's launch opens on the title page; screenshot, driver and test runs go straight in
		_dev = args.Any(a => a is "--shot" or "--play" or "--driver" or "--screen" or "--drawer" or "--clip" or "--fake" or "--pour" or "--hover" or "--autopress" or "--seed")
			// --player: a driven run that takes the player's path (the title page, real saves)
			&& !args.Contains("--player");
		if (Arg("--profile", "") is { Length: > 0 } profile && profile.All(c => char.IsLetterOrDigit(c) || c is '-' or '_'))
		{
			SaveStore.PathName = $"user://panacea-{profile}.save";
			Settings.PathName = $"user://settings-{profile}.cfg";
		}
		if (!_dev)
		{
			Settings.Load();
			SyncSound();
			ShowTitle();
		}
		else if (args.Contains("--title")) ShowTitle();
		else if (args.Contains("--menu")) ShowPause();
		if (Arg("--savetest", "") is { Length: > 0 } months) SaveTest(int.Parse(months));
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

	/// <summary>
	/// On the proof, Tab runs through the bench (and any open drawer) and then out to the footer,
	/// so LAST EDITION and SOUND can be reached without a mouse.
	/// </summary>
	private void WireDesk()
	{
		if (!_onDesk) return;
		var items = Print.Focusables(_desk);
		if (items.Count == 0) return;
		_sound.FocusPrevious = items[^1].GetPath();
		items[^1].FocusNext = _sound.GetPath();
		_nav.FocusNext = items[0].GetPath();
		items[0].FocusPrevious = _nav.GetPath();
		_sound.FocusNext = _nav.GetPath();
	}

	/// <summary>The keyboard starts on each screen's main action.</summary>
	private void FocusMain()
	{
		if (_clipping != null) return;
		if (_onDesk) _desk.FocusStart();
		else _nav.GrabFocus();
	}
	private string _seed = "";
	private int _restarts;

	/// <summary>On the black footer the ticket is printed white-on-black, inking white on hover.</summary>
	/// <summary>The sound toggle: says it is the sound, in the shop's words (the press, the till and the paper heard, or hushed).</summary>
	private static string Band(bool on) => on ? "SOUND: THE SHOP HEARD" : "SOUND: HUSHED";

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
		// focus: a second paper rule inside the first, so the keyboard's place is plain on black
		var ring = Print.Box(new Color(0, 0, 0, 0), Print.Paper, 2, 6);
		ring.ExpandMarginLeft = ring.ExpandMarginRight = ring.ExpandMarginTop = ring.ExpandMarginBottom = -5;
		b.AddThemeStyleboxOverride("focus", ring);
		// locked (during the press): a dim outline on black, never lit
		b.AddThemeStyleboxOverride("disabled", Print.Box(Print.Table, new Color(Print.Paper, 0.3f), 1, 8));
		b.AddThemeColorOverride("font_disabled_color", new Color(Print.Paper, 0.4f));
		return b;
	}

	// ---------- the front door: the title page, the menu, the settings, the save ----------

	private bool _dev;
	private FrontCard? _card;
	private CanvasLayer? _cardLayer;
	private bool _confirmNew;

	private void SyncSound()
	{
		_sound.Text = Band(Sfx.Enabled);
		_sound.TooltipText = Sfx.Enabled ? "Sound is on. Press to silence it." : "Sound is off. Press to have it back.";
	}

	private void ShowCard(FrontCard card)
	{
		CloseCard();
		_cardLayer ??= new CanvasLayer { Layer = 30 };
		if (_cardLayer.GetParent() == null) AddChild(_cardLayer);
		_card = card;
		_cardLayer.AddChild(card);
		Sfx.Play("slip", -4f);
	}

	private void CloseCard()
	{
		if (_card == null) return;
		_card.GetParent()?.RemoveChild(_card);
		_card.QueueFree();
		_card = null;
		Main.Guard(250);
		FocusMain();
	}

	private void ShowTitle()
	{
		var c = new FrontCard(640);
		c.Title("PANACEA", 54);
		c.Line("A business in patent medicines · New York, 1885–1906", 17);
		c.Rule();
		c.Notice("A NOTE BEFORE THE SHOP OPENS",
			"Panacea is a satire of the patent-medicine trade. It shows drug use and addiction, deaths (among them the deaths of infants) and the frauds the trade practised. The joke is on the sellers and the men who let them sell, never on the people they sold to.");
		var label = SaveStore.Label();
		var resumable = label != null && label != "The business is closed.";
		if (resumable) c.Choice("☞  CONTINUE", ContinueSaved, label, solid: true);
		c.Choice(_confirmNew ? "A NEW SHOP? PRESS AGAIN" : "A NEW SHOP", () =>
		{
			// a shop on the books is not thrown away on one press
			if (resumable && !_confirmNew) { _confirmNew = true; ShowTitle(); return; }
			_confirmNew = false;
			NewShop(null);
			CloseCard();
		}, resumable ? (_confirmNew ? "The shop on the books will be lost." : null) : "Twenty years of the trade, from January 1885.", solid: !resumable);
		c.Choice("SETTINGS", () => ShowSettings(ShowTitle));
		c.Choice("QUIT", () => GetTree().Quit());
		ShowCard(c);
	}

	private void ShowPause()
	{
		var c = new FrontCard(520);
		c.Title("THE MENU", 30);
		c.Line(_game.Ended ? "The business is closed." : "The game is saved at every edition.", 15);
		c.OnEscape = CloseCard;
		c.Choice("☞  BACK TO THE SHOP", CloseCard, solid: true);
		c.Choice("SETTINGS", () => ShowSettings(ShowPause));
		c.Choice("SAVE, AND THE TITLE PAGE", () => { SaveNow(); ShowTitle(); });
		c.Choice("SAVE, AND QUIT", () => { SaveNow(); GetTree().Quit(); });
		ShowCard(c);
	}

	private void ShowSettings(Action back)
	{
		var c = new FrontCard(480);
		c.Title("SETTINGS", 30);
		c.OnEscape = back;
		c.Choice($"THE WINDOW: {(Settings.Fullscreen ? "FULL SCREEN" : "A WINDOW")}", () => { Settings.SetFullscreen(!Settings.Fullscreen); ShowSettings(back); }, "Press to change.");
		c.Choice($"SOUND: {(Settings.Sound ? "ON" : "OFF")}", () => { Settings.SetSound(!Settings.Sound); SyncSound(); ShowSettings(back); }, "The press, the till, the paper.");
		c.Choice("☜  BACK", back, solid: true);
		ShowCard(c);
	}

	/// <summary>Writes the save (a dev run never touches the player's save).</summary>
	private void SaveNow()
	{
		if (_dev || _game is not SimGame sg) return;
		SaveStore.Write(sg.SaveJson());
	}

	private void ContinueSaved()
	{
		var json = SaveStore.Read();
		if (json == null) return;
		using (var f = FileAccess.Open("res://data/paper.en.json", FileAccess.ModeFlags.Read))
			_writer = new EditionWriter(f.GetAsText());
		var writer = _writer;
		var loaded = SimGame.Load(json, facts => writer.Write(facts));
		if (loaded == null) { ShowTitle(); return; }
		_game = loaded;
		_desk.Bind(_game);
		SetDesk(false, slide: false);
		ShowEdition();
		CloseCard();
	}

	/// <summary>A fresh shop: a new seed (the same one in a dev run), a fresh paper, the old save gone.</summary>
	private void NewShop(string? seed)
	{
		if (!_dev) SaveStore.Delete();
		using (var f = FileAccess.Open("res://data/paper.en.json", FileAccess.ModeFlags.Read))
			_writer = new EditionWriter(f.GetAsText());
		_game = SimGame.Start(seed ?? $"shop-{(long)Time.GetUnixTimeFromSystem()}-{GD.Randi() % 10000}");
		_desk.Bind(_game);
		SetDesk(false, slide: false);
		ShowEdition();
		SaveNow();
	}

	/// <summary>Dev: plays N months, saves, loads, and checks the loaded game matches.</summary>
	private void SaveTest(int months)
	{
		for (var i = 0; i < months && !_game.Ended; i++) Autoplay.OneMonth(_game);
		if (_game is not SimGame sg) return;
		var json = sg.SaveJson();
		var back = SimGame.Load(json, null);
		var opt = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
		var a = System.Text.Json.JsonSerializer.Serialize(sg.Desk(), opt) + System.Text.Json.JsonSerializer.Serialize(sg.Facts, opt);
		var b = back == null ? "" : System.Text.Json.JsonSerializer.Serialize(back.Desk(), opt) + System.Text.Json.JsonSerializer.Serialize(back.Facts, opt);
		GD.Print(a == b ? $"SAVETEST OK: {months} months, {json.Length} bytes" : "SAVETEST MISMATCH");
	}

	public override void _Notification(int what)
	{
		// closing the window keeps the shop
		if (what == NotificationWMCloseRequest) SaveNow();
	}

	private void Navigate()
	{
		if (_game.Ended)
		{
			_restarts++;
			NewShop(_dev ? $"{_seed}-{_restarts}" : null);
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
		// the skip hint is in the footer for the whole run of the presses
		UpdateStatus(null);
		var cashBefore = _game.Desk().CashCents;
		Sfx.Play("lever", 0f, 0.02f);
		Sfx.StopLoop("death", 0.3f);
		_game.GoToPress();
		SaveNow();
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
		// the footer stops asking for a key once the edition is out, and a key meant for the
		// press run (pressed as it ends) does not turn the page straight past the paper
		UpdateStatus(null);
		Guard(700);
		FocusMain();
	}

	/// <summary>A click during the press run goes straight to the printed paper.</summary>
	public override void _UnhandledInput(InputEvent e)
	{
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
		// a second, impatient key must not fly straight past the edition just revealed
		_guardUntil = Time.GetTicksMsec() + 1200;
	}

	private static ulong _guardUntil;

	/// <summary>Ignore keys and clicks for a moment, so one impatient press does not do two things.</summary>
	public static void Guard(ulong ms) => _guardUntil = Math.Max(_guardUntil, Time.GetTicksMsec() + ms);

	public override void _Input(InputEvent e)
	{
		// Escape puts down whatever is in your hand: a cutting, then a drawer. Taken here, before
		// the GUI, so a tooltip left showing by the last click cannot swallow it
		if (!_pressing && e.IsActionPressed("ui_cancel") && Time.GetTicksMsec() >= _guardUntil)
		{
			if (_clipping != null) { CloseClipping(); GetViewport().SetInputAsHandled(); return; }
			if (_onDesk && _desk.CloseDrawer()) { GetViewport().SetInputAsHandled(); return; }
			if (_card == null && !_dev) { ShowPause(); GetViewport().SetInputAsHandled(); return; }
		}
		// a cutting in the hand holds the keyboard: Tab and the arrows go nowhere behind it
		if (_clipping != null && (e.IsActionPressed("ui_focus_next") || e.IsActionPressed("ui_focus_prev")
			|| e.IsActionPressed("ui_up") || e.IsActionPressed("ui_down") || e.IsActionPressed("ui_left") || e.IsActionPressed("ui_right")))
		{
			GetViewport().SetInputAsHandled();
			return;
		}
		if (Time.GetTicksMsec() < _guardUntil && (e is InputEventKey { Pressed: true } || e is InputEventMouseButton { Pressed: true }))
			GetViewport().SetInputAsHandled();
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
		// the hint stays above the sheets as they land, for the whole run
		skip.ZIndex = 20;
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
		if (_clipOpener != null && IsInstanceValid(_clipOpener) && _clipOpener.IsVisibleInTree()) _clipOpener.GrabFocus(); else FocusMain();
	}

	private Control? _clipOpener;

	/// <summary>The cutting behind a paper item: laid over the edition; any click puts it down.</summary>
	private void ShowClipping(Clipping c)
	{
		// remember what the reader took it up from, to hand the keyboard back there
		_clipOpener = GetViewport().GuiGetFocusOwner();
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
		text.AddChild(Print.Label("Put it down. (Click, or Escape.)", Print.FellIt, 14, color: Print.Faint));
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
			var paid = _game.Facts.CashCents >= _game.Facts.CashStartCents;
			ct.TweenCallback(Callable.From(() => { if (paid) Sfx.Play("till", -3f); else Sfx.Play("ticket", 0f, 0.02f, 0.55f); }));
		}
		var t = CreateTween();
		// the footer quotes the same figure as the books: closing cash against the month's opening,
		// purchases made at the bench included, so it can never disagree with THE MONTH IN BRIEF
		var f = _game.Facts;
		_monthDelta = f.CashCents - f.CashStartCents;
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
		// Tab from the footer reaches into the paper only while the paper is up
		if (_onDesk) Callable.From(WireDesk).CallDeferred();
		else Callable.From(_edition.WireFocus).CallDeferred();
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
		_nav.Text = _onDesk ? "☜ LAST EDITION" : (_game.Ended ? "BEGIN AGAIN" : "TO THE PROOF ☞");
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
		// reading the paper, say which edition it is, so its date and the month in hand never seem to disagree
		if (!_onDesk && !_game.Ended)
		{
			var f = _game.Facts;
			var printed = f.FirstEdition ? EditionWriter.FirstSaturday(f.Year, f.Month) : EditionWriter.PublicationDate(f.Year, f.Month);
			when = $"The Gazette of {printed.ToString("MMMM d", System.Globalization.CultureInfo.InvariantCulture)} · now {when}";
		}
		_status.Text = _pressing ? "☞ Click, or press any key, to see the edition." : _game.Ended
			? (_game.Facts.Career is { } c
				? $"The business is closed after {(c.Months >= 24 ? $"{c.Months / 12} years" : $"{c.Months} months")}: {c.Bottles:N0} bottles sold, {c.Deaths} dead. The paper has the last word."
				: "The business is closed. The paper has the last word.")
			: $"{when} · cash in hand {EditionWriter.Money(cash ?? d.CashCents)}{(_monthDelta is long dm && !_onDesk ? (dm == 0 ? " (the books even)" : $" (the books {(dm > 0 ? "up" : "down")} {EditionWriter.Money(Math.Abs(dm))})") : "")} · {d.Customers} regulars{(d.Letters.Count == 0 ? "" : $" · {d.Letters.Count} letter{(d.Letters.Count == 1 ? "" : "s")} waiting")}";
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
