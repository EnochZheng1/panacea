using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Panacea.Game;
using Panacea.Paper;

namespace Panacea.UI;

/// <summary>
/// The morning's post. Each letter is a sheet on its sender's letterhead: the coroner's office
/// with its seal, a diploma mill's flourish, a customer's own hand. Replies are slips under the
/// letter. Answering stamps the sheet and folds it away, and says at once what the answer cost.
/// </summary>
public partial class LettersDesk : VBoxContainer
{
	private readonly IGame _game;
	private string? _note;
	public event Action? Changed;

	private sealed record Head(string Sender, string Address, string Sign, string Seal);

	/// <summary>Who writes each kind of letter.</summary>
	private static readonly Dictionary<string, Head> Heads = new()
	{
		["inquest"] = new("OFFICE OF THE CORONER", "City and County of New-York · Centre Street", "By order of the Coroner, H. FOGARTY, Clerk", "CORONER"),
		["grateful_letter"] = new("", "Mulberry Street, top floor, rear", "Your grateful servant, a customer", ""),
		["diploma_mill"] = new("THE ECLECTIC MEDICAL COLLEGE OF OHIO", "Diplomas Conferred by Post · Cincinnati", "J. T. MARSH, M.D. (Hon.), Registrar", "FIFTY DOLLARS"),
		["sanitary_inspector"] = new("DEPARTMENT OF HEALTH · SANITARY BUREAU", "301 Mott Street", "P. QUINLAN, Inspector of Nuisances", "HEALTH"),
		["rival_shop_opens"] = new("", "A note pushed under the door", "A friend of the shop", ""),
		["temperance_picket"] = new("THE LADIES' TEMPERANCE LEAGUE", "Sixth Ward Branch · Meetings Thursdays", "Mrs. ABIGAIL STOWE, Secretary", "LEAGUE"),
		["board_of_health"] = new("THE BOARD OF HEALTH OF THE CITY OF NEW-YORK", "Order of the Board", "By order, T. R. KEMBLE, Assistant Sanitary Superintendent", "BOARD OF HEALTH"),
		["customer_wants_stronger"] = new("", "In pencil, on the back of a handbill", "One of your regulars", ""),
	};

	public LettersDesk(IGame game)
	{
		_game = game;
		AddThemeConstantOverride("separation", 14);
		Refresh();
	}

	public void Refresh()
	{
		foreach (var c in GetChildren()) { RemoveChild(c); c.QueueFree(); }
		var d = _game.Desk();
		var top = new HBoxContainer();
		top.AddChild(Print.Label("THE MORNING'S POST", Print.Rye, 22, color: Print.Paper));
		top.AddChild(Print.Label(d.Letters.Count == 0 ? "nothing for you" : d.Letters.Count == 1 ? "one letter" : $"{d.Letters.Count} letters", Print.FellIt, 17, HorizontalAlignment.Right, Print.Paper));
		// a band of ink, so the heading never sits on the proof behind the shade
		AddChild(Print.Panel(top, Print.Box(Print.Ink, Print.Ink, 0, 8)));
		if (_note != null) AddChild(Print.Slip(Print.Body($"[i]{Print.Escape(_note)}[/i]", 15, justify: false), 10));
		if (d.Letters.Count == 0)
		{
			AddChild(Envelope());
			return;
		}
		var date = EditionWriter.PublicationDate(d.Year, d.Month).AddDays(-6 + d.Letters.Count);
		for (var i = 0; i < d.Letters.Count; i++)
			AddChild(Sheet(d.Letters[i], date, i));
	}

