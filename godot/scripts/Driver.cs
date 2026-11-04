using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Godot;

namespace Panacea;

/// <summary>
/// Dev tool for scripted playtesting (reviewers, smoke tests): with <c>-- --driver PORT</c> the
/// game listens on 127.0.0.1:PORT for one JSON object per line and answers each with one JSON
/// line. Input is synthesised through Godot's own input pipeline, so it exercises the real UI.
///
/// Actions: {"click":[x,y]} {"rclick":[x,y]} {"move":[x,y]} {"scroll":[x,y,steps]} (negative = up)
/// {"key":"Escape"} {"type":"some text"} {"wait":ms} {"shot":"/abs/path.png"}
/// {"focus":true} where the keyboard focus is.
/// {"targets":true} clickable things on screen: buttons, paper items, jars (text, tooltip, rect)
/// {"read":true} visible text on screen with positions; {"state":true} the game as the player sees it.
/// </summary>
public partial class Driver : Node
{
	private TcpListener? _listener;
	private TcpClient? _client;
	private StreamReader? _in;
	private StreamWriter? _out;
	private readonly Queue<string> _pending = new();
	private ulong _waitUntil;
	private int _framesLeft = -1;
	private string? _shotPath;
	private Func<JsonObject>? _state;

	public static Driver? Start(Node parent, int port, Func<JsonObject> state)
	{
		var d = new Driver { _state = state };
		parent.AddChild(d);
		try
		{
			d._listener = new TcpListener(IPAddress.Loopback, port);
			d._listener.Start();
		}
		catch (Exception e)
		{
			d._listener = null;
			GD.PrintErr($"DRIVER could not listen on {port}: {e.Message}");
			return d;
		}
		GD.Print($"DRIVER listening on 127.0.0.1:{port}");
		return d;
	}

	public override void _ExitTree() => _listener?.Stop();

