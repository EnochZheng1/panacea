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
	private TextureRect _wood = null!;
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

		_game = SimGame.Start(Arg("--seed", "gazette-1885"));
		using (var f = FileAccess.Open("res://data/paper.en.json", FileAccess.ModeFlags.Read))
			_writer = new EditionWriter(f.GetAsText());

		AddChild(new ColorRect { Color = Print.Paper, AnchorRight = 1, AnchorBottom = 1 });
		_wood = new TextureRect
		{
			Texture = GD.Load<Texture2D>("res://assets/polyhaven/dark_wood/albedo.jpg"),
			StretchMode = TextureRect.StretchModeEnum.Tile,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			Modulate = new Color(0.55f, 0.45f, 0.4f),
			AnchorRight = 1,
			AnchorBottom = 1,
			Visible = false,
		};
		AddChild(_wood);

		_edition = new EditionView();
		AddChild(_edition);
		_desk = new DeskView { Visible = false };
		_desk.PressRequested += GoToPress;
		AddChild(_desk);

		var layer = new CanvasLayer { Layer = 5 };
		AddChild(layer);
		_press = new ColorRect { MouseFilter = MouseFilterEnum.Ignore, AnchorRight = 1, AnchorBottom = 1, Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/press.gdshader") } };
		layer.AddChild(_press);

		var ui = new CanvasLayer { Layer = 10 };
		AddChild(ui);
		_nav = Print.Button("TO THE DESK ▸", 18, solid: true);
		_nav.AnchorLeft = _nav.AnchorRight = _nav.AnchorTop = _nav.AnchorBottom = 1;
		_nav.OffsetLeft = -250; _nav.OffsetRight = -24; _nav.OffsetTop = -68; _nav.OffsetBottom = -22;
		_nav.Pressed += () => SetDesk(!_onDesk);
		ui.AddChild(_nav);

		var play = int.Parse(Arg("--play", "0"));
		for (var i = 0; i < play && !_game.Ended; i++) Autoplay.OneMonth(_game);
		ShowEdition();
		if (Arg("--screen", "edition") == "desk") SetDesk(true);
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
		_nav.Text = _onDesk ? "◂ THE PAPER" : (_game.Ended ? "THE END" : "TO THE DESK ▸");
		_nav.Disabled = _game.Ended;
		var m = (ShaderMaterial)_press.Material;
		m.SetShaderParameter("speck", _onDesk ? 0.0f : 0.16f);
		m.SetShaderParameter("wobble", _onDesk ? 0.25f : 0.55f);
	}
}

/// <summary>A plain scripted proprietor, for screenshots and end-to-end smoke tests.</summary>
public static class Autoplay
{
	public static void OneMonth(IGame game)
	{
		var d = game.Desk();
		foreach (var l in d.Letters) game.Resolve(l.InstanceId, l.Choices[0].Id);
		if (d.Formulas.Count == 0)
		{
			var recipe = new (string, double)[] { ("grain_alcohol", 2), ("sugar_syrup", 1), ("peppermint_oil", 0.5), ("laudanum", 0.25), ("water", 4) };
			foreach (var (id, oz) in recipe) game.Buy(id, oz * 40);
			game.CreateFormula("Dr. ——'s Celebrated Panacea", recipe, "simple_mix", 60);
			d = game.Desk();
		}
		var f = d.Formulas[0];
		if (f.ClaimIds.Count == 0)
		{
			var claims = d.Claims.Select(c => c.Id).Where(c => c is "soothes_nerves" or "cures_consumption").ToList();
			game.SetClaims(f.Id, claims);
		}
		if (d.CanAdvertise && d.AdSpendCents == 0) game.SetAdSpend(300);
		if (f.MaxBrewable < 30)
			foreach (var (id, oz) in new (string, double)[] { ("grain_alcohol", 2), ("sugar_syrup", 1), ("peppermint_oil", 0.5), ("laudanum", 0.25) })
				game.Buy(id, oz * 30);
		d = game.Desk();
		var max = d.Formulas[0].MaxBrewable;
		if (d.StillsFree > 0 && max > 0) game.Brew(f.Id, max);
		game.GoToPress();
	}
}