	private Control Envelope()
	{
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 4);
		v.AddChild(Print.Label("A CIRCULAR", Print.Rye, 18, HorizontalAlignment.Center));
		v.AddChild(Print.Body("[center][i]Dr. Grimsby begs to announce his Galvanic Belt, which restores Lost Vigor through every Nerve. Sent on thirty days' trial.[/i][/center]", 15, justify: false));
		v.AddChild(Print.Body("[center]Nothing else in the post this morning.[/center]", 14, justify: false));
		var s = Print.Slip(v, 18);
		s.RotationDegrees = -1f;
		return s;
	}

	private Control Sheet(Letter l, DateTime date, int index)
	{
		var head = Heads.GetValueOrDefault(l.EventId) ?? new Head("", "", "Yours, &c.", "");
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 6);

		// the letterhead: printed, or nothing (a hand-written note)
		var lh = new HBoxContainer();
		lh.AddThemeConstantOverride("separation", 14);
		var names = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		names.AddThemeConstantOverride("separation", 0);
		if (head.Sender.Length > 0)
		{
			names.AddChild(Print.Label(head.Sender, Print.Rye, 17));
			names.AddChild(Print.Label(head.Address, Print.FellIt, 13));
		}
		else names.AddChild(Print.Label(head.Address, Print.FellIt, 15));
		lh.AddChild(names);
		if (head.Seal.Length > 0) lh.AddChild(Seal(head.Seal));
		v.AddChild(lh);
		if (head.Sender.Length > 0) v.AddChild(Print.Rule(2, 0.8f, 2));
		v.AddChild(Print.Label($"New-York, {date.ToString("MMMM d", CultureInfo.InvariantCulture)}{Ordinal(date.Day)}, {date.Year}.", Print.FellIt, 14, HorizontalAlignment.Right));
		v.AddChild(Print.Label("Re: " + l.Title, Print.Rye, 15));
		v.AddChild(Print.Body((head.Sender.Length > 0 ? "Sir,—" : "") + Print.Escape(l.Body), 16, justify: false));
		v.AddChild(Print.Label(head.Sign, Print.FellIt, 14, HorizontalAlignment.Right));

		// the replies, each a slip to send back
		v.AddChild(Print.Label("YOUR REPLY", Print.Fell, 12));
		Control? sheet = null;
		foreach (var (id, text) in l.Choices)
		{
			var b = Print.Button("☞  " + text, 14, sound: null);
			b.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			b.Alignment = HorizontalAlignment.Left;
			var choice = id;
			var said = text;
			b.Pressed += () => Answer(sheet!, l.InstanceId, choice, said);
			v.AddChild(b);
		}
		var s = Print.Slip(v, 18);
		s.RotationDegrees = index % 2 == 0 ? -0.6f : 0.5f;
		sheet = s;
		return s;
	}

	private void Answer(Control sheet, string instance, string choice, string said)
	{
		var before = _game.Desk().CashCents;
		var err = _game.Resolve(instance, choice);
		if (err != null) { _note = "Refused: " + err; Refresh(); return; }
		var after = _game.Desk().CashCents;
		var money = after == before ? "" : after < before ? $" It cost {EditionWriter.Money(before - after)}." : $" It brought in {EditionWriter.Money(after - before)}.";
		_note = $"Sent: “{said}”{money} The outcome, if any, will be in the next edition.";

		// the rubber stamp comes down, and the letter is folded away
		Sfx.Play("sheet", 0f, 0.05f, 0.8f);
		var stamp = Print.Panel(Print.Label("ANSWERED", Print.Rye, 30, HorizontalAlignment.Center, new Color(Print.Ink, 0.75f), wrap: false), Print.Box(new Color(0, 0, 0, 0), new Color(Print.Ink, 0.75f), 4, 6));
		stamp.MouseFilter = MouseFilterEnum.Ignore;
		stamp.Position = new Vector2(sheet.Size.X * 0.5f - 110, 30);
		stamp.RotationDegrees = -14;
		stamp.Scale = new Vector2(1.6f, 1.6f);
		sheet.AddChild(stamp);
		stamp.TopLevel = false;
		var t = sheet.CreateTween();
		t.TweenProperty(stamp, "scale", Vector2.One, 0.12f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
		t.TweenInterval(0.45f);
		t.TweenCallback(Callable.From(() => { sheet.PivotOffset = new Vector2(sheet.Size.X / 2, 0); Sfx.Play("slip", -4f); }));
		t.TweenProperty(sheet, "scale:y", 0.05f, 0.25f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
		t.Parallel().TweenProperty(sheet, "modulate:a", 0f, 0.25f);
		t.TweenCallback(Callable.From(() => { Refresh(); Changed?.Invoke(); }));
	}

	/// <summary>A wax seal, inked: a solid disc with the sender's word reversed out.</summary>
	private static Control Seal(string word)
	{
		var size = word.Length > 8 ? 84 : 72;
		var p = new PanelContainer { CustomMinimumSize = new Vector2(size, size), SizeFlagsVertical = SizeFlags.ShrinkBegin };
		var st = Print.Box(Print.Ink, Print.Ink, 0, 4);
		st.SetCornerRadiusAll(size / 2);
		p.AddThemeStyleboxOverride("panel", st);
		var l = Print.Label(word, Print.Rye, word.Length > 8 ? 9 : 11, HorizontalAlignment.Center, Print.Paper, wrap: word.Contains(' '));
		l.VerticalAlignment = VerticalAlignment.Center;
		p.AddChild(l);
		p.RotationDegrees = -10;
		return p;
	}

	private static string Ordinal(int d) => (d % 100) is 11 or 12 or 13 ? "th" : (d % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
}
