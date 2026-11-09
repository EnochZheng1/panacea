using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Panacea.UI;

/// <summary>The one save: the game as its seed and its log of actions (see SimGame.SaveJson).</summary>
public static class SaveStore
{
	/// <summary>The save's file; --profile NAME gives a run its own (several testers on one machine).</summary>
	public static string PathName = "user://panacea.save";

	public static bool Exists => FileAccess.FileExists(PathName);

	public static void Write(string json)
	{
		// written to a fresh file and moved over the old, so a crash mid-write keeps the last save
		var tmp = PathName + ".tmp";
		using (var f = FileAccess.Open(tmp, FileAccess.ModeFlags.Write))
		{
			if (f == null) return;
			f.StoreString(json);
		}
		DirAccess.RenameAbsolute(ProjectSettings.GlobalizePath(tmp), ProjectSettings.GlobalizePath(PathName));
	}

	public static string? Read()
	{
		if (!Exists) return null;
		using var f = FileAccess.Open(PathName, FileAccess.ModeFlags.Read);
		return f?.GetAsText();
	}

	public static void Delete()
	{
		if (Exists) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(PathName));
	}

	/// <summary>The line the title card prints under CONTINUE ("March 1887 · $312.40 · 22 regulars").</summary>
	public static string? Label()
	{
		try
		{
			var json = Read();
			if (json == null) return null;
			using var doc = System.Text.Json.JsonDocument.Parse(json);
			return doc.RootElement.TryGetProperty("Label", out var l) ? l.GetString() : null;
		}
		catch (Exception) { return null; }
	}
}

/// <summary>The player's settings, kept between sessions: the window and the sound.</summary>
public static class Settings
{
	public static string PathName = "user://settings.cfg";
	public static bool Fullscreen { get; private set; }
	public static bool Sound { get; private set; } = true;

	public static void Load()
	{
		var cfg = new ConfigFile();
		if (cfg.Load(PathName) == Error.Ok)
		{
			Fullscreen = (bool)cfg.GetValue("window", "fullscreen", false);
			Sound = (bool)cfg.GetValue("sound", "on", true);
		}
		Apply();
	}

	public static void SetFullscreen(bool on) { Fullscreen = on; Apply(); Save(); }
	public static void SetSound(bool on) { Sound = on; Apply(); Save(); }

	private static void Apply()
	{
		// headless runs (screenshots, the driver) have no window to change
		if (DisplayServer.GetName() != "headless")
			DisplayServer.WindowSetMode(Fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
		Sfx.SetEnabled(Sound);
	}

	private static void Save()
	{
		var cfg = new ConfigFile();
		cfg.SetValue("window", "fullscreen", Fullscreen);
		cfg.SetValue("sound", "on", Sound);
		cfg.Save(PathName);
	}
}

/// <summary>
/// A card laid over the table: the title page with its content note, the settings, or the pause
/// menu. Printed matter like everything else: a framed slip, its choices as tickets.
/// </summary>
public partial class FrontCard : Control
{
	private readonly VBoxContainer _body = new();
	private readonly List<Button> _buttons = new();
	/// <summary>Escape on this card (null: Escape does nothing, as on the title page).</summary>
	public Action? OnEscape;

	public FrontCard(int width = 620)
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		var shade = new ColorRect { Color = new Color(Print.Table, 0.92f), MouseFilter = MouseFilterEnum.Ignore };
		shade.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(shade);
		var centre = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		centre.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(centre);
		_body.AddThemeConstantOverride("separation", 10);
		_body.CustomMinimumSize = new Vector2(width, 0);
		centre.AddChild(Print.Slip(_body, 26));
	}

	public void Title(string text, int size = 40) => _body.AddChild(Print.Label(text, Print.Rye, size, HorizontalAlignment.Center));
	public void Line(string text, int size = 16, bool italic = true) => _body.AddChild(Print.Label(text, italic ? Print.FellIt : Print.Fell, size, HorizontalAlignment.Center));
	public void Rule() => _body.AddChild(Print.Rule(3f, 1f, 2f));

	/// <summary>A boxed notice: the content note, in small type within a rule.</summary>
	public void Notice(string head, string text)
	{
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 4);
		v.AddChild(Print.Label(head, Print.Rye, 14, HorizontalAlignment.Center));
		v.AddChild(Print.Body($"[center]{Print.Escape(text)}[/center]", 15, justify: false));
		_body.AddChild(Print.Panel(v, Print.Box(new Color(0, 0, 0, 0), Print.Ink, 1, 10)));
	}

	public Button Choice(string text, Action pressed, string? under = null, bool solid = false)
	{
		var b = Print.Button(text, 18, solid: solid);
		b.Pressed += pressed;
		_body.AddChild(b);
		_buttons.Add(b);
		if (under != null) _body.AddChild(Print.Label(under, Print.FellIt, 14, HorizontalAlignment.Center));
		return b;
	}

	public override void _Ready()
	{
		Callable.From(() => { if (IsInstanceValid(this)) _buttons.FirstOrDefault(b => !b.Disabled)?.GrabFocus(); }).CallDeferred();
	}

	public override void _Input(InputEvent e)
	{
		if (!IsVisibleInTree()) return;
		// the card holds the keyboard: Tab and the arrows walk its choices, Escape backs out
		if (e.IsActionPressed("ui_cancel"))
		{
			OnEscape?.Invoke();
			GetViewport().SetInputAsHandled();
			return;
		}
		var next = e.IsActionPressed("ui_focus_next") || e.IsActionPressed("ui_down");
		var prev = e.IsActionPressed("ui_focus_prev") || e.IsActionPressed("ui_up");
		if (!next && !prev) return;
		var live = _buttons.Where(b => !b.Disabled && b.IsVisibleInTree()).ToList();
		if (live.Count == 0) return;
		var at = live.IndexOf(GetViewport().GuiGetFocusOwner() as Button ?? live[0]);
		live[(Math.Max(0, at) + (next ? 1 : -1) + live.Count) % live.Count].GrabFocus();
		GetViewport().SetInputAsHandled();
	}
}
