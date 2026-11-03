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
	private DeskView _desk = null!;
	private Button _nav = null!;
	private ColorRect _press = null!;
	private ColorRect _wood = null!;
	private bool _onDesk;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		var args = OS.GetCmdlineUserArgs();
		string Arg(string name, string fallback)
		{
			for (var i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
			return fallback;
		}

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
		AddChild(_edition);
		_desk = new DeskView { Visible = false, OffsetBottom = -FooterHeight };
		_desk.PressRequested += GoToPress;
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
		_nav = InvertedButton("TO THE DESK ▸");
		_nav.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		_nav.CustomMinimumSize = new Vector2(230, 0);
		_nav.Pressed += Navigate;
		row.AddChild(_nav);
		footer.AddChild(row);
		ui.AddChild(footer);
		_seed = Arg("--seed", "gazette-1885");

		var play = int.Parse(Arg("--play", "0"));
		Autoplay.Laudanum = double.Parse(Arg("--dose", "0.25"), System.Globalization.CultureInfo.InvariantCulture);
		for (var i = 0; i < play && !_game.Ended; i++) Autoplay.OneMonth(_game);
		ShowEdition();
		if (Arg("--screen", "edition") == "desk") SetDesk(true);
	}

	private const int FooterHeight = 64;
	private Label _status = null!;
	private string _seed = "";
	private int _restarts;

	/// <summary>On the black footer the ticket is printed white-on-black, inking white on hover.</summary>
	private static Button InvertedButton(string text)
	{
		var b = Print.Button(text, 18);
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

	private void GoToPress()
	{
		if (_game.Ended) return;
		_game.GoToPress();
		ShowEdition();
	}

	private void ShowEdition()
	{
		_edition.Show(_writer.Write(_game.Facts));
		SetDesk(false);
	}

	private void SetDesk(bool desk)
	{
		_onDesk = desk && !_game.Ended;
		if (_onDesk) _desk.Bind(_game);
		_desk.Visible = _onDesk;
		_wood.Visible = _onDesk;
		_edition.Visible = !_onDesk;
		_nav.Text = _onDesk ? "◂ THE PAPER" : (_game.Ended ? "BEGIN AGAIN" : "TO THE DESK ▸");
		var d = _game.Desk();
		var when = new System.DateTime(d.Year, d.Month, 1).ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
		_status.Text = _game.Ended
			? "The business is closed. The paper has the last word."
			: $"{when} · cash in hand {EditionWriter.Money(d.CashCents)} · {d.Customers} regulars · {d.Letters.Count} letter{(d.Letters.Count == 1 ? "" : "s")} waiting";
		var m = (ShaderMaterial)_press.Material;
		m.SetShaderParameter("speck", _onDesk ? 0.0f : 0.16f);
		m.SetShaderParameter("wobble", _onDesk ? 0.25f : 0.55f);
	}
}

/// <summary>A plain scripted proprietor, for screenshots and end-to-end smoke tests.</summary>
public static class Autoplay
{
	/// <summary>Ounces of laudanum per bottle in the scripted recipe (--dose).</summary>
	public static double Laudanum = 0.25;

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
		if (d.CanAdvertise && d.AdSpendCents == 0) Try("ads", game.SetAdSpend(300));
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
