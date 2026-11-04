using Godot;

namespace Panacea.UI;

/// <summary>
/// A price-current article: the name, then a row of leader dots carrying the eye across to the
/// price, as a compositor sets them (". . . . ."), stopping short of the next column.
/// </summary>
public partial class Leader : Label
{
	private const float Pitch = 7f;

	public override void _Draw()
	{
		var font = GetThemeFont("font");
		var size = GetThemeFontSize("font_size");
		var w = font.GetStringSize(Text, HorizontalAlignment.Left, -1, size).X;
		var baseline = (Size.Y + font.GetAscent(size) - font.GetDescent(size)) / 2f;
		var ink = new Color(GetThemeColor("font_color"), 0.7f);
		// dots sit on a fixed pitch from the cell's left edge, so the rows line up down the column
		var x = Mathf.Ceil((w + 6f) / Pitch) * Pitch;
		for (; x <= Size.X + 8f; x += Pitch) DrawCircle(new Vector2(x, baseline - 1.5f), 1.1f, ink);
	}

	public override void _Notification(int what)
	{
		if (what == NotificationResized) QueueRedraw();
	}
}