	public override void _Process(double delta)
	{
		if (_listener == null) return;
		// one reviewer at a time: a new connection replaces the old one (whose end may be gone)
		if (_listener.Pending())
		{
			if (_client != null && _pending.Count == 0 && _waitUntil == 0 && _framesLeft < 0) Drop();
		}
		if (_client == null && _listener.Pending())
		{
			_client = _listener.AcceptTcpClient();
			var s = _client.GetStream();
			_in = new StreamReader(s, new UTF8Encoding(false));
			_out = new StreamWriter(s, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
		}
		if (_client == null) return;
		try
		{
			while (_client.Available > 0 && _in!.Peek() >= 0)
			{
				var line = _in.ReadLine();
				if (!string.IsNullOrWhiteSpace(line)) _pending.Enqueue(line);
			}
		}
		catch (Exception) { Drop(); return; }
		if (!_client.Connected) { Drop(); return; }

		// a screenshot waits a couple of drawn frames so the input has been laid out and painted
		if (_framesLeft > 0) { _framesLeft--; return; }
		if (_framesLeft == 0)
		{
			_framesLeft = -1;
			var img = GetViewport().GetTexture().GetImage();
			var err = img.SavePng(_shotPath!);
			Reply(new JsonObject { ["ok"] = err == Error.Ok, ["shot"] = _shotPath, ["error"] = err == Error.Ok ? null : err.ToString() });
			return;
		}
		if (_waitUntil > Time.GetTicksMsec()) return;
		if (_waitUntil != 0) { _waitUntil = 0; Reply(new JsonObject { ["ok"] = true }); return; }
		if (_pending.Count == 0) return;
		Run(_pending.Dequeue());
	}

	private void Drop()
	{
		_client?.Close();
		_client = null;
		_pending.Clear();
	}

	private void Reply(JsonObject o)
	{
		try { _out?.WriteLine(o.ToJsonString()); } catch (Exception) { Drop(); }
	}

	private void Run(string line)
	{
		JsonObject a;
		try { a = JsonNode.Parse(line)!.AsObject(); }
		catch (Exception e) { Reply(new JsonObject { ["ok"] = false, ["error"] = "bad json: " + e.Message }); return; }
		try
		{
			if (a["click"] is JsonArray c) { Click(P(c), MouseButton.Left); Reply(Ok()); }
			else if (a["rclick"] is JsonArray rc) { Click(P(rc), MouseButton.Right); Reply(Ok()); }
			else if (a["move"] is JsonArray m) { Move(P(m)); Reply(Ok()); }
			else if (a["scroll"] is JsonArray sc)
			{
				var at = P(sc);
				var steps = sc.Count > 2 ? sc[2]!.GetValue<int>() : 3;
				Move(at);
				for (var i = 0; i < Math.Abs(steps); i++)
				{
					var btn = steps > 0 ? MouseButton.WheelDown : MouseButton.WheelUp;
					Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = btn, Pressed = true, Position = at, GlobalPosition = at, Factor = 1 });
					Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = btn, Pressed = false, Position = at, GlobalPosition = at, Factor = 1 });
				}
				Reply(Ok());
			}
			else if (a["focus"] is not null)
			{
				// where the keyboard is: the focused control's kind, text and rectangle
				var f = GetViewport().GuiGetFocusOwner();
				var text = f switch { Button bt => bt.Text, Label lb => lb.Text, _ => f?.Name.ToString() ?? "" };
				var r = f?.GetGlobalRect() ?? new Rect2();
				Reply(new JsonObject { ["ok"] = true, ["focus"] = f?.GetClass() ?? "none", ["text"] = text, ["rect"] = new JsonArray((int)r.Position.X, (int)r.Position.Y, (int)r.Size.X, (int)r.Size.Y) });
			}
			else if (a["key"] is JsonNode k)
			{
				var code = OS.FindKeycodeFromString(k.GetValue<string>());
				Input.ParseInputEvent(new InputEventKey { Keycode = code, PhysicalKeycode = code, Pressed = true });
				Input.ParseInputEvent(new InputEventKey { Keycode = code, PhysicalKeycode = code, Pressed = false });
				Reply(Ok());
			}
			else if (a["type"] is JsonNode t)
			{
				foreach (var ch in t.GetValue<string>())
				{
					Input.ParseInputEvent(new InputEventKey { Unicode = ch, Pressed = true, Keycode = Key.None });
					Input.ParseInputEvent(new InputEventKey { Unicode = ch, Pressed = false, Keycode = Key.None });
				}
				Reply(Ok());
			}
			else if (a["wait"] is JsonNode w) _waitUntil = Time.GetTicksMsec() + (ulong)Math.Max(1, w.GetValue<int>());
			else if (a["shot"] is JsonNode sp) { _shotPath = sp.GetValue<string>(); _framesLeft = 3; }
			else if (a.ContainsKey("targets")) Reply(new JsonObject { ["ok"] = true, ["targets"] = Targets() });
			else if (a.ContainsKey("read")) Reply(new JsonObject { ["ok"] = true, ["text"] = ReadText() });
			else if (a.ContainsKey("state")) Reply(new JsonObject { ["ok"] = true, ["state"] = _state?.Invoke() });
			else Reply(new JsonObject { ["ok"] = false, ["error"] = "unknown action: " + line });
		}
		catch (Exception e) { Reply(new JsonObject { ["ok"] = false, ["error"] = e.Message }); }
	}

	private static JsonObject Ok() => new() { ["ok"] = true };
	private static Vector2 P(JsonArray a) => new(a[0]!.GetValue<float>(), a[1]!.GetValue<float>());

	private static void Move(Vector2 at)
	{
		Input.WarpMouse(at);
		Input.ParseInputEvent(new InputEventMouseMotion { Position = at, GlobalPosition = at, Relative = new Vector2(1, 1) });
	}

	private static void Click(Vector2 at, MouseButton b)
	{
		Move(at);
		Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = b, Pressed = true, Position = at, GlobalPosition = at });
		Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = b, Pressed = false, Position = at, GlobalPosition = at });
	}

	private IEnumerable<Control> Visible()
	{
		var screen = GetViewport().GetVisibleRect();
		var stack = new Stack<Node>();
		stack.Push(GetTree().Root);
		while (stack.Count > 0)
		{
			var n = stack.Pop();
			if (n is CanvasItem ci && !ci.Visible) continue;
			if (n is Control c)
			{
				var r = c.GetGlobalRect();
				if (r.Size.X > 1 && r.Size.Y > 1 && screen.Intersects(r)) yield return c;
			}
			foreach (var k in n.GetChildren()) stack.Push(k);
		}
	}

	private static JsonArray Rect(Rect2 r) => new((int)r.Position.X, (int)r.Position.Y, (int)r.Size.X, (int)r.Size.Y);

	private static string TextIn(Node n)
	{
		var parts = new List<string>();
		void Walk(Node x)
		{
			if (x is Label l && !string.IsNullOrWhiteSpace(l.Text)) parts.Add(l.Text);
			else if (x is RichTextLabel rt && !string.IsNullOrWhiteSpace(rt.GetParsedText())) parts.Add(rt.GetParsedText());
			foreach (var k in x.GetChildren()) Walk(k);
		}
		Walk(n);
		var s = string.Join(" | ", parts).Replace("\n", " ");
		return s.Length > 160 ? s[..160] + "…" : s;
	}

	private JsonArray Targets()
	{
		var list = new JsonArray();
		foreach (var c in Visible())
		{
			if (c is Button b)
				list.Add(new JsonObject { ["kind"] = "button", ["text"] = b.Text, ["disabled"] = b.Disabled, ["tooltip"] = b.TooltipText, ["rect"] = Rect(b.GetGlobalRect()) });
			else if (c.MouseDefaultCursorShape == Control.CursorShape.PointingHand)
				list.Add(new JsonObject { ["kind"] = "clickable", ["text"] = TextIn(c), ["tooltip"] = c.TooltipText, ["rect"] = Rect(c.GetGlobalRect()) });
			else if (c is LineEdit le)
				list.Add(new JsonObject { ["kind"] = "text field", ["text"] = le.Text, ["rect"] = Rect(le.GetGlobalRect()) });
		}
		return list;
	}

	private JsonArray ReadText()
	{
		var list = new JsonArray();
		foreach (var c in Visible())
		{
			var t = c switch { Label l => l.Text, RichTextLabel r => r.GetParsedText(), _ => null };
			if (!string.IsNullOrWhiteSpace(t)) list.Add(new JsonObject { ["text"] = t.Replace("\n", " "), ["rect"] = Rect(c.GetGlobalRect()) });
		}
		return list;
	}
}
