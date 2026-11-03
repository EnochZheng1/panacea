using System;
using Godot;

namespace Panacea.UI;

/// <summary>
/// The shop's small motions. Every one means something: people breathe, the addicted shake,
/// the dying sway, Death leans; things you can press lift under the hand. Tweens are bound to
/// their node, so they stop when the node is freed (screens rebuild freely).
/// </summary>
public static class Anim
{
	/// <summary>Runs <paramref name="start"/> once the control is in the tree (tweens need it).</summary>
	private static void WhenReady(Control c, Action start)
	{
		if (c.IsInsideTree()) start();
		else c.Ready += start;
	}

	/// <summary>Lifts a button a little under the cursor.</summary>
	public static void LiftOnHover(Control c, float scale = 1.035f)
	{
		Tween? t = null;
		void To(float s)
		{
			if (!c.IsInsideTree()) return;
			c.PivotOffset = c.Size / 2;
			t?.Kill();
			t = c.CreateTween();
			t.TweenProperty(c, "scale", new Vector2(s, s), 0.09f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
		}
		c.MouseEntered += () => To(scale);
		c.MouseExited += () => To(1f);
	}

	/// <summary>A slow rise and fall of the chest, pivoting at the feet.</summary>
	public static void Breathe(Control c, float amount = 0.012f, float period = 2.6f, float phase = 0f) => WhenReady(c, () =>
	{
		c.PivotOffset = new Vector2(c.Size.X / 2, c.Size.Y);
		var t = c.CreateTween().SetLoops();
		if (phase > 0) t.TweenInterval(phase);
		t.TweenProperty(c, "scale", new Vector2(1 - amount * 0.3f, 1 + amount), period / 2).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		t.TweenProperty(c, "scale", Vector2.One, period / 2).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
	});

	/// <summary>Withdrawal: a fine, fast tremor.</summary>
	public static void Shake(Control c, float px = 1.6f) => WhenReady(c, () =>
	{
		var x = c.Position.X;
		var t = c.CreateTween().SetLoops();
		t.TweenProperty(c, "position:x", x + px, 0.05f);
		t.TweenProperty(c, "position:x", x - px, 0.07f);
		t.TweenProperty(c, "position:x", x + px * 0.5f, 0.05f);
		t.TweenProperty(c, "position:x", x, 0.06f);
		t.TweenInterval(0.35f);
	});

	/// <summary>A slow sway from the feet: the gravely ill, or Death keeping them company.</summary>
	public static void Sway(Control c, float degrees = 1.2f, float period = 3.4f, float lean = 0f) => WhenReady(c, () =>
	{
		c.PivotOffset = new Vector2(c.Size.X / 2, c.Size.Y);
		c.RotationDegrees = lean;
		var t = c.CreateTween().SetLoops();
		t.TweenProperty(c, "rotation_degrees", lean + degrees, period / 2).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		t.TweenProperty(c, "rotation_degrees", lean - degrees, period / 2).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
	});

	/// <summary>A quick stamp: something just changed here.</summary>
	public static void Punch(Control c, float scale = 1.06f) => WhenReady(c, () =>
	{
		c.PivotOffset = c.Size / 2;
		c.Scale = new Vector2(scale, scale);
		c.CreateTween().TweenProperty(c, "scale", Vector2.One, 0.28f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
	});

	/// <summary>Slides a control in from an offset while it inks in.</summary>
	public static void SlideIn(Control c, Vector2 from, float seconds = 0.22f) => WhenReady(c, () =>
	{
		var to = c.Position;
		c.Position = to + from;
		c.Modulate = new Color(1, 1, 1, 0);
		var t = c.CreateTween().SetParallel();
		t.TweenProperty(c, "position", to, seconds).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		t.TweenProperty(c, "modulate:a", 1f, seconds * 0.8f);
	});

	/// <summary>A jolt, as when a sheet lands or the platen comes down.</summary>
	public static void Jolt(Control c, float px = 6f)
	{
		if (!c.IsInsideTree()) return;
		var p = c.Position;
		var t = c.CreateTween();
		t.TweenProperty(c, "position", p + new Vector2(0, px), 0.04f);
		t.TweenProperty(c, "position", p + new Vector2(px * 0.4f, -px * 0.3f), 0.05f);
		t.TweenProperty(c, "position", p, 0.08f);
	}
}
