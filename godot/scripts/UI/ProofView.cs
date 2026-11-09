using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Panacea.Game;
using Panacea.Paper;

namespace Panacea.UI;

/// <summary>
/// The month's work, done on next edition's proof. Left: the proof page (your space on it, the
/// lead story still blank) and the counter, where the regulars stand and Death waits behind the
/// one nearest the end. Right: the composing bench (type case, letters, the works, the books
/// and the press). Detailed tools open as slips laid over the proof.
/// </summary>
public partial class ProofView : Control
{
	private IGame _game = null!;
	private string? _drawer;
	public event Action? PressRequested;
	/// <summary>Raised after every rebuild (an action changed the game), so the footer can follow.</summary>
	public event Action? Changed;
	private Button? _lever;

	public void FocusLever() { if (_drawer == null && _lever != null && IsInstanceValid(_lever)) _lever.GrabFocus(); }

	/// <summary>
	/// Coming to the proof, the keyboard starts where the month's work starts: the formula book
	/// while there is no remedy, the still while it is cold and has something to brew, else the press.
	/// </summary>
	public void FocusStart()
	{
		if (_drawer != null || _game == null) { FocusLever(); return; }
		var d = _game.Desk();
		var start = d.Formulas.Count == 0 ? "FORMULA" : d.Brewing.Count == 0 && d.StillsFree > 0 && d.Formulas.Sum(f => f.Stock) < 24 ? "THE STILL" : null;
		var b = start == null ? null : Print.Focusables(this).OfType<Button>().FirstOrDefault(x => x.Text == start && !x.Disabled);
		if (b != null) b.GrabFocus(); else FocusLever();
	}

	/// <summary>Closes an open tool slip; false if none was open.</summary>
	public bool CloseDrawer()
	{
		if (_drawer == null) return false;
		Sfx.Play("slip", -4f, 0.04f, 0.85f);
		var was = _drawer;
		_drawer = null;
		DropBench();
		Rebuild();
		// the keyboard goes back to the button that opened the drawer, never to the press
		var opener = was switch { "still" => "THE STILL", "formula" => "FORMULA", "supplies" => "SUPPLIES", "ventures" => "VENTURES", "letters" => "OPEN THE LETTERS", _ => "" };
		Callable.From(() =>
		{
			// (the letters button is gone once the post is answered: then the works, not the press)
			var b = Print.Focusables(this).OfType<Button>().FirstOrDefault(x => x.Text == opener && !x.Disabled)
				?? Print.Focusables(this).OfType<Button>().FirstOrDefault(x => x.Text == "THE STILL" && !x.Disabled);
			if (b != null) b.GrabFocus(); else FocusLever();
		}).CallDeferred();
		return true;
	}

