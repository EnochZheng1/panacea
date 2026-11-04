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
			"I am directed to make an inspection of your back premises. Inspections of this kind are thorough, and can run on for weeks, during which the ward will see a Board of Health man in your back room, and talk of it. A small consideration for my trouble has been known to shorten them."),
		["rival_shop_opens"] = new("", "A note pushed under the door", "A friend of the shop", "",
			"Unsigned, in pencil, with a thumbprint.", "",
			"Thought you ought to know. Feeney has opened two doors down and his window is full of bottles the very spit of yours, at a penny less. People will say anything about a man's stock, if they are given a start."),
		["temperance_picket"] = new("THE LADIES' TEMPERANCE LEAGUE", "Sixth Ward Branch · Meetings Thursdays", "Mrs. ABIGAIL STOWE, Secretary", "seal_league",
			"Handed in by a lady, while a dozen more sing hymns on the step.", "Sir,—",
			"The League has caused your remedy to be tasted by a member, who has not been well since. We shall sing outside your door each day until you see the error of your trade, or contribute to the cause of temperance, which we should accept as a first step."),
		["board_of_health"] = new("THE BOARD OF HEALTH OF THE CITY OF NEW-YORK", "Order of the Board", "By order, T. R. KEMBLE, Assistant Sanitary Superintendent", "seal_health",
			"Served with a warrant, by two inspectors and a wagon.", "Sir,—",
			"Complaint having been made of your preparations, the Board directs that the whole of your stock be removed for analysis. The Board will, in the alternative, accept a fine of thirty dollars, and consider the complaint answered."),
		["rival_price_war"] = new("", "A handbill, pushed under the door", "J. FEENEY, Druggist, two doors down", "",
			"Found on the mat, with three more like it in the street.", "",
			"FEENEY'S TONIC, SIXTY-FIVE CENTS THE BOTTLE. Why pay more on Mulberry street? For the next four months, two callers in five whose bottle costs more than mine will try mine instead. Ask anyone. — J. Feeney"),
		["landlord_rent"] = new("", "Upstairs, the front room", "Your obedient servant, M. O'ROURKE, Landlord", "",
			"Brought down by the landlord's boy, who waited for an answer.", "Sir,—",
			"I see the trade is good, and I am glad of it. From this month the rent is a dollar more, and it will stay so. I would sell you the lease outright for sixty dollars, which would bring it a dollar under the old rent for good. The narrow shop across the street is to let at a dollar under the old rent, if you can do with room for eight fewer regulars."),
		["spirits_dearer"] = new("J. & W. HOBART & CO.", "Wholesale Druggists & Importers of Drugs · 81 Pearl Street", "J. & W. HOBART & CO., per T. Hobart", "seal_hobart",
			"A printed circular from Pearl street, with a note in the margin.", "Sir,—",
			"We regret that the distillers have combined, and that grain spirit will be charged at double the old price for six months from your acknowledgment. Should you prefer it, a Jersey distiller outside the ring will supply at the old price for six dollars down; we are not to be named in the matter."),
		["benefit_society"] = new("ST. BRIDGET'S MUTUAL AID SOCIETY", "Meetings Tuesdays · over the bakery, Mott Street", "Your servant, the Secretary (a regular of the shop)", "",
			"Handed across the counter by one of your regulars, in her Sunday hat.", "Dear Doctor,—",
			"The Society would recommend your remedy to its members in return for a subscription of a dollar and a half a month for the year. We think you may count on five more callers a month. The Society asks only that the remedy do its members no harm, which we are sure it will not."),
		["customer_wants_stronger"] = new("", "In pencil, on the back of a handbill", "One of your regulars", "",
			"Left on the counter, folded small.", "",
			"Doc, the tonic dont do what it did. I take two spoons and then four and its like water. You must have something stronger in the back. I can pay. I have to have it."),
	};

	/// <summary>What each answer to the coroner risks, said plainly under the slip.</summary>
	private static readonly Dictionary<string, string> Risks = new()
	{
		["inquest.testify"] = "Your word helps a mild bottle, and hurts a strong one: the jury's chemist tests whatever you bring. If they name your bottle, suspicion rises 24 and your standing falls.",
		["inquest.stay_silent"] = "No chemistry is heard, which suits a strong bottle. If the jury names your bottle anyway, suspicion rises 24 and your standing falls.",
		["inquest.bribe_coroner"] = "The verdict you pay for. His fee rises with every envelope and with the talk in the ward, and an envelope can come to light: a bought verdict found out is worse than a jury's.",
		["grateful_letter.print_testimonial"] = "Standing in the ward +3, and the Gazette will sell you space once you have $100 put by.",
		["grateful_letter.file_away"] = "Nothing gained, nothing risked.",
		["diploma_mill.buy_diploma"] = "$50, and you may style yourself Doctor: a new claim for the type case. You become better known, for better or worse.",
		["diploma_mill.decline"] = "Costs nothing.",
		["sanitary_inspector.pay_fee"] = "$3, and he is gone by dinner. You become a little better known at the Department.",
		["sanitary_inspector.let_him_look"] = "Free, but the ward sees him at it: standing −2.",
		["rival_shop_opens.spread_rumour"] = "Standing +1 for now, and you become better known, which the coroner may notice too.",
		["rival_shop_opens.ignore"] = "Standing −3: the ward takes his side.",
		["temperance_picket.donate"] = "$5, and the ladies move on: suspicion −5.",
		["temperance_picket.ignore_them"] = "Free, but the hymns cost you standing −2.",
		["board_of_health.pay_fine"] = "$30, and the complaint is answered: suspicion −10.",
		["board_of_health.surrender_stock"] = "Every bottle on the shelf is taken away; suspicion −20.",
		["customer_wants_stronger.promise_stronger"] = "Standing +1 with the regular. Keeping the promise is up to the formula book.",
		["customer_wants_stronger.recommend_rest"] = "Good advice, and nothing changes.",
		["rival_price_war.handbills"] = "$10, and Feeney takes nothing from you.",
		["rival_price_war.cry_him_down"] = "Free, and Feeney takes nothing, but suspicion +4 and standing −2.",
		["rival_price_war.let_him_try"] = "For four months he takes two callers in five from any bottle dearer than 65c.",
		["landlord_rent.buy_the_lease"] = "$60 now, and the rent falls a dollar a month, for good.",
		["landlord_rent.smaller_premises"] = "Rent a dollar under the old, for good, but room for eight fewer regulars.",
		["landlord_rent.pay_the_rise"] = "Rent up a dollar a month, for good.",
		["spirits_dearer.outside_distiller"] = "$6 now, and spirit stays at the old price.",
		["spirits_dearer.pay_the_new_price"] = "Grain spirit costs double for six months.",
		["benefit_society.subscribe"] = "$1.50 a month for a year, and about five more callers a month.",
		["benefit_society.decline_politely"] = "Nothing changes.",
	};

	/// <summary>"one in 20", "three in 10": odds a reader can feel.</summary>
	private static string Chance(double p) =>
		p < 0.05 ? "fewer than one in 20" : p < 0.1 ? "about one in 10" : $"about {Math.Clamp((int)Math.Round(p * 10), 1, 9)} in 10";

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
		if (_note != null) AddChild(Print.Slip(Print.Body($"[i]{Print.Escape(EditionWriter.Set(_note))}[/i]", 15, justify: false), 10));
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
			AddChild(Sheet(d.Letters[i], press.AddDays(-2 - (int)(h % 12)), i, d.Letters.Take(i + 1).Count(x => x.EventId == "inquest")));
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

	private Control Sheet(Letter l, DateTime date, int index, int inquestNo = 0)
	{
		var head = Heads.GetValueOrDefault(l.EventId) ?? new Head("", "", "Yours, &c.", "", "", "Sir,—", "");
		var at = l.Title.IndexOf("Death of ", StringComparison.Ordinal);
		var concerns = at >= 0 ? l.Title[(at + 9)..].Replace("the Infant of", "the infant child of").Replace("an Unknown", "an unknown") : "";
		if (concerns.StartsWith("an unknown", StringComparison.Ordinal)) concerns = concerns.ToLowerInvariant();
		var body = head.Voice.Length > 0 ? head.Voice.Replace("{name}", concerns.Length > 0 ? concerns : "a person") : l.Body;
		// the Clerk keeps count: a busy month's summonses say so
		if (l.EventId == "inquest" && inquestNo >= 2)
			body += $" This is the {Nth(inquestNo)} such notice the Clerk has had the honour to serve on you this month.";
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
		// a printed letter is dated; a note pushed under the door knows only the day
		var note = head.Sender.Length == 0;
		v.AddChild(Print.Label(note ? date.DayOfWeek + "." : $"{(l.EventId == "diploma_mill" ? "Cincinnati" : "New-York")}, {date.ToString("MMMM d", CultureInfo.InvariantCulture)}{Ordinal(date.Day)}, {date.Year}.", Print.FellIt, 14, HorizontalAlignment.Right));
		v.AddChild(Print.Label("Re: " + EditionWriter.Set(l.Title), Print.Rye, 15));
		v.AddChild(Print.Body(note ? $"[i]{Print.Escape(EditionWriter.Set(head.Salute + body))}[/i]" : Print.Escape(EditionWriter.Set(head.Salute + body)), note ? 17 : 16, justify: false));
		v.AddChild(Print.Label(l.From != null ? $"Your grateful servant, {l.From}" : head.Sign, Print.FellIt, 14, HorizontalAlignment.Right));

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
			// the reply and the line under it come into view together, with room to spare
			Control? riskLine = null;
			b.FocusEntered += () => Callable.From(() =>
			{
				for (Node? n = b; n != null; n = n.GetParent())
					if (n is ScrollContainer sc)
					{
						sc.EnsureControlVisible(riskLine ?? b);
						var bottom = (riskLine ?? b).GetGlobalRect().End.Y - sc.GetGlobalRect().End.Y;
						if (bottom > -30) sc.ScrollVertical += (int)(bottom + 30);
						break;
					}
			}).CallDeferred();
			if (Risks.TryGetValue($"{l.EventId}.{id}", out var risk))
			{
				string? pencil = null;
				// the verdict is given as soon as the reply goes: say what the odds are now
				if (l.Odds.TryGetValue(id, out var p))
					pencil = id == "bribe_coroner"
						? $"Pencilled in the margin: {Chance(p)} that it comes out. —J.D."
						: $"Pencilled in the margin: {Chance(p)} for naming" +
							(l.Odds.TryGetValue(id == "testify" ? "stay_silent" : "testify", out var q) && Chance(q) == Chance(p) ? ", speak or not" : "") + ". The jury sits at once. —J.D.";
				var line = Print.Label(risk, Print.FellIt, 13, color: new Color(Print.Ink, 0.85f));
				riskLine = line;
				v.AddChild(line);
				if (pencil != null)
				{
					var margin = Print.Label(pencil, Print.FellIt, 13, HorizontalAlignment.Right, new Color(Print.Ink, 0.85f));
					margin.RotationDegrees = -0.8f;
					riskLine = margin;
					v.AddChild(margin);
				}
			}
		}
		var s = Print.Slip(v, 18);
		// notes lie crooked on the desk; printed letters nearly straight
		s.RotationDegrees = (index % 2 == 0 ? -0.6f : 0.5f) * (head.Sender.Length == 0 ? 3f : 1f);
		sheet = s;
		return s;
	}

	private void Answer(Control sheet, string instance, string choice, string said)
	{
		var d0 = _game.Desk();
		var before = d0.CashCents;
		var heat0 = d0.Heat;
		var inquest = d0.Letters.FirstOrDefault(x => x.InstanceId == instance)?.EventId == "inquest";
		var err = _game.Resolve(instance, choice);
		if (err != null) { _note = "Refused: " + err; Refresh(); return; }
		var d1 = _game.Desk();
		var after = d1.CashCents;
		var money = after == before ? "" : after < before ? $" It cost {EditionWriter.Money(before - after)}." : $" It brought in {EditionWriter.Money(after - before)}.";
		// say what happened at once; only what has not happened yet is left to the paper
		var stockGone = d0.Formulas.Sum(f => f.Stock) - d1.Formulas.Sum(f => f.Stock);
		var talk = d1.Heat - heat0;
		var standing = d1.Reputation - d0.Reputation;
		var now = new List<string>();
		if (stockGone > 0) now.Add($"{stockGone} bottles are gone from the shelf");
		if (Math.Abs(talk) >= 1) now.Add($"suspicion {(talk > 0 ? "rises" : "falls")} {Math.Abs(talk):0}");
		if (Math.Abs(standing) >= 1) now.Add($"your standing {(standing > 0 ? "rises" : "falls")} {Math.Abs(standing):0}");
		var outcome = !inquest ? (now.Count > 0 ? " At once: " + string.Join("; ", now) + "." : "") + " Anything further will be in the next edition."
			: choice == "bribe_coroner" ? " The coroner has your envelope; the verdict will be the one you paid for."
			: d1.Heat > heat0 + 10 ? $" The jury sat at once and named your bottle. Suspicion is now {d1.Heat:0} of 100. The paper will have the rest."
			: " The jury sat at once and found nothing against you. The paper will have the verdict.";
		_note = $"Sent: “{said}”{money}{outcome}";
		Main.Guard(800);

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
		t.TweenCallback(Callable.From(() =>
		{
			Refresh();
			Changed?.Invoke();
			// the next letter's first reply takes the keyboard, scrolled into view
			Callable.From(() =>
			{
				if (!IsInsideTree()) return;
				var first = Print.Focusables(this).OfType<Button>().FirstOrDefault(x => !x.Disabled);
				if (first == null) return;
				first.GrabFocus();
				for (Node? n = this; n != null; n = n.GetParent())
					if (n is ScrollContainer sc) { sc.EnsureControlVisible(first); break; }
			}).CallDeferred();
		}));
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

	private static string Nth(int n) => n switch { 2 => "second", 3 => "third", 4 => "fourth", 5 => "fifth", 6 => "sixth", 7 => "seventh", 8 => "eighth", _ => $"{n}th" };

	private static string Ordinal(int d) => (d % 100) is 11 or 12 or 13 ? "th" : (d % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" };
}
