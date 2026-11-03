using Godot;

namespace Panacea.UI;

/// <summary>
/// A container with a dashed ink border: the composing room's mark for "space not yet set". An
/// optional solid accent ring (the product's green) marks the space that belongs to the player.
/// </summary>
public partial class Dashed : MarginContainer
{
	public Color Ink = Print.Ink;
	public float Width = 2f;
	public float Dash = 7f;
	public Color? Accent;

	public Dashed(int pad = 10)
	{
		foreach (var side in new[] { "left", "right", "top", "bottom" }) AddThemeConstantOverride("margin_" + side, pad);
	}

	public override void _Draw()
	{
		var r = new Rect2(Vector2.Zero, Size);
		var a = r.Position; var b = new Vector2(r.End.X, r.Position.Y); var c = r.End; var d = new Vector2(r.Position.X, r.End.Y);
		DrawDashedLine(a, b, Ink, Width, Dash);
		DrawDashedLine(b, c, Ink, Width, Dash);
		DrawDashedLine(c, d, Ink, Width, Dash);
		DrawDashedLine(d, a, Ink, Width, Dash);
		if (Accent is Color g) DrawRect(r.Grow(5), g, false, 2.5f);
	}

	public override void _Notification(int what)
	{
		if (what == NotificationResized) QueueRedraw();
	}
}