	public ProofView()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
	}

	public void Bind(IGame game)
	{
		_note = null;
		_game = game;
		_drawer = null;
		DropBench();
		Rebuild();
	}

	private void Refresh() => Rebuild();

	/// <summary>Opens a tool over the proof: letters, still, formula or supplies.</summary>
	private bool _drawerOpened;
	private bool _skipRestore;
	private Control? _holder;
	private int _slipsScroll;
	/// <summary>The remedy whose label the type case is setting, when there is more than one.</summary>
	private string? _adFormula;

	/// <summary>
	/// While a drawer is open, Tab and Shift+Tab go round the drawer's own controls only: the bench
	/// behind the shade is out of reach until the drawer is put down.
	/// </summary>
	/// <summary>True while the player is using the keyboard (the last input was a key, not the mouse).</summary>
	private bool _keyboard;
	/// <summary>The stills and the ventures on offer when those drawers were last opened.</summary>
	private int _seenStills, _seenOffers;
	/// <summary>Whom Death stands beside at the counter this month, if anyone.</summary>
	private string? _deathBeside;

	/// <summary>
	/// The keyboard never stands nowhere: when an action takes away the focused control (the fire
	/// lit, a slip reset) focus goes to the drawer's first control, or the works, rather than to none.
	/// </summary>
	public override void _Process(double delta)
	{
		if (!_keyboard || !IsVisibleInTree()) return;
		var owner = GetViewport().GuiGetFocusOwner();
		if (owner != null && owner.IsVisibleInTree()) return;
		if (_drawer != null && _holder != null && IsInstanceValid(_holder))
			Print.Focusables(_holder).FirstOrDefault(c => !(c is BaseButton { Disabled: true }))?.GrabFocus();
		else if (_drawer == null)
			(Print.Focusables(this).OfType<Button>().FirstOrDefault(x => x.Text == "THE STILL" && !x.Disabled) as Control
				?? (_lever != null && IsInstanceValid(_lever) && _lever.IsInsideTree() ? _lever : null))?.GrabFocus();
	}

	public override void _Input(InputEvent e)
	{
		if (e is InputEventKey { Pressed: true }) _keyboard = true;
		else if (e is InputEventMouseButton { Pressed: true }) _keyboard = false;
		if (_drawer == null || _holder == null || !IsInstanceValid(_holder) || !IsVisibleInTree()) return;
		var next = e.IsActionPressed("ui_focus_next");
		var prev = e.IsActionPressed("ui_focus_prev");
		// the arrow keys walk the drawer too, in reading order, and never out onto the bench
		// behind it (a control that uses the arrows itself, a slider or a text field, keeps them)
		var owner = GetViewport().GuiGetFocusOwner();
		var ownsArrows = owner is Godot.Range or LineEdit or TextEdit or Print.IArrowKeys;
		if (!ownsArrows)
		{
			next |= e.IsActionPressed("ui_down") || e.IsActionPressed("ui_right");
			prev |= e.IsActionPressed("ui_up") || e.IsActionPressed("ui_left");
		}
		if (!next && !prev) return;
		var items = Print.Focusables(_holder).Where(c => !(c is BaseButton { Disabled: true })).ToList();
		if (items.Count == 0) return;
		var at = items.IndexOf(GetViewport().GuiGetFocusOwner());
		var to = at < 0 ? (next ? 0 : items.Count - 1) : (at + (next ? 1 : -1) + items.Count) % items.Count;
		items[to].GrabFocus();
		GetViewport().SetInputAsHandled();
	}
	private FormulaBench? _bench;
	private Control? _tool;
	private string? _toolKind;

	private void DropBench()
	{
		if (_tool != null) { _tool.GetParent()?.RemoveChild(_tool); _tool.QueueFree(); _tool = null; _toolKind = null; }
		if (_bench == null) return;
		_bench.GetParent()?.RemoveChild(_bench);
		_bench.QueueFree();
		_bench = null;
	}

	/// <summary>True while Death stands at someone's shoulder on the counter.</summary>
	public bool DeathPresent { get; private set; }

	public void DemoPour(IReadOnlyList<string> ids) => _bench?.Demo(ids);

	public void OpenDrawer(string which)
	{
		// a game that has ended has no proof to open a drawer on
		if (_game == null) return;
		_drawerOpened = true;
		_drawer = which;
		Rebuild();
	}

	private string? _note;
	private bool _punch;

	private void Act(string? error)
	{
		_note = error;
		_punch = error == null;
		Rebuild();
	}

	public void Rebuild()
	{
		// a keyboard player keeps their place on the bench when it is reset after a click
		var keep = Print.FocusIndex(this);
		_bench?.GetParent()?.RemoveChild(_bench);
		_tool?.GetParent()?.RemoveChild(_tool);
		foreach (var c in GetChildren()) { RemoveChild(c); c.QueueFree(); }
		var m = _game.Desk();

		var root = new HBoxContainer();
		root.SetAnchorsPreset(LayoutPreset.FullRect);
		root.OffsetLeft = 22; root.OffsetRight = -22; root.OffsetTop = 18; root.OffsetBottom = -14;
		root.AddThemeConstantOverride("separation", 20);
		AddChild(root);

		// the proof column; drawers are laid over exactly this area
		var leftStack = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		root.AddChild(leftStack);
		var left = new VBoxContainer();
		left.SetAnchorsPreset(LayoutPreset.FullRect);
		left.AddThemeConstantOverride("separation", 12);
		leftStack.AddChild(left);
		left.AddChild(ProofPage(m));
		left.AddChild(Counter(m));

		var bench = new VBoxContainer { CustomMinimumSize = new Vector2(BenchWidth, 0) };
		bench.AddThemeConstantOverride("separation", 10);
		root.AddChild(bench);
		// the slips scroll if the type case runs long; the warnings and the lever never move
		var slipsFrame = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		// the case keeps its place when the bench is reset under the hand
		var keepScroll = _slipsScroll;
		slipsFrame.Ready += () => Callable.From(() => { if (IsInstanceValid(slipsFrame)) slipsFrame.ScrollVertical = keepScroll; }).CallDeferred();
		slipsFrame.GetVScrollBar().ValueChanged += v => _slipsScroll = (int)v;
		Print.QuietScrollbar(slipsFrame);
		bench.AddChild(slipsFrame);
		var slips = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		slips.AddThemeConstantOverride("separation", 10);
		slipsFrame.AddChild(slips);
		slips.AddChild(TypeCase(m));
		// the post and the works hold the drawers' buttons: always in view, never scrolled away
		// fixed heights, so the buttons stay where the hand left them from month to month
		var post = LettersSlip(m);
		post.CustomMinimumSize = new Vector2(0, 104);
		bench.AddChild(post);
		bench.AddChild(WorksSlip(m));
		var warn = new List<string>();
		var unansweredInquests = m.Letters.Count(l => l.Title.StartsWith("Inquest", StringComparison.Ordinal));
		if (unansweredInquests > 0) warn.Add($"{Spell(unansweredInquests)} inquest{(unansweredInquests == 1 ? "" : "s")} unanswered: the coroner will proceed without you.");
		else if (m.Letters.Count > 0) warn.Add($"{Spell(m.Letters.Count)} letter{(m.Letters.Count == 1 ? "" : "s")} unanswered: the usual reply will go out for you.");
		if (_deathBeside != null) warn.Add($"Death waits at the counter beside {_deathBeside}.");
		var nothing = m.Formulas.Sum(f => f.Stock) == 0 && m.Brewing.Count == 0;
		// the first month: say where to begin, in so many words
		if (m.Formulas.Count == 0) warn.Add("Begin with the FORMULA book: pour a remedy and enter it. Then light THE STILL, and send the paper to press.");
		else if (nothing) warn.Add("Nothing on the shelf and nothing on the still: light THE STILL, or the shop will sell nothing this month.");
		// a shelf that keeps ending bare is money left on the counter, and orders left unfilled
		if (m.ShelfBareMonths >= 2 && !nothing)
			warn.Add($"The shelf ended bare in {Spell(m.ShelfBareMonths)} of the last three months: brew more, or look in THE STILL for what the next still waits on.");
		// space the shop cannot pay for is not printed: say so before the press, not after
		if (m.CanAdvertise && m.AdSpendCents > m.CashCents)
			warn.Add($"The notice costs {EditionWriter.Money(m.AdSpendCents)} and you have {EditionWriter.Money(m.CashCents)}: the Gazette will not print it unpaid.");
		// the warnings sit in a box of fixed depth under the lever's row, so they never move it
		var warnings = new VBoxContainer { CustomMinimumSize = new Vector2(0, 76) };
		warnings.AddThemeConstantOverride("separation", 1);
		foreach (var w in warn.Take(4)) warnings.AddChild(Print.Label("☞ " + w, Print.FellIt, 13, color: Print.Paper));
		bench.AddChild(warnings);
		bench.AddChild(Lever(nothing || unansweredInquests > 0));

		if (_drawer != null)
		{
			leftStack.AddChild(Drawer(_drawer));
			// while a drawer is open the bench is out of reach of the mouse as well as the keyboard
			bench.Modulate = new Color(1, 1, 1, 0.6f);
			void Lock(Node n) { foreach (var k in n.GetChildren()) { if (k is Control c) c.MouseFilter = MouseFilterEnum.Ignore; Lock(k); } }
			Lock(bench);
		}
		if (!_skipRestore) Print.RestoreFocus(this, keep);
		_skipRestore = false;
		Changed?.Invoke();
	}

	// ---------- the proof page ----------

	private Control ProofPage(DeskModel m)
	{
		var page = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		page.AddThemeConstantOverride("separation", 4);
		var date = EditionWriter.PublicationDate(m.Year, m.Month);
		page.AddChild(Print.Label($"PROOF FOR {date.DayOfWeek.ToString().ToUpperInvariant()}, {date.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture).ToUpperInvariant()} · CORRECT AND RETURN TO THE COMPOSING ROOM", Print.FellIt, 12, HorizontalAlignment.Center));
		page.AddChild(Print.Label("THE BOWERY GAZETTE", Print.Rye, 40, HorizontalAlignment.Center));
		page.AddChild(Print.Rule(4, 1.4f, 2));

		// the columns take whatever height is left and never push the counter down: a long
		// Coroner's Court is cut off at the foot of the proof, as a compositor would
		var holder = new Control { SizeFlagsVertical = SizeFlags.ExpandFill, ClipContents = true };
		page.AddChild(holder);
		var cols = new HBoxContainer();
		cols.SetAnchorsPreset(LayoutPreset.FullRect);
		cols.AddThemeConstantOverride("separation", 0);
		holder.AddChild(cols);

		// left column: what will be set after press
		var lc = Col(186);
		lc.AddChild(Head("DEATHS."));
		lc.AddChild(Print.Label("set after press", Print.FellIt, 12, HorizontalAlignment.Center));
		// last month's matter, struck out in proof marks, waits for this month's type
		var last = _game.Facts;
		Struck(lc, last.Deaths.Select(d => d.Anonymous ? "UNKNOWN.—A body, unclaimed." : d.Infant ? $"{SurnameOf(d.Name)}.—The infant of {d.Name}." : $"{SurnameOf(d.Name)}.—{d.Name}."), "No deaths last month.");
		lc.AddChild(Head("CORONER'S COURT."));
		var inquests = m.Letters.Where(l => l.Title.StartsWith("Inquest", StringComparison.Ordinal)).ToList();
		if (inquests.Count == 0) lc.AddChild(Print.Label("set after press", Print.FellIt, 12, HorizontalAlignment.Center));
		foreach (var q in inquests.Take(2))
			lc.AddChild(Print.Body($"{Print.Escape(q.Title)} sits this month. [i]See your letters.[/i]", 12));
		if (inquests.Count > 2) lc.AddChild(Print.Body($"[i]And {inquests.Count - 2} more: see your letters.[/i]", 12));
		lc.AddChild(Head("PERSONAL."));
		Struck(lc, last.Joined.Select(p => $"{p.Name}, a new customer.").Concat(last.Left.Select(l => $"{l.Person.Name}, gone from the shop.")), "Nothing personal last month.");
		cols.AddChild(lc);
		cols.AddChild(VRule());

		// centre: the lead story, blank until press (or your notice, if you bought the front page)
		var centre = Col(0, expand: true);
		var tier = m.CanAdvertise ? AdSpace.TierFor(m.AdSpendCents) : 0;
		var product = m.Formulas.FirstOrDefault();
		var lead = new Dashed(14) { SizeFlagsVertical = SizeFlags.ExpandFill, Ink = new Color(Print.Ink, 0.4f) };
		var lv = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		lv.AddThemeConstantOverride("separation", 6);
		if (tier == AdSpace.Front && product != null)
		{
			lead.Accent = Print.Green;
			lv.AddChild(Print.Label("YOUR NOTICE, SET AS NEWS", Print.Rye, 13, HorizontalAlignment.Center));
			lv.AddChild(Print.Label((product.ClaimIds.Count > 0 ? m.Claims.First(c => c.Id == product.ClaimIds[0]).Name : "Never Felt Better").ToUpperInvariant() + "!", Print.Rye, 30, HorizontalAlignment.Center));
			lv.AddChild(Print.Label("A Remarkable Restoration.", Print.FellIt, 18, HorizontalAlignment.Center));
			lv.AddChild(Print.Body("[i]The Gazette will find a satisfied customer to quote, and end the column with a very small[/i] Adv.", 13, centre: true));
		}
		else
		{
			lv.AddChild(Print.Label("THE LEAD STORY", Print.Rye, 26, HorizontalAlignment.Center, new Color(Print.Ink, 0.72f)));
			lv.AddChild(Print.Body("[i]The editor sets it when the month is out. Whatever happens in the ward, it will be printed here.[/i]", 14, new Color(Print.Ink, 0.88f), centre: true));
		}
		var ghostDeath = Print.Cut("wc_death");
		if (ghostDeath != null)
			lv.AddChild(new TextureRect { Texture = ghostDeath, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, CustomMinimumSize = new Vector2(0, 150), Modulate = new Color(1, 1, 1, 0.16f) });
		lead.AddChild(lv);
		centre.AddChild(lead);
		cols.AddChild(centre);
		cols.AddChild(VRule());

		// right column: your space, and the competition's
		var rc = Col(214);
		rc.AddChild(YourSpace(m, tier, product));
		rc.AddChild(SmallAd("DR. GRIMSBY'S GALVANIC BELT", "Restores Lost Vigor!"));
		if (tier < 2 || product == null) rc.AddChild(SmallAd("MRS. WHITTLE'S COMPOUND", "She answers every letter."));
		cols.AddChild(rc);

		var sheet = Print.Panel(page, Sheet());
		var stack = new Control { SizeFlagsVertical = SizeFlags.ExpandFill, ClipContents = true };
		sheet.SetAnchorsPreset(LayoutPreset.FullRect);
		stack.AddChild(sheet);
		var stamp = Print.Label("PROOF", Print.Rye, 90, HorizontalAlignment.Center, new Color(Print.Ink, 0.06f), wrap: false);
		stamp.Position = new Vector2(220, 170);
		stamp.RotationDegrees = -22;
		stamp.MouseFilter = MouseFilterEnum.Ignore;
		stack.AddChild(stamp);
		return stack;
	}

	private Control YourSpace(DeskModel m, int tier, FormulaRow? product)
	{
		var box = new Dashed(8) { Accent = tier > 0 && product != null ? Print.Ink : null };
		if (_punch) { _punch = false; Anim.Punch(box, 1.07f); }
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 3);
		var tag = Print.Panel(Print.Label($"YOUR SPACE · {AdSpace.Tiers[tier].Short}{(tier > 0 ? " · " + EditionWriter.Money(AdSpace.Tiers[tier].Cents) : "")}", Print.Rye, 11, HorizontalAlignment.Center, Print.Paper), Print.Box(Print.Ink, Print.Ink, 0, 3));
		v.AddChild(tag);
		if (product == null)
			v.AddChild(Print.Body("[i]Nothing to advertise. Enter a recipe in the formula book.[/i]", 13, centre: true));
		else if (!m.CanAdvertise)
		{
			var ads = m.NextUnlocks.FirstOrDefault(u => u.Name.Contains("Advertis"));
			var savings = m.CashCents >= 10000 ? "$100 in savings (you have it)" : $"$100 in savings (you have {EditionWriter.Money(m.CashCents)})";
			v.AddChild(Print.Body(ads.Name != null && ads.Progress >= 0.999
				? "[i]Both conditions are met. The Gazette will take your money from next month.[/i]"
				: $"[i]The Gazette will not take your money yet. It wants {savings} and a printed testimonial: print one when a grateful customer writes.[/i]", 13, centre: true));
		}
		else if (tier == 0)
			v.AddChild(Print.Body("[i]Empty. Buy space from the type case and the ward will read your claims.[/i]", 13, centre: true));
		else
		{
			if (tier >= 3) v.AddChild(Print.Label("The Wonder of the Age!", Print.FellIt, 12, HorizontalAlignment.Center));
			v.AddChild(Print.Label(product.Name.ToUpperInvariant(), Print.Rye, 16, HorizontalAlignment.Center));
			var cut = Print.Cut("wc_bottle_panacea");
			if (tier >= 2 && cut != null)
				v.AddChild(new TextureRect { Texture = cut, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, CustomMinimumSize = new Vector2(0, tier >= 3 ? 110 : 76) });
			var claims = product.ClaimIds.Select(id => m.Claims.FirstOrDefault(c => c.Id == id).Name).Where(n => n != null).ToList();
			v.AddChild(Print.Label(claims.Count > 0 ? string.Join(" · ", claims).ToUpperInvariant() : "FOR ALL COMPLAINTS", Print.Rye, 11, HorizontalAlignment.Center));
			if (tier >= 2) v.AddChild(Print.Label($"{EditionWriter.Money(product.PriceCents)} the bottle.", Print.FellIt, 11, HorizontalAlignment.Center));
		}
		box.AddChild(v);
		return box;
	}

	// ---------- the counter ----------

	/// <summary>A compositor spells out small numbers (IM Fell's old-style figures misread small).</summary>
	// numbers under a hundred are words on the bench, as in the paper
	private static string Spell(int n) => EditionWriter.Words(n);

	private static int Risk(Regular r) =>
		(r.Looks switch { "gravely_ill" => 30, "poorly" => 20, "peaky" => 8, _ => 0 }) + (r.Withdrawal ? 6 : 0) + (r.Craving ? 3 : 0) + (r.Infant ? 4 : 0);

	private Control Counter(DeskModel m)
	{
		var strip = new Control { CustomMinimumSize = new Vector2(0, 292) };
		var bg = new ColorRect { Color = Print.Paper };
		bg.SetAnchorsPreset(LayoutPreset.FullRect);
		strip.AddChild(bg);
		var tint = new ColorRect { Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/tint.gdshader") } };
		tint.SetAnchorsPreset(LayoutPreset.FullRect);
		strip.AddChild(tint);
		var counter = new ColorRect { Color = Print.Ink, AnchorTop = 1, AnchorRight = 1, AnchorBottom = 1, OffsetTop = -66 };
		strip.AddChild(counter);
		strip.AddChild(new ColorRect { Color = Print.Paper, AnchorTop = 1, AnchorRight = 1, AnchorBottom = 1, OffsetTop = -62, OffsetBottom = -60 });

		var title = Print.Label("AT THE COUNTER", Print.Rye, 14, HorizontalAlignment.Right, wrap: false);
		title.AnchorLeft = title.AnchorRight = 1;
		title.OffsetLeft = -300; title.OffsetRight = -14; title.OffsetTop = 8;
		strip.AddChild(title);

		// the most notable regulars: the sick first, then the hooked, then the new
		var shown = m.Regulars.OrderByDescending(Risk).ThenBy(r => r.MonthsAsCustomer).Take(5).ToList();
		// he stands beside the gravest case shown, and the warning names the same customer
		var atRisk = shown.FirstOrDefault(r => r.Looks is "gravely_ill") ?? shown.FirstOrDefault(r => r.Looks is "poorly");
		_deathBeside = atRisk?.Name;
		DeathPresent = atRisk != null;
		var others = m.Regulars.Count - shown.Count;
		var more = Print.Label(others > 0 ? $"and {Spell(others)} more regular{(others == 1 ? "" : "s")}" : m.Regulars.Count == 0 ? "No one has come in yet." : "", Print.FellIt, 13);
		more.HorizontalAlignment = HorizontalAlignment.Right;
		more.AnchorLeft = more.AnchorRight = 1;
		more.OffsetLeft = -300; more.OffsetRight = -14; more.OffsetTop = 28;
		more.MouseFilter = MouseFilterEnum.Ignore;
		strip.AddChild(more);

		// the strip is as wide as the proof column; share it out between the figures
		const float width = 1280f - 44f - 20f - BenchWidth, start = 54f;
		var slot = Math.Min(172f, (width - start - 70f) / Math.Max(shown.Count, 1));
		var tags = new List<(PanelContainer Tag, string About)>();
		for (var i = 0; i < shown.Count; i++)
		{
			var r = shown[i];
			var x = start + i * slot;
			// Death stands at the shoulder of whoever is nearest the end, a head taller than anyone
			// he leans toward the one he is waiting for
			if (r == atRisk && AddFigure(strip, "wc_death", x - 44, 230, 1f) is { } death)
			{
				Anim.Sway(death, 1.4f, 4.2f, lean: 3.5f);
				death.MouseFilter = _drawer == null ? MouseFilterEnum.Pass : MouseFilterEnum.Ignore;
				death.TooltipText = $"Death, at the shoulder of {r.Name}, who is {(r.Looks == "gravely_ill" ? "gravely ill" : "poorly")}. He stands beside whoever is nearest the end.";
			}
			// the same stock cut twice in a row is printed the other way round
			var cut = CutFor(r);
			var flip = shown.Take(i).Count(o => CutFor(o) == cut) % 2 == 1;
			// people are not all one height, even when the printer owns one engraving of them
			var h = 176 + (int)(StableHash(r.Id) % 28u);
			var fig = AddFigure(strip, cut, x + 10, h, 1f, flip);
			if (fig != null)
			{
				// behind an open drawer the counter keeps its hover text to itself
				fig.MouseFilter = _drawer == null ? MouseFilterEnum.Pass : MouseFilterEnum.Ignore;
				fig.TooltipText = $"{r.Name}: {(r.Infant ? "for " : "")}{r.Ailment.ToLowerInvariant()}. A regular for {r.MonthsAsCustomer} month{(r.MonthsAsCustomer == 1 ? "" : "s")}; {Panacea.Game.SimGame.Describe(r)}.";
				var phase = (StableHash(r.Id) % 100u) / 100f * 2f;
				if (r.Looks == "gravely_ill") Anim.Sway(fig, 1.6f, 3.0f + phase * 0.5f);
				else Anim.Breathe(fig, r.Looks == "poorly" ? 0.006f : 0.012f, 2.4f + phase * 0.4f, phase);
				if (r.Withdrawal) Anim.Shake(fig);
			}
			// the printer owns one engraving per trade: a repeat is labelled as what it is
			// (never in the right-hand slots, where it would sit on the AT THE COUNTER heading)
			if (shown.Take(i).Any(o => CutFor(o) == cut) && x + slot <= width - 300)
			{
				var no = Print.Label($"Cut No. {3 + StableHash(cut) % 37u}, again.", Print.FellIt, 12, HorizontalAlignment.Center, wrap: false);
				// over the figure's head, where the printer's own number would be
				// clear of the AT THE COUNTER heading in the right-hand slots
				no.Position = new Vector2(x, Math.Max(4, CounterTop - h - 20));
				no.CustomMinimumSize = new Vector2(slot - 10, 0);
				no.MouseFilter = MouseFilterEnum.Ignore;
				strip.AddChild(no);
			}
			var tag = Print.Slip(Tag(r), 5);
			tag.Position = new Vector2(x, CounterTop + 2);
			tag.CustomMinimumSize = new Vector2(slot - 10, 0);
			// what the mouse would show on hover, for the keyboard (see below)
			var about = $"{r.Name}: a regular for {Spell(r.MonthsAsCustomer)} month{(r.MonthsAsCustomer == 1 ? "" : "s")}; {Panacea.Game.SimGame.Describe(r)}.";
			tag.TooltipText = _drawer != null ? "" : fig?.TooltipText ?? about;
			if (_drawer != null) tag.MouseFilter = MouseFilterEnum.Ignore;
			strip.AddChild(tag);
			tags.Add((tag, about));
		}
		// the counter is one stop in the Tab order; the arrows walk along it. The customer in hand
		// is printed in reverse, white on black, and described in the line under AT THE COUNTER
		if (tags.Count > 0)
		{
			strip.FocusMode = FocusModeEnum.All;
			var moreText = more.Text;
			var sel = 0;
			void Mark(int i, bool on)
			{
				var (t, about) = tags[i];
				t.AddThemeStyleboxOverride("panel", on ? Print.Box(Print.Ink, Print.Paper, 3, 4) : Print.Box(Print.Paper, Print.Ink, 3, 4));
				foreach (var l in Print.Descendants<Label>(t)) l.AddThemeColorOverride("font_color", on ? Print.Paper : Print.Ink);
				if (on) more.Text = about + (tags.Count > 1 ? "  (◀ ▶ for the next)" : "");
			}
			// a ring round the whole counter while it has the keyboard, as round any other stop
			var ring = new ReferenceRect { EditorOnly = false, BorderColor = Print.Ink, BorderWidth = 3, Visible = false, MouseFilter = MouseFilterEnum.Ignore };
			ring.SetAnchorsPreset(LayoutPreset.FullRect);
			strip.AddChild(ring);
			strip.FocusEntered += () => { ring.Visible = true; Mark(sel, true); };
			strip.FocusExited += () => { ring.Visible = false; Mark(sel, false); more.Text = moreText; };
			strip.GuiInput += ev =>
			{
				// Down leaves the counter for the next stop, as Tab does (the counter is the last thing on the page)
				if (ev.IsActionPressed("ui_down"))
				{
					strip.FindNextValidFocus()?.GrabFocus();
					strip.AcceptEvent();
					return;
				}
				var step = ev.IsActionPressed("ui_right") ? 1 : ev.IsActionPressed("ui_left") ? -1 : 0;
				if (step == 0) return;
				Mark(sel, false);
				sel = (sel + step + tags.Count) % tags.Count;
				Mark(sel, true);
				strip.AcceptEvent();
			};
		}
		// nobody close to the end yet: he waits at the far end of the counter, faintly
		if (atRisk == null && AddFigure(strip, "wc_death", start + shown.Count * slot + 6, 230, 0.2f) is { } waiting)
		{
			Anim.Sway(waiting, 0.8f, 5.5f);
			waiting.MouseFilter = _drawer == null ? MouseFilterEnum.Pass : MouseFilterEnum.Ignore;
			waiting.TooltipText = "Death, at the far end of the counter. Nobody is near enough for him yet; when someone falls poorly he comes and stands beside them.";
		}
		return strip;
	}

	/// <summary>FNV-1a: the same id gives the same figure on every launch.</summary>
	private static uint StableHash(string s)
	{
		var h = 2166136261u;
		foreach (var ch in s) { h ^= ch; h *= 16777619u; }
		return h;
	}

	private const float CounterTop = 226f;
	private const int BenchWidth = 372;

	private static TextureRect? AddFigure(Control parent, string cut, float x, float height, float alpha, bool flip = false)
	{
		var tex = Print.Cut(cut);
		if (tex == null) return null;
		var w = height * tex.GetWidth() / tex.GetHeight();
		var rect = new TextureRect
		{
			Texture = tex,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			Position = new Vector2(x, CounterTop - height + 12),
			Size = new Vector2(w, height),
			Modulate = new Color(1, 1, 1, alpha),
			FlipH = flip,
			MouseFilter = MouseFilterEnum.Ignore,
		};
		parent.AddChild(rect);
		return rect;
	}

	// the variant is fixed by the person's name, which the paper's death and inquest records share
	private static string CutFor(Regular r) => Cuts.For(r.Segment, !r.Name.StartsWith("Mr."), r.Name);

	/// <summary>Which stock cut the composing room uses for a customer.</summary>
	public static string CutFor(string segment, bool female) => segment switch
	{
		"mother" => "wc_mother",
		"labourer" or "dockworker" => "wc_longshoreman",
		"seamstress" => female ? "wc_seamstress" : "wc_clerk",
		"clerk" => female ? "wc_seamstress" : "wc_clerk",
		"elderly" => female ? "wc_widow" : "wc_oldman",
		// the proprietor's own likeness is never a customer
		"well_to_do" => female ? "wc_lady" : "wc_oldman",
		_ => female ? "wc_widow" : "wc_oldman",
	};

	private static Control Tag(Regular r)
	{
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", -1);
		var name = Print.Label(r.Name, Print.Rye, 12);
		name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		v.AddChild(name);
		v.AddChild(Print.Label((r.Infant ? "for " : "") + r.Ailment.ToLowerInvariant(), Print.Fell, 12, wrap: false));
		var status = new List<string>
		{
			(r.Infant ? "infant " : "") + r.Looks switch { "gravely_ill" => "gravely ill", "poorly" => "poorly", "peaky" => "peaky", _ => "hale" },
		};
		if (r.Ruined) status.Add("ruined by it");
		else if (r.Withdrawal) status.Add("shaking");
		else if (r.Craving) status.Add("asks for it by name");
		else if (r.MonthsAsCustomer == 0) status.Add("new");
		else status.Add(r.Mood);
		v.AddChild(Print.Label(string.Join(" · ", status), Print.FellIt, 12, wrap: false));
		return v;
	}

	// ---------- the composing bench ----------

	private Control TypeCase(DeskModel m)
	{
		var v = SlipBody("THE TYPE CASE");
		var product = m.Formulas.FirstOrDefault(x => x.Id == _adFormula) ?? m.Formulas.FirstOrDefault();
		if (product == null)
		{
			v.AddChild(Print.Body("[i]No remedy yet: open the formula book.[/i]", 13, justify: false));
			return Print.Slip(v, 10);
		}
		v.AddChild(Print.Label("SPACE IN THE GAZETTE", Print.Rye, 12));
		var sizes = new HBoxContainer();
		sizes.AddThemeConstantOverride("separation", 4);
		var tier = AdSpace.TierFor(m.AdSpendCents);
		for (var i = 0; i < AdSpace.Tiers.Length; i++)
		{
			var t = AdSpace.Tiers[i];
			var b = Print.Button(t.Short, 11, solid: m.CanAdvertise && i == tier);
			b.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			b.TooltipText = t.Cents == 0 ? "No notice in the Gazette" : $"{t.Name}: {EditionWriter.Money(t.Cents)} a month";
			Print.SetLive(b, m.CanAdvertise);
			var cents = t.Cents;
			b.Pressed += () => Act(_game.SetAdSpend(cents));
			sizes.AddChild(b);
		}
		v.AddChild(sizes);
		// the rates, printed rather than hidden in the tooltips
		v.AddChild(Print.Label("Rates: " + string.Join(" · ", AdSpace.Tiers.Where(t => t.Cents > 0).Select(t => $"{t.Short.ToLowerInvariant()} {EditionWriter.Money(t.Cents)}")) + " a month.", Print.FellIt, 12));
		var adsNext = m.NextUnlocks.FirstOrDefault(u => u.Name.Contains("Advertis"));
		if (!m.CanAdvertise) v.AddChild(Print.Body(adsNext.Name != null && adsNext.Progress >= 0.999
			? "[i]Both conditions are met: space can be bought from next month.[/i]"
			: "[i]Not yet: the Gazette wants $100 in savings and a printed testimonial first.[/i]", 12, justify: false));
		// a second remedy has its own label: choose which one the claims are set for
		if (m.Formulas.Count > 1)
		{
			var tabs = new HFlowContainer();
			tabs.AddThemeConstantOverride("h_separation", 4);
			foreach (var g in m.Formulas)
			{
				var t = Print.Button(g.Name.ToUpperInvariant(), 10, solid: g.Id == product.Id);
				var gid = g.Id;
				t.TooltipText = $"Set the claims on the label of {g.Name}.";
				t.Pressed += () => { _adFormula = gid; Refresh(); };
				tabs.AddChild(t);
			}
			v.AddChild(tabs);
		}
		v.AddChild(Print.Body($"[i]Claims for {Print.Escape(product.Name)}, up to four. Bolder type sells more, and the coroner reads it too.[/i]", 12, justify: false));
		var flow = new HFlowContainer();
		flow.AddThemeConstantOverride("h_separation", 4);
		flow.AddThemeConstantOverride("v_separation", 4);
		foreach (var (id, name) in m.Claims)
		{
			var on = product.ClaimIds.Contains(id);
			var bold = m.ClaimBoldness.GetValueOrDefault(id);
			var weight = bold >= 3 ? " !!!" : bold >= 1.5 ? " !!" : bold >= 1 ? " !" : "";
			// long claims wrap onto a second line of the slug rather than being cut short
			var b = Print.Button(name.ToUpperInvariant() + weight, 11, solid: on, sound: "slug");
			if (name.Length > 26)
			{
				b.AutowrapMode = TextServer.AutowrapMode.WordSmart;
				b.CustomMinimumSize = new Vector2(200, 0);
			}
			b.TooltipText = name + (on ? "\nClick to strike it out." : "\nClick to set it in type.");
			var claim = id;
			b.Pressed += () =>
			{
				var next = on ? product.ClaimIds.Where(c => c != claim).ToList() : product.ClaimIds.Append(claim).ToList();
				Act(_game.SetClaims(product.Id, next));
			};
			flow.AddChild(b);
		}
		v.AddChild(flow);
		if (_note != null)
		{
			var why = _note.Contains("At most", StringComparison.Ordinal) ? _note + " Strike one out to make room." : _note;
			v.AddChild(Print.Body($"[b]Refused.[/b] [i]{Print.Escape(why)}[/i]", 12, justify: false));
		}
		return Print.Slip(v, 10);
	}

	private Control LettersSlip(DeskModel m)
	{
		var v = SlipBody(m.Letters.Count == 0 ? "NO LETTERS" : m.Letters.Count == 1 ? "ONE LETTER" : $"{Spell(m.Letters.Count).ToUpperInvariant()} LETTERS");
		if (m.Letters.Count == 0) v.AddChild(Print.Body("[i]Nothing in the post but a circular for galvanic belts.[/i]", 13, justify: false));
		foreach (var l in m.Letters.Take(1))
		{
			// one line, cut short if need be: the whole post is in the drawer
			var title = EditionWriter.Set(l.Title);
			if (title.Length > 40) title = title[..38].TrimEnd(' ', ',') + "…";
			var line = Print.Label(title + (m.Letters.Count > 1 ? $"; and {Spell(m.Letters.Count - 1)} more." : "."), Print.Fell, 13);
			line.TooltipText = string.Join("\n", m.Letters.Select(x => x.Title));
			line.MouseFilter = MouseFilterEnum.Pass;
			v.AddChild(line);
		}
		if (m.Letters.Count > 0)
		{
			var open = Print.Button("OPEN THE LETTERS", 13, solid: true);
			open.Pressed += () => OpenDrawer("letters");
			v.AddChild(open);
		}
		return Print.Slip(v, 10);
	}

	private Control WorksSlip(DeskModel m)
	{
		var v = SlipBody("THE WORKS");
		var grid = new HBoxContainer();
		grid.AddThemeConstantOverride("separation", 14);
		var left = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		left.AddThemeConstantOverride("separation", 0);
		var stock = m.Formulas.Sum(f => f.Stock);
		var brewing = m.Brewing.Sum(b => b.Bottles);
		left.AddChild(Print.Label($"{stock} bottle{(stock == 1 ? "" : "s")} on the shelf", Print.Fell, 14));
		left.AddChild(Print.Label(brewing > 0 ? $"{brewing} brewing" : m.StillsFree > 0 ? "the still is cold" : "the still is busy", Print.FellIt, 13));
		grid.AddChild(left);
		var books = new VBoxContainer();
		books.AddThemeConstantOverride("separation", -2);
		books.AddChild(Print.Label(EditionWriter.Money(m.CashCents), Print.Rye, 22, HorizontalAlignment.Right, wrap: false));
		var talk = Print.Label($"suspicion: {DeskView.Talk(m.Heat)} ({m.Heat:0} of 100)", Print.FellIt, 13, HorizontalAlignment.Right, wrap: false);
		talk.TooltipText = "What the ward says about your bottles. The more it talks, the more inquests the coroner calls; at 100 the Board of Health moves to close the shop.";
		talk.MouseFilter = MouseFilterEnum.Pass;
		books.AddChild(talk);
		// standing is moved by letters and deaths: show it, not only suspicion
		var standing = Print.Label($"standing in the ward: {m.Reputation:0}", Print.FellIt, 13, HorizontalAlignment.Right, wrap: false);
		standing.TooltipText = "What the ward thinks of the shop. Good standing brings callers and keeps regulars; letters and deaths move it.";
		standing.MouseFilter = MouseFilterEnum.Pass;
		books.AddChild(standing);
		grid.AddChild(books);
		v.AddChild(grid);
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 4);
		// the house's long game sits beside the works: the prospectus of improvements
		// something new behind a drawer (a still, a venture on offer) is printed in reverse until looked at
		var stills = m.Stills > 0 ? m.Stills : m.StillsFree + m.Brewing.Count;
		if (_seenStills == 0) _seenStills = stills;
		var offers = m.Investments.Count(i => !i.Held && i.CanBuy);
		var fresh = new Dictionary<string, bool>
		{
			["still"] = stills > _seenStills && _seenStills > 0,
			["ventures"] = offers > _seenOffers,
		};
		foreach (var (key, label) in new[] { ("still", "THE STILL"), ("formula", "FORMULA"), ("supplies", "SUPPLIES"), ("ventures", "VENTURES") })
		{
			var isNew = fresh.GetValueOrDefault(key);
			var b = Print.Button(label, 11, solid: isNew);
			if (isNew) b.TooltipText = key == "still" ? "New: another still is yours." : "New: a venture is on offer.";
			b.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			var k = key;
			b.Pressed += () =>
			{
				if (k == "still") _seenStills = stills;
				if (k == "ventures") _seenOffers = offers;
				OpenDrawer(k);
			};
			row.AddChild(b);
		}
		v.AddChild(row);
		return Print.Slip(v, 10);
	}

	private bool _armed;

	private Control Lever(bool nothingToSell)
	{
		var b = Print.Button("SEND TO PRESS ☞", 28, solid: true, sound: null);
		b.CustomMinimumSize = new Vector2(0, 70);
		b.TooltipText = "End the month. The lead story is written by what happens.";
		b.AddThemeStyleboxOverride("normal", Print.Box(Print.Ink, Print.Paper, 3, 10));
		b.AddThemeStyleboxOverride("hover", Print.Box(Print.Paper, Print.Paper, 3, 10));
		b.AddThemeColorOverride("font_hover_color", Print.Ink);
		// focus: a paper rule set inside the border, plainly visible on the black lever
		var ring = Print.Box(new Color(0, 0, 0, 0), Print.Paper, 3, 10);
		ring.ExpandMarginLeft = ring.ExpandMarginRight = ring.ExpandMarginTop = ring.ExpandMarginBottom = -7;
		b.AddThemeStyleboxOverride("focus", ring);
		b.Pressed += () =>
		{
			// an empty month, or an unanswered coroner, asks once: press again to send anyway
			if (nothingToSell && !_armed) { _armed = true; b.Text = "SEND IT ANYWAY ☞"; Sfx.Play("ticket", 0f, 0.02f, 0.7f); return; }
			_armed = false;
			PressRequested?.Invoke();
		};
		_lever = b;
		return b;
	}

	// ---------- drawers: a tool laid over the proof ----------

	private Control Drawer(string which)
	{
		var shade = new ColorRect { Color = new Color(0, 0, 0, 0.3f) };
		shade.SetAnchorsPreset(LayoutPreset.FullRect);
		var holder = new VBoxContainer();
		holder.SetAnchorsPreset(LayoutPreset.FullRect);
		_holder = holder;
		void Fit()
		{
			if (!IsInstanceValid(holder)) return;
			holder.SetAnchorsPreset(LayoutPreset.FullRect);
			holder.OffsetLeft = 14; holder.OffsetRight = -14; holder.OffsetTop = 6; holder.OffsetBottom = -6;
		}
		Fit();
		// an anchored box keeps any size its contents once demanded (text laid out before it had a
		// width asks for a great deal); put it back to the drawer's size whenever that settles
		holder.MinimumSizeChanged += () => Callable.From(Fit).CallDeferred();
		holder.Ready += () => Callable.From(Fit).CallDeferred();
		var close = Print.Button("PUT IT DOWN", 14, solid: true);
		close.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
		close.AddThemeStyleboxOverride("normal", Print.Box(Print.Ink, Print.Paper, 2, 6));
		close.Pressed += () => CloseDrawer();
		close.TooltipText = "Close (Escape)";
		holder.AddChild(close);
		if (which == "formula")
		{
			// the bench keeps the glass as poured while the proof around it refreshes
			if (_bench == null)
			{
				_bench = new FormulaBench(_game) { SizeFlagsVertical = SizeFlags.ExpandFill };
				_bench.Changed += Rebuild;
			}
			_bench.GetParent()?.RemoveChild(_bench);
			// a scroll frame holds the bench at the drawer's size, whatever its text asks for
			var frame = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true };
			Print.QuietScrollbar(frame);
			_bench.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			frame.AddChild(_bench);
			holder.AddChild(frame);
		}
		else
		{
			// each tool is printed matter of its own: the post, the wholesaler's order form, the still
			if (_tool == null || _toolKind != which)
			{
				_tool?.QueueFree();
				switch (which)
				{
					case "letters": { var t = new LettersDesk(_game); t.Changed += Rebuild; _tool = t; break; }
					case "still": { var t = new StillBench(_game); t.Changed += Rebuild; _tool = t; break; }
					case "ventures": { var t = new Prospectus(_game); t.Changed += Rebuild; _tool = t; break; }
					default: { var t = new OrderForm(_game); t.Changed += Rebuild; _tool = t; break; }
				}
				_toolKind = which;
				_tool.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			}
			_tool.GetParent()?.RemoveChild(_tool);
			var frame = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true };
			Print.QuietScrollbar(frame);
			frame.AddChild(_tool);
			holder.AddChild(frame);
		}
		shade.AddChild(holder);
		Anim.SlideIn(holder, new Vector2(0, 60), 0.24f);
		if (_drawerOpened)
		{
			_drawerOpened = false;
			Sfx.Play("slip");
			// the keyboard goes into the drawer: its first control after CLOSE, never back to the bench
			Callable.From(() =>
			{
				if (!IsInstanceValid(holder)) return;
				var inside = Print.Focusables(holder).Where(c => c != close && !(c is BaseButton { Disabled: true })).ToList();
				(inside.FirstOrDefault() ?? close).GrabFocus();
			}).CallDeferred();
			_skipRestore = true;
		}
		shade.GuiInput += e => { if (e is InputEventMouseButton { Pressed: true } mb && mb.ButtonIndex == MouseButton.Left && !holder.GetGlobalRect().HasPoint(mb.GlobalPosition)) { _drawer = null; DropBench(); CallDeferred(nameof(Refresh)); } };
		return shade;
	}

	// ---------- helpers ----------

	private static StyleBoxFlat Sheet()
	{
		var s = Print.Box(Print.Paper, new Color(0, 0, 0, 0), 0, 0);
		s.ContentMarginLeft = s.ContentMarginRight = 22;
		s.ContentMarginTop = 10;
		s.ContentMarginBottom = 14;
		s.ShadowColor = new Color(0, 0, 0, 0.6f);
		s.ShadowSize = 14;
		return s;
	}

	private static VBoxContainer Col(int width, bool expand = false)
	{
		var v = new VBoxContainer { CustomMinimumSize = new Vector2(width, 0) };
		if (expand) v.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		v.AddThemeConstantOverride("separation", 5);
		var m = new MarginContainer();
		return v;
	}

	private static Control VRule()
	{
		var m = new MarginContainer { CustomMinimumSize = new Vector2(22, 0) };
		m.AddChild(new ColorRect { Color = Print.Ink, CustomMinimumSize = new Vector2(1.4f, 0), SizeFlagsHorizontal = SizeFlags.ShrinkCenter });
		return m;
	}

	private static Control Head(string s)
	{
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 1);
		v.AddChild(Print.Label(s, Print.Rye, 15, HorizontalAlignment.Center));
		v.AddChild(Print.Dash(60));
		return v;
	}

	/// <summary>Last month's lines, struck through, with the proofreader's "dele" in the margin.</summary>
	private static void Struck(VBoxContainer col, IEnumerable<string> lines, string none)
	{
		var all = lines.ToList();
		var list = all.Take(3).ToList();
		if (list.Count == 0) { col.AddChild(Print.Label(none, Print.FellIt, 12, HorizontalAlignment.Center, new Color(Print.Ink, 0.75f))); return; }
		if (all.Count > 3) list[^1] += $" And {Spell(all.Count - 3)} more.";
		foreach (var l in list)
			col.AddChild(Print.Body($"[s]{Print.Escape(EditionWriter.Set(l))}[/s] [i]dele (strike out)[/i]", 12, new Color(Print.Ink, 0.82f), justify: false));
	}

	private static string SurnameOf(string name)
	{
		var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return parts.Length == 0 ? "" : (parts.Length >= 3 && parts[^2] is "Van" or "De" ? parts[^2] + " " + parts[^1] : parts[^1]).ToUpperInvariant();
	}

	private static Control Ghost(float width)
	{
		var h = new HBoxContainer();
		h.AddChild(new ColorRect { Color = new Color(Print.Ink, 0.12f), CustomMinimumSize = new Vector2(170 * width, 8) });
		return h;
	}

	private static Control SmallAd(string head, string text)
	{
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 0);
		v.AddChild(Print.Label(head, Print.Rye, 13, HorizontalAlignment.Center));
		v.AddChild(Print.Label(text, Print.FellIt, 12, HorizontalAlignment.Center));
		var inner = Print.Panel(v, Print.Box(new Color(0, 0, 0, 0), Print.Ink, 1, 5));
		return Print.Panel(inner, Print.Box(new Color(0, 0, 0, 0), Print.Ink, 3, 2));
	}

	private static VBoxContainer SlipBody(string title)
	{
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 5);
		v.AddChild(Print.Label(title, Print.Rye, 17, HorizontalAlignment.Center));
		v.AddChild(Print.Rule(2.5f, 1, 2));
		return v;
	}
}
