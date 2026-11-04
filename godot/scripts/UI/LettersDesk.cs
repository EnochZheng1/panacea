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

	/// <summary>
	/// Who writes each kind of letter, and what they write, in their own voice. The docket is the
	/// clerk's note of how it came (set in italic above the sheet); {name} is the person the letter
	/// concerns, read off its title.
	/// </summary>
	private sealed record Head(string Sender, string Address, string Sign, string Seal, string Docket, string Salute, string Voice);

	private static readonly Dictionary<string, Head> Heads = new()
	{
		["inquest"] = new("OFFICE OF THE CORONER", "City and County of New-York · Centre Street", "By order of the Coroner, J. DUFFY, Clerk", "seal_coroner",
			"Served at the shop by a boy from Centre street.", "Sir,—",
			"You are hereby notified that a jury will sit this month upon the death of {name}, who is said to have been a customer of yours. The Coroner will hear any evidence you may wish to give as to the composition of your preparations. The Coroner's time is valuable, as is his good opinion."),
		["grateful_letter"] = new("", "Mulberry Street, top floor, rear", "Your grateful servant, a customer", "",
			"In a careful hand, on good paper bought for the purpose.", "Dear Doctor,—",
			"Three physicians gave me up, and your tonic gave me back. I am a new woman and my husband says he hardly knows me. If my words would be of use to other sufferers, you have my leave to put them in the papers, with my name, which I should like spelled correctly."),
		["diploma_mill"] = new("THE ECLECTIC MEDICAL COLLEGE OF OHIO", "Diplomas Conferred by Post · Cincinnati", "J. T. MARSH, M.D. (Hon.), Registrar", "seal_college",
			"A printed circular, with the blanks filled in by hand.", "Dear Doctor,—",
			"The Faculty have heard of your work with great interest, and are pleased to offer you the degree of Doctor of Medicine on receipt of fifty dollars. Attendance is not required; examination is waived in cases of evident merit, and all cases are evident. The diploma is suitable for framing."),
		["sanitary_inspector"] = new("DEPARTMENT OF HEALTH · SANITARY BUREAU", "301 Mott Street", "P. QUINLAN, Inspector of Nuisances", "seal_sanitary",
			"Left on the counter by a man in a hard hat, who is still in the back room.", "Sir,—",
			"I am directed to make an inspection of your back premises. Inspections of this kind are thorough, and can run on for weeks, during which the shop cannot well be used. A small consideration for my trouble has been known to shorten them."),
		["rival_shop_opens"] = new("", "A note pushed under the door", "A friend of the shop", "",
			"Unsigned, in pencil, with a thumbprint.", "",
			"Thought you ought to know. Feeney has opened two doors down and his window is full of bottles the very spit of yours, at a penny less. People will say anything about a man's stock, if they are given a start."),
		["temperance_picket"] = new("THE LADIES' TEMPERANCE LEAGUE", "Sixth Ward Branch · Meetings Thursdays", "Mrs. ABIGAIL STOWE, Secretary", "seal_league",
			"Handed in by a lady, while a dozen more sing hymns on the step.", "Sir,—",
			"The League has caused your remedy to be tasted by a member, who has not been well since. We shall sing outside your door each day until you see the error of your trade, or contribute to the cause of temperance, which we should accept as a first step."),
		["board_of_health"] = new("THE BOARD OF HEALTH OF THE CITY OF NEW-YORK", "Order of the Board", "By order, T. R. KEMBLE, Assistant Sanitary Superintendent", "seal_health",
			"Served with a warrant, by two inspectors and a wagon.", "Sir,—",
			"Complaint having been made of your preparations, the Board directs that the whole of your stock be removed for analysis. The Board will, in the alternative, accept a fine of thirty dollars, and consider the complaint answered."),
		["customer_wants_stronger"] = new("", "In pencil, on the back of a handbill", "One of your regulars", "",
			"Left on the counter, folded small.", "",
			"Doc, the tonic dont do what it did. I take two spoons and then four and its like water. You must have something stronger in the back. I can pay. I have to have it."),
	};

	/// <summary>What each answer to the coroner risks, said plainly under the slip.</summary>
	private static readonly Dictionary<string, string> Risks = new()
	{
		["testify"] = "The best odds with the jury, and if they name your bottle anyway, no worse than silence.",
		["stay_silent"] = "Costs nothing now. If the jury names your bottle, the ward will talk all the more for your absence.",
		["bribe_coroner"] = "The verdict you pay for. His fee rises every time, and with the talk in the ward.",
	};

	public LettersDesk(IGame game)
	{
		_game = game;
		AddThemeConstantOverride("separation", 14);
		Refresh();
	}

	public void Refresh()
	{
		var keep = Print.FocusIndex(this);
		RefreshBody();
		Print.RestoreFocus(this, keep);
	}

	private void RefreshBody()
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
		// each letter keeps the date it arrived: fixed by the letter itself, never after the proof
		var press = EditionWriter.PublicationDate(d.Year, d.Month);
		for (var i = 0; i < d.Letters.Count; i++)
		{
			var h = 0u;
			foreach (var ch in d.Letters[i].InstanceId) h = (h ^ ch) * 16777619u;
			AddChild(Sheet(d.Letters[i], press.AddDays(-2 - (int)(h % 12)), i));
		}
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
		var head = Heads.GetValueOrDefault(l.EventId) ?? new Head("", "", "Yours, &c.", "", "", "Sir,—", "");
		var at = l.Title.IndexOf("Death of ", StringComparison.Ordinal);
		var concerns = at >= 0 ? l.Title[(at + 9)..].Replace("the Infant of", "the infant child of") : "";
		var body = head.Voice.Length > 0 ? head.Voice.Replace("{name}", concerns.Length > 0 ? concerns : "a person") : l.Body;
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 6);

		// the clerk's docket: how the letter came
		if (head.Docket.Length > 0) v.AddChild(Print.Label(head.Docket, Print.FellIt, 13, color: new Color(Print.Ink, 0.85f)));

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
		v.AddChild(Print.Label($"{(l.EventId == "diploma_mill" ? "Cincinnati" : "New-York")}, {date.ToString("MMMM d", CultureInfo.InvariantCulture)}{Ordinal(date.Day)}, {date.Year}.", Print.FellIt, 14, HorizontalAlignment.Right));
		v.AddChild(Print.Label("Re: " + EditionWriter.Set(l.Title), Print.Rye, 15));
		v.AddChild(Print.Body(Print.Escape(EditionWriter.Set(head.Salute + body)), 16, justify: false));
		v.AddChild(Print.Label(head.Sign, Print.FellIt, 14, HorizontalAlignment.Right));

		// the replies, each a slip to send back
		v.AddChild(Print.Label("YOUR REPLY", Print.Fell, 12));
		Control? sheet = null;
		foreach (var (id, text) in l.Choices)
		{
			var b = Print.Button("☞  " + EditionWriter.Set(text), 14, sound: null);
			b.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			b.Alignment = HorizontalAlignment.Left;
			var choice = id;
			var said = text;
			b.Pressed += () => Answer(sheet!, l.InstanceId, choice, said);
			v.AddChild(b);
			if (Risks.TryGetValue(id, out var risk)) v.AddChild(Print.Label(risk, Print.FellIt, 13, color: new Color(Print.Ink, 0.85f)));
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

	/// <summary>
	/// The sender's engraved seal (tools/make_seals.py): a roundel with its legend round the rim,
	/// printed in the letterhead's corner.
	/// </summary>
	public static Control Seal(string cut, int size = 104)
	{
		var tex = Print.Cut(cut);
		var r = new TextureRect
		{
			Texture = tex,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			CustomMinimumSize = new Vector2(size, size),
			SizeFlagsVertical = SizeFlags.ShrinkBegin,
			PivotOffset = new Vector2(size / 2f, size / 2f),
			RotationDegrees = -8,
		};
		return r;
	}

	private static string Ordinal(int d) => (d % 100) is 11 or 12 or 13 ? "th" : (d % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
}
