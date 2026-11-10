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
		var opener = was switch { "still" => "THE STILL", "formula" => "FORMULA", "supplies" => "SUPPLIES", "ventures" => "VENTURES", "books" => "BOOKS", "letters" => "OPEN THE LETTERS", "regulars" => AllRegularsText, _ => "" };
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

	// the proof is laid out for the text size: a change in the settings sets it again at once
	public override void _EnterTree() => LargePrint.Changed += OnTextSize;
	public override void _ExitTree() => LargePrint.Changed -= OnTextSize;

	private void OnTextSize()
	{
		if (_game == null || !IsInsideTree()) return;
		DropBench();
		Rebuild();
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
	/// <summary>The regular Death stands beside, for the warning that says why.</summary>
	private Regular? _deathRegular;

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
		// the proof, the counter and the bench are laid out for every text size: their type is set
		// at the size asked for and never shrunk to fit, and what runs long there scrolls
		LargePrint.NoFit(left);
		left.SetAnchorsPreset(LayoutPreset.FullRect);
		left.AddThemeConstantOverride("separation", 12);
		leftStack.AddChild(left);
		// the counter first: it decides whom Death stands beside, and the notes on the proof say why
		var counterStrip = Counter(m);
		var warn = Warnings(m, out var nothing, out var unansweredInquests);
		left.AddChild(ProofPage(m, warn));
		left.AddChild(counterStrip);

		var bench = new VBoxContainer { CustomMinimumSize = new Vector2(BenchWidth, 0) };
		LargePrint.NoFit(bench);
		bench.AddThemeConstantOverride("separation", 10);
		root.AddChild(bench);
		// the type case takes what height is left, and scrolls under its own title; everything under
		// it is of fixed height for the text size, stacked from the foot of the bench: the post, the
		// drawer tickets and the lever stand in the same place at every size and in every month
		bench.AddChild(TypeCase(m));
		bench.AddChild(WorksSlip(m));
		var post = LettersSlip(m);
		post.CustomMinimumSize = new Vector2(0, PostHeight);
		bench.AddChild(post);
		bench.AddChild(DrawerRow(m));
		bench.AddChild(Lever(nothing || unansweredInquests > 0));

		if (_drawer != null)
		{
			var drawer = Drawer(_drawer);
			// at the larger sizes the formula book is laid across the whole table: its shelf, glass,
			// assay and price need the width to be read without cutting a label short
			if (_drawer == "formula" && LargePrint.Level > 0)
			{
				drawer.SetAnchorsPreset(LayoutPreset.FullRect);
				drawer.OffsetLeft = 22; drawer.OffsetRight = -22; drawer.OffsetTop = 18; drawer.OffsetBottom = -14;
				AddChild(drawer);
			}
			else leftStack.AddChild(drawer);
			// while a drawer is open the bench is out of reach of the mouse as well as the keyboard
			// (dimmed only a little: the cash and the suspicion stay readable while a letter asks for money)
			bench.Modulate = new Color(1, 1, 1, 0.88f);
			void Lock(Node n) { foreach (var k in n.GetChildren()) { if (k is Control c) c.MouseFilter = MouseFilterEnum.Ignore; Lock(k); } }
			Lock(bench);
		}
		if (!_skipRestore) Print.RestoreFocus(this, keep);
		_skipRestore = false;
		Changed?.Invoke();
	}


	/// <summary>
	/// A tooltip left standing over a list that has scrolled under it is put away: the pointer is
	/// "moved" where it already is, so the tooltip follows whatever is under it now.
	/// </summary>
	private void Untip()
	{
		if (!IsInsideTree()) return;
		var at = GetViewport().GetMousePosition();
		Input.ParseInputEvent(new InputEventMouseMotion { Position = at, GlobalPosition = at });
	}

	/// <summary>The compositor's notes under the case, most urgent first.</summary>
	private List<string> Warnings(DeskModel m, out bool nothing, out int unansweredInquests)
	{
		var warn = new List<string>();
		unansweredInquests = m.Letters.Count(l => l.Title.StartsWith("Inquest", StringComparison.Ordinal));
		if (unansweredInquests > 0)
		{
			// say before the reply, not after it, how far a verdict moves the meters: a jury sits at
			// once when its letter is answered, and four of them can double suspicion in a click
			var n = unansweredInquests;
			var worst = Math.Min(100, m.Heat + n * InquestHeat);
			var each = n == 1 ? "If the jury names the bottle" : "Each jury that names the bottle";
			var all = n == 1 ? "it would then stand at" : n == 2 ? "if both name it, it will stand at" : $"if all {Spell(n)} name it, it will stand at";
			warn.Add($"{Cap(Spell(n))} inquest{(n == 1 ? "" : "s")} unanswered: the coroner will proceed without you. {each}, suspicion rises {InquestHeat} (it is {m.Heat:0} now) and the good name falls {InquestGoodName}; {all} {worst:0} of 100{(worst >= 100 ? ", and at 100 the Board of Health closes the shop" : "")}. The letters print each reply's odds.");
		}
		else if (m.Letters.Count > 0) warn.Add($"{Cap(Spell(m.Letters.Count))} letter{(m.Letters.Count == 1 ? "" : "s")} unanswered: the usual reply will go out for you.");
		// every regular at real risk, not only the one he stands beside
		var frail = m.Regulars.Where(r => r.Looks is "gravely_ill" or "poorly").OrderByDescending(Risk).ToList();
		if (_deathRegular is { } dr)
		{
			// say why he stands there: what the frail customer looks like and how much they take
			var how = dr.Looks == "gravely_ill" ? "gravely ill" : "poorly";
			var took = dr.BottlesLastMonth > 0 ? $", {Spell(dr.BottlesLastMonth).ToLowerInvariant()} bottle{(dr.BottlesLastMonth == 1 ? "" : "s")} last month" : "";
			var habit = dr.Withdrawal ? ", in want of the bottle" : dr.Craving ? ", craving it" : "";
			var others = frail.Where(r => r != dr).ToList();
			var rest = others.Count == 0 ? "" : others.Count <= 2
				? $" Also at risk: {string.Join(" and ", others.Select(r => r.Name))}."
				: $" {Cap(Spell(others.Count))} more are poorly or gravely ill, and at risk as well.";
			var opiate = m.Formulas.Any(f => f.Components.Any(c => c.Ounces > 0 && m.Ingredients.FirstOrDefault(i => i.Id == c.IngredientId)?.DrugClass is "opiate" or "cocaine"));
			warn.Add($"Death waits beside {dr.Name}: {how}{took}{habit}.{rest} {(opiate ? "Each strong bottle a frail customer takes may be the last; a milder recipe is safer." : "Any regular may die of age or illness, whatever the bottle; the frail most of all.")}");
		}
		nothing = m.Formulas.Sum(f => f.Stock) == 0 && m.Brewing.Count == 0;
		// the first month: say where to begin, in so many words
		if (m.Formulas.Count == 0) warn.Add("Begin with the FORMULA book: pour a remedy and enter it. Then light THE STILL, and send the paper to press.");
		else if (nothing) warn.Add("Nothing on the shelf and nothing on the still: light THE STILL, or the shop will sell nothing this month.");
		else if (LowStock(m) is { } low) warn.Add(low);
		// what the shop earned last month and was not told about: a still, a percolator, the Gazette's space
		foreach (var (id, title, _) in _game.Facts.Unlocks)
			if (NewThing(id, title) is { } news) warn.Add(news);
		// a shelf that keeps ending bare is money left on the counter, and orders left unfilled
		if (m.ShelfBareMonths >= 2 && !nothing)
			warn.Add($"The shelf ended bare in {Spell(m.ShelfBareMonths)} of the last three months: brew more, or look in THE STILL for what the next still waits on.");
		// space the shop cannot pay for is not printed: say so before the press, not after
		if (m.CanAdvertise && m.AdSpendCents > m.CashCents)
			warn.Add($"The notice costs {EditionWriter.Money(m.AdSpendCents)} and you have {EditionWriter.Money(m.CashCents)}: the Gazette will not print it unpaid.");
		return warn;
	}

	private static string Cap(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

	/// <summary>
	/// The shelf will not last the month: what is on it and on the fire is fewer bottles than last
	/// month sold (or no more, when last month sold the shelf out and could have sold more).
	/// </summary>
	private string? LowStock(DeskModel m)
	{
		var f = _game.Facts;
		if (f.FirstEdition) return null;
		var sold = f.Sales.Sum(s => s.Bottles);
		if (sold <= 0) return null;
		var stock = m.Formulas.Sum(x => x.Stock);
		var brewing = m.Brewing.Sum(b => b.Bottles);
		var have = stock + brewing;
		var want = sold + Math.Max(2, sold / 10);
		if (have >= want) return null;
		var on = brewing > 0 ? $"{Spell(stock)} on the shelf and {Spell(brewing)} on the fire" : $"{Spell(stock)} bottle{(stock == 1 ? "" : "s")} on the shelf and nothing on the fire";
		return $"Low stock: {on}, and last month sold {Spell(sold)}. {(m.StillsFree > 0 ? "Light THE STILL" : "Every still is busy; brew more next month")}, or the shelf will be bare before the month is out.";
	}

	/// <summary>What a newly earned thing is, and where it is, the month it arrives.</summary>
	private static string? NewThing(string id, string title) => id switch
	{
		"second_still" => "New: a second still is yours, earned by two hundred bottles sold. Two batches a month now: light both in THE STILL.",
		"third_still" => "New: a third still is yours, earned by six hundred bottles sold. THE STILL can light all three at once.",
		"percolator" => "New: a glass percolator, earned by running ten batches. PERCOLATION is now in the FORMULA book: a stronger draw, at 2c. more a bottle. Nothing was spent.",
		"newspaper_ads" => "New: the Gazette will sell you space. Choose a size in THE TYPE CASE; it is paid at press.",
		"bulk_supplier" => "New: a wholesale supplier on Pearl street, fifteen per cent off every order.",
		"quality_control" => "New: an assistant tests each batch, and pours away a bad one before it reaches a customer.",
		"coroner_fees" => null,
		_ => title.Length > 0 ? $"New: {title}." : null,
	};

	// ---------- the proof page ----------

	/// <summary>The proof's columns, by text size: the deaths and the ward's talk, and your space.</summary>
	private static float LeftWidth => LargePrint.Pick(186f, 206f, 230f);
	private static float RightWidth => LargePrint.Pick(214f, 176f, 0f);

	private Control ProofPage(DeskModel m, List<string> notes)
	{
		var page = new VBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		page.AddThemeConstantOverride("separation", LargePrint.Pick(4, 3, 2));
		var date = EditionWriter.PublicationDate(m.Year, m.Month);
		var day = $"PROOF FOR {date.DayOfWeek.ToString().ToUpperInvariant()}, {date.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture).ToUpperInvariant()}";
		// at the larger sizes the compositor's instruction is left off, so the date is never cut short
		var head = Print.Label(LargePrint.Level == 0 ? day + " · CORRECT AND RETURN TO THE COMPOSING ROOM" : day, Print.FellIt, 12, HorizontalAlignment.Center, wrap: false);
		head.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		page.AddChild(head);
		// the masthead gives up a little of its height to the larger type below it
		page.AddChild(Print.Label("THE BOWERY GAZETTE", Print.Rye, LargePrint.Pick(40, 32, 28), HorizontalAlignment.Center));
		page.AddChild(Print.Rule(4, 1.4f, 2));

		// three columns take whatever height is left and never push the counter down; each scrolls
		// when its matter runs long, and says so at its foot, so no line is cut off or shrunk
		var cols = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		cols.AddThemeConstantOverride("separation", 0);
		page.AddChild(cols);

		// left column: last month's deaths and what moved suspicion; this month's set after press
		var lc = new VBoxContainer();
		lc.AddThemeConstantOverride("separation", 5);
		lc.AddChild(Head("DEATHS."));
		lc.AddChild(Pencil("set after press"));
		// last month's matter, struck out in proof marks, waits for this month's type
		var last = _game.Facts;
		Struck(lc, last.Deaths.Select(d => d.Anonymous ? "UNKNOWN.—A body, unclaimed." : d.Infant ? $"{SurnameOf(d.Name)}.—The infant of {d.Name}." : $"{SurnameOf(d.Name)}.—{d.Name}."), "No deaths last month.",
			last.Deaths.Select(d => BottleNote(d, last, m)).ToList());
		// what moved suspicion last month, cause by cause, as the books have it
		lc.AddChild(Head("SUSPICION."));
		foreach (var line in SuspicionLines(last)) lc.AddChild(Print.Label(line, Print.Fell, 12));
		lc.AddChild(Head("CORONER'S COURT."));
		var inquests = m.Letters.Where(l => l.Title.StartsWith("Inquest", StringComparison.Ordinal)).ToList();
		if (inquests.Count == 0) lc.AddChild(Pencil("set after press"));
		foreach (var q in inquests.Take(2))
			lc.AddChild(Print.Body($"{Print.Escape(q.Title)} sits this month. [i]See your letters.[/i]", 12, justify: false));
		if (inquests.Count > 2) lc.AddChild(Print.Body($"[i]And {inquests.Count - 2} more: see your letters.[/i]", 12, justify: false));
		lc.AddChild(Head("PERSONAL."));
		Struck(lc, last.Joined.Select(p => $"{p.Name}, a new customer.").Concat(last.Left.Select(l => $"{l.Person.Name}, gone from the shop.")), "Nothing personal last month.");
		cols.AddChild(Column(lc, "left", LeftWidth));
		cols.AddChild(VRule());

		// centre: the lead story, blank until press (or your notice, if you bought the front page),
		// with the compositor's notes pencilled in it; and the month so far in a box of its own
		var centre = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
		centre.AddThemeConstantOverride("separation", 6);
		var tier = m.CanAdvertise ? AdSpace.TierFor(m.AdSpendCents) : 0;
		var product = m.Formulas.FirstOrDefault();
		var pad = notes.Count > 0 ? LargePrint.Pick(10, 8, 7) : LargePrint.Pick(14, 9, 7);
		var lead = new Dashed(pad) { SizeFlagsVertical = SizeFlags.ExpandFill, Ink = new Color(Print.Ink, 0.4f) };
		var lv = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		lv.AddThemeConstantOverride("separation", 6);
		var soFar = MonthBook.SoFar(_game, m);
		if (tier == AdSpace.Front && product != null)
		{
			lead.Accent = Print.Green;
			lv.AddChild(Print.Label("YOUR NOTICE, SET AS NEWS", Print.Rye, 13, HorizontalAlignment.Center));
			lv.AddChild(Print.Label((product.ClaimIds.Count > 0 ? m.Claims.First(c => c.Id == product.ClaimIds[0]).Name : "Never Felt Better").ToUpperInvariant() + "!", Print.Rye, LargePrint.Pick(30, 24, 22), HorizontalAlignment.Center));
			lv.AddChild(Print.Label("A Remarkable Restoration.", Print.FellIt, 18, HorizontalAlignment.Center));
			if (notes.Count == 0) lv.AddChild(Print.Body("[i]The Gazette will find a satisfied customer to quote, and end the column with a very small[/i] Adv.", 13, centre: true));
		}
		else
		{
			lv.AddChild(Print.Label("THE LEAD STORY", Print.Rye, notes.Count > 0 ? 17 : 22, HorizontalAlignment.Center, new Color(Print.Ink, 0.72f)));
			// with notes to read, the editor's line gives them its room
			if (notes.Count == 0)
				lv.AddChild(Print.Body("[i]The editor sets it when the month is out, from whatever happens in the ward.[/i]", 13, new Color(Print.Ink, 0.9f), centre: true));
		}
		// the compositor's notes to the proprietor, in the margin of the proof: what wants doing
		if (notes.Count > 0)
		{
			var nv = new VBoxContainer();
			nv.AddThemeConstantOverride("separation", 2);
			nv.AddChild(Print.Label("NOTES FOR THE PROPRIETOR", Print.Rye, LargePrint.Pick(13, 15, 16), HorizontalAlignment.Center));
			nv.AddChild(Print.Dash(60));
			foreach (var w in notes.Take(6)) nv.AddChild(Print.Label("☞ " + w, Print.FellIt, 13));
			lv.AddChild(nv);
			lv.Alignment = BoxContainer.AlignmentMode.Begin;
		}
		// with nothing to note or done, the box holds the editor's ghost of the month's dead
		var ghostDeath = Print.Cut("wc_death");
		if (ghostDeath != null && notes.Count == 0 && soFar.Count == 0)
			lv.AddChild(new TextureRect { Texture = ghostDeath, ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, CustomMinimumSize = new Vector2(0, LargePrint.Pick(120, 90, 70)), Modulate = new Color(1, 1, 1, 0.16f) });
		lead.AddChild(Column(lv, "lead", 0, expand: true));
		centre.AddChild(lead);
		centre.AddChild(MonthSoFar(soFar, pad));
		cols.AddChild(centre);

		// right column: your space, and the competition's. At the larger sizes the rivals' notices
		// give way to larger type, and at LARGER the column itself does: the type case on the bench
		// shows the space bought and the claims set, and why space cannot be bought yet
		if (RightWidth > 0)
		{
			cols.AddChild(VRule());
			var rc = new VBoxContainer();
			rc.AddThemeConstantOverride("separation", 5);
			rc.AddChild(YourSpace(m, tier, product));
			if (LargePrint.Level == 0) rc.AddChild(SmallAd("DR. GRIMSBY'S GALVANIC BELT", "Restores Lost Vigor!"));
			if (LargePrint.Level == 0 && (tier < 2 || product == null)) rc.AddChild(SmallAd("MRS. WHITTLE'S COMPOUND", "She answers every letter."));
			cols.AddChild(Column(rc, "right", RightWidth));
		}

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

	/// <summary>
	/// THE MONTH SO FAR, for a proprietor back at the bench: what has been done since the last
	/// edition. It has a box of its own under the lead story, as tall as it needs up to a third of
	/// the column (then it scrolls), so the notes above it can never push it out of sight.
	/// </summary>
	private Control MonthSoFar(List<string> soFar, int pad)
	{
		var box = new Dashed(pad) { Ink = new Color(Print.Ink, 0.4f) };
		var hook = new VBoxContainer();
		hook.AddThemeConstantOverride("separation", 0);
		var title = Print.Label("THE MONTH SO FAR", Print.Rye, LargePrint.Pick(13, 15, 16), HorizontalAlignment.Center);
		hook.AddChild(title);
		hook.AddChild(Print.Dash(60));
		var lines = soFar.Count == 0 ? new List<string> { "Nothing done yet this month." } : soFar.Select(l => "· " + l).ToList();
		foreach (var line in lines) hook.AddChild(Print.Label(line, soFar.Count == 0 ? Print.FellIt : Print.Fell, 13, soFar.Count == 0 ? HorizontalAlignment.Center : HorizontalAlignment.Left));
		// as tall as its lines, measured at the width they will be set in, up to the cap
		var px = LargePrint.Size(13);
		var width = 800f - LeftWidth - RightWidth - (RightWidth > 0 ? 44f : 22f) - 4 * pad - 10f;
		var need = Print.Rye.GetHeight(LargePrint.Pick(13, 15, 16)) + 6;
		foreach (var line in lines)
			need += (soFar.Count == 0 ? Print.FellIt : Print.Fell).GetMultilineStringSize(line, HorizontalAlignment.Left, width, px).Y - 2;
		var cap = LargePrint.Pick(118f, 128f, 136f);
		var frame = Column(hook, "month", 0, expand: true);
		// (the height is the scroll's own, so the "more below" line, when there is more, is extra)
		frame.GetChild<ScrollContainer>(0).CustomMinimumSize = new Vector2(0, Mathf.Ceil(Mathf.Min(need + 4, cap)));
		box.AddChild(frame);
		return box;
	}

	/// <summary>A line pencilled on the proof by the compositor: italic, and dark enough to read.</summary>
	private static Label Pencil(string text) => Print.Label(text, Print.FellIt, 13, HorizontalAlignment.Center, new Color(Print.Ink, 0.9f));

	/// <summary>Where each column of the proof was scrolled to, kept while the bench is reset under the hand.</summary>
	private readonly Dictionary<string, int> _scrolled = new();

	/// <summary>
	/// A column of the proof that scrolls when its matter runs long (a scroll is never shrunk to fit,
	/// so its type stays at the size asked for), with a line at its foot that says there is more.
	/// </summary>
	private Control Column(VBoxContainer content, string key, float width, bool expand = false)
	{
		var v = new VBoxContainer { CustomMinimumSize = new Vector2(width, 0), SizeFlagsVertical = SizeFlags.ExpandFill };
		if (expand) v.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		v.AddThemeConstantOverride("separation", 0);
		var sc = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		Print.InkScrollbar(sc);
		// (filling the frame, so a short column's matter can still be centred in it)
		content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		content.SizeFlagsVertical = SizeFlags.ExpandFill;
		sc.AddChild(content);
		v.AddChild(sc);
		var more = Print.Label("▼ more below (scroll)", Print.FellIt, 12, HorizontalAlignment.Center, wrap: false);
		more.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		more.Visible = false;
		more.MouseFilter = MouseFilterEnum.Ignore;
		v.AddChild(more);
		var bar = sc.GetVScrollBar();
		var keep = _scrolled.GetValueOrDefault(key);
		sc.Ready += () => Callable.From(() => { if (IsInstanceValid(sc)) sc.ScrollVertical = keep; }).CallDeferred();
		void Show()
		{
			if (!IsInstanceValid(sc) || !IsInstanceValid(more)) return;
			var over = bar.MaxValue - bar.Page > 4;
			more.Visible = over;
			more.Text = bar.Value < bar.MaxValue - bar.Page - 4 ? "▼ more below (scroll)" : "▲ more above";
		}
		bar.Changed += Show;
		bar.ValueChanged += val => { _scrolled[key] = (int)val; Show(); Untip(); };
		return v;
	}

	/// <summary>What moved suspicion last month, cause by cause, as the books record it.</summary>
	private static List<string> SuspicionLines(PaperFacts f)
	{
		if (f.FirstEdition) return new() { "Nothing yet: the shop has only just opened." };
		var moved = f.HeatLines.Where(x => Math.Abs(x.Amount) >= 0.5).OrderByDescending(x => Math.Abs(x.Amount)).ToList();
		if (moved.Count == 0) return new() { "No change last month." };
		return moved.Select(x => $"{(x.Amount > 0 ? "Rose" : "Fell")} {Math.Abs(x.Amount):0}: {HeatWhy(x.Label)}.").ToList();
	}

	/// <summary>A cause in the ward's words, and what a newcomer needs to know of it.</summary>
	private static string HeatWhy(string label) => label switch
	{
		"Deaths among customers" => "a death among the regulars. The ward talks of every death at the shop, whether the bottle is blamed or not, and of a blamed one twice as much",
		"Inquest verdicts" => "the coroner's juries. A verdict against the bottle raises it; a jury that finds nothing lowers it a little",
		"Talk dies down" => "the talk dying down, as it does in a quiet month",
		"Bold advertising claims" => "the claims on the label and in the paper: the bolder the claim, the more it is talked of",
		"Letters answered" => "replies to the post",
		"Complaints" => "complaints at the counter",
		_ => label.Length > 0 ? char.ToLowerInvariant(label[0]) + label[1..] : label,
	};

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

	internal static int Risk(Regular r) =>
		(r.Looks switch { "gravely_ill" => 30, "poorly" => 20, "peaky" => 8, _ => 0 }) + (r.Withdrawal ? 6 : 0) + (r.Craving ? 3 : 0) + (r.Infant ? 4 : 0);

	private Control Counter(DeskModel m)
	{
		var strip = new Control { CustomMinimumSize = new Vector2(0, StripHeight) };
		var bg = new ColorRect { Color = Print.Paper };
		bg.SetAnchorsPreset(LayoutPreset.FullRect);
		strip.AddChild(bg);
		var tint = new ColorRect { Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/tint.gdshader") } };
		tint.SetAnchorsPreset(LayoutPreset.FullRect);
		strip.AddChild(tint);
		// the shelves fill as the shop grows, and each venture taken up hangs its card on the wall
		strip.AddChild(new CounterDressing(m) { ClearRight = 350f });
		// at the larger sizes the counter stands higher and the people a little shorter, so the
		// cards in front of it have room for larger type
		var top = CounterTop;
		var counter = new ColorRect { Color = Print.Ink, AnchorTop = 1, AnchorRight = 1, AnchorBottom = 1, OffsetTop = top - StripHeight };
		strip.AddChild(counter);
		strip.AddChild(new ColorRect { Color = Print.Paper, AnchorTop = 1, AnchorRight = 1, AnchorBottom = 1, OffsetTop = top - StripHeight + 4, OffsetBottom = top - StripHeight + 6 });

		var title = Print.Label("AT THE COUNTER", Print.Rye, 14, HorizontalAlignment.Right, wrap: false);
		title.AnchorLeft = title.AnchorRight = 1;
		title.OffsetLeft = -300; title.OffsetRight = -14; title.OffsetTop = 8;
		strip.AddChild(title);

		// the most notable regulars: the sick first, then the hooked, then the new; fewer of them
		// at the larger sizes, each card wider, and every one listed in THE REGULARS
		var shown = m.Regulars.OrderByDescending(Risk).ThenBy(r => r.MonthsAsCustomer).Take(LargePrint.Pick(5, 4, 3)).ToList();
		// he stands beside the gravest case shown, and the warning names the same customer
		var atRisk = shown.FirstOrDefault(r => r.Looks is "gravely_ill") ?? shown.FirstOrDefault(r => r.Looks is "poorly");
		_deathBeside = atRisk?.Name;
		_deathRegular = atRisk;
		DeathPresent = atRisk != null;
		var others = m.Regulars.Count - shown.Count;
		// every regular, with their health and habit, a click away
		if (m.Regulars.Count > 0)
		{
			var all = Print.Button(AllRegularsText, 11);
			all.TooltipText = $"The list of all {Spell(m.Regulars.Count)} regulars, the frailest first: how each looks, their habit, and how long they have come.";
			// on the heading's line, to its left, above the tallest hat
			all.AnchorLeft = all.AnchorRight = 1;
			all.OffsetLeft = -340; all.OffsetRight = -172; all.OffsetTop = 5;
			all.Pressed += () => OpenDrawer("regulars");
			if (_drawer != null) all.MouseFilter = MouseFilterEnum.Ignore;
			strip.AddChild(all);
		}
		var more = Print.Label(others > 0 ? $"and {Spell(others)} more regular{(others == 1 ? "" : "s")}" : m.Regulars.Count == 0 ? "No one has come in yet." : "", Print.FellIt, 13);
		more.HorizontalAlignment = HorizontalAlignment.Right;
		more.AnchorLeft = more.AnchorRight = 1;
		more.OffsetLeft = -300; more.OffsetRight = -14; more.OffsetTop = 28;
		more.MouseFilter = MouseFilterEnum.Ignore;
		strip.AddChild(more);

		// the strip is as wide as the proof column; share it out between the figures
		const float width = 1280f - 44f - 20f - BenchWidth, start = 54f;
		var slot = Math.Min(LargePrint.Pick(172f, 200f, 240f), (width - start - 70f) / Math.Max(shown.Count, 1));
		// the people stand a little shorter when the counter stands higher
		var scale = (top - 30f) / (226f - 30f);
		var tags = new List<(PanelContainer Tag, string About)>();
		for (var i = 0; i < shown.Count; i++)
		{
			var r = shown[i];
			var x = start + i * slot;
			// Death stands at the shoulder of whoever is nearest the end, a head taller than anyone
			// he leans toward the one he is waiting for
			if (r == atRisk && AddFigure(strip, "wc_death", x - 44 * scale, 230 * scale, 1f) is { } death)
			{
				Anim.Sway(death, 1.4f, 4.2f, lean: 3.5f);
				death.MouseFilter = _drawer == null ? MouseFilterEnum.Pass : MouseFilterEnum.Ignore;
				var alsoFrail = m.Regulars.Count(o => o != r && o.Looks is "gravely_ill" or "poorly");
				death.TooltipText = $"Death, at the shoulder of {r.Name}, who is {(r.Looks == "gravely_ill" ? "gravely ill" : "poorly")}. He stands beside whoever looks nearest the end{(alsoFrail > 0 ? $"; {Spell(alsoFrail)} more regular{(alsoFrail == 1 ? " is" : "s are")} poorly or gravely ill too" : "")}. Any regular may die of age or illness; the frail most of all.";
			}
			// the same stock cut twice in a row is printed the other way round
			var cut = CutFor(r);
			var flip = shown.Take(i).Count(o => CutFor(o) == cut) % 2 == 1;
			// people are not all one height, even when the printer owns one engraving of them
			var h = (176 + (int)(StableHash(r.Id) % 28u)) * scale;
			var fig = AddFigure(strip, cut, x + 10, h, 1f, flip);
			if (fig != null)
			{
				// behind an open drawer the counter keeps its hover text to itself
				fig.MouseFilter = _drawer == null ? MouseFilterEnum.Pass : MouseFilterEnum.Ignore;
				fig.TooltipText = $"{r.Name}: {(r.Infant ? "for " : "")}{r.Ailment.ToLowerInvariant()}. {Cap(Since(r))}; {Panacea.Game.SimGame.Describe(r)}.";
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
				no.Position = new Vector2(x, Math.Max(4, top - h - 20));
				no.CustomMinimumSize = new Vector2(slot - 10, 0);
				no.MouseFilter = MouseFilterEnum.Ignore;
				strip.AddChild(no);
			}
			// the card stands on the strip's foot and grows upward, over the counter's front, as its
			// type grows: never past the foot of the strip, where it would show under an open drawer
			var tag = Print.Slip(Tag(r), LargePrint.Pick(5, 5, 6));
			tag.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			var stand = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.End, MouseFilter = MouseFilterEnum.Ignore, AnchorBottom = 1 };
			stand.OffsetLeft = x; stand.OffsetRight = x + slot - 10; stand.OffsetTop = 4; stand.OffsetBottom = -3;
			stand.AddChild(tag);
			// what the mouse would show on hover, for the keyboard (see below)
			var about = $"{r.Name}: {Since(r)}; {Panacea.Game.SimGame.Describe(r)}.";
			tag.TooltipText = _drawer != null ? "" : (fig?.TooltipText ?? about) + "\n" + LooksKey;
			if (_drawer != null) tag.MouseFilter = MouseFilterEnum.Ignore;
			strip.AddChild(stand);
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
				foreach (var l in Print.Descendants<Label>(t)) if (!l.HasMeta("chip")) l.AddThemeColorOverride("font_color", on ? Print.Paper : Print.Ink);
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
		if (atRisk == null && AddFigure(strip, "wc_death", start + shown.Count * slot + 6, 230 * scale, 0.2f) is { } waiting)
		{
			Anim.Sway(waiting, 0.8f, 5.5f);
			waiting.MouseFilter = _drawer == null ? MouseFilterEnum.Pass : MouseFilterEnum.Ignore;
			waiting.TooltipText = "Death, at the far end of the counter. Nobody looks near the end; but any regular may die of age or illness, the peaky sooner than the hale. When someone falls poorly he comes and stands beside them.";
		}
		return strip;
	}

	/// <summary>The counter's button to the list of every regular.</summary>
	private const string AllRegularsText = "ALL THE REGULARS ▸";

	/// <summary>How long a regular has come, in words: "new this month", "a regular for three months".</summary>
	public static string Since(Regular r) => r.MonthsAsCustomer <= 0 ? "new this month" : $"a regular for {Spell(r.MonthsAsCustomer)} month{(r.MonthsAsCustomer == 1 ? "" : "s")}";

	/// <summary>FNV-1a: the same id gives the same figure on every launch.</summary>
	private static uint StableHash(string s)
	{
		var h = 2166136261u;
		foreach (var ch in s) { h ^= ch; h *= 16777619u; }
		return h;
	}

	/// <summary>The top of the counter in the strip: higher at the larger sizes, where the cards in front of it are taller.</summary>
	private static float CounterTop => LargePrint.Pick(226f, 206f, 190f);
	private const float StripHeight = 292f;

	/// <summary>What a verdict against the bottle does (the sim's Scrutiny.ResolveInquest, Implicate(24, 12, 8)): printed before the reply.</summary>
	private const int InquestHeat = 24, InquestGoodName = 12;
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

	/// <summary>What each look means, for the card's tooltip.</summary>
	public const string LooksKey = "Hale: well. Peaky: a little pale and off colour, not yet in danger. POORLY: ill, and may die. GRAVELY ILL (white on black): near the end.";

	/// <summary>
	/// A regular's card at the counter: name, complaint, how they look, and their habit. The card's
	/// type grows with the text size (its woodtype too, which the floors leave alone), and both the
	/// health and the habit are set heavier as they worsen, so the danger reads without the words.
	/// </summary>
	private static Control Tag(Regular r)
	{
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", LargePrint.Pick(0, 1, 2));
		var name = Print.Label(r.Name, Print.Rye, LargePrint.Pick(12, 14, 15));
		name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		v.AddChild(name);
		v.AddChild(Print.Label((r.Infant ? "for " : "") + r.Ailment.ToLowerInvariant(), Print.Fell, 12));
		// how they look (hale in italic, peaky in roman, POORLY in capitals, GRAVELY ILL reversed),
		// and, when the bottle has no hold on them, their mood beside it
		var line = new HFlowContainer();
		line.AddThemeConstantOverride("h_separation", 4);
		line.AddThemeConstantOverride("v_separation", 1);
		// (the infant's, when the bottle is bought for one; at the regular size the card has room for
		// "INFANT POORLY" but not "INFANT GRAVELY ILL", and the line above already says "for an infant")
		var looks = Health(r.Looks, r.Infant && (LargePrint.Level > 0 || r.Looks != "gravely_ill") ? "infant " : "");
		looks.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		line.AddChild(looks);
		var habit = Habit(r);
		if (habit == null)
		{
			var mood = Print.Label("· " + (r.MonthsAsCustomer == 0 ? "new" : r.Mood), Print.FellIt, 12, wrap: false);
			mood.SizeFlagsVertical = SizeFlags.ShrinkCenter;
			line.AddChild(mood);
		}
		v.AddChild(line);
		// the habit has a line of its own, never cut short
		if (habit != null) v.AddChild(habit);
		return v;
	}

	/// <summary>How a customer looks, set heavier as it worsens.</summary>
	public static Control Health(string looks, string who = "")
	{
		var rye = LargePrint.Pick(11, 13, 15);
		return looks switch
		{
			"gravely_ill" => Chip((who + "gravely ill").ToUpperInvariant(), rye, inverted: true),
			"poorly" => Print.Label((who + "poorly").ToUpperInvariant(), Print.Rye, rye + 1, wrap: false),
			"peaky" => Print.Label(who + "peaky", Print.Fell, 12, wrap: false),
			_ => Print.Label(who + "hale", Print.FellIt, 12, wrap: false),
		};
	}

	/// <summary>
	/// The bottle's hold on a customer, set heavier as it worsens, as the health is: asking for it by
	/// name in ruled capitals, shaking for want of it reversed, white on black, and ruined by it
	/// reversed with a hand. Null when it has no hold on them.
	/// </summary>
	public static Control? Habit(Regular r)
	{
		var rye = LargePrint.Pick(11, 13, 15);
		Control? chip = r.Ruined ? Chip("☞ RUINED BY IT", rye, inverted: true)
			: r.Withdrawal ? Chip("SHAKING", rye, inverted: true)
			: r.Craving ? Chip("ASKS BY NAME", rye, inverted: false)
			: null;
		if (chip != null)
		{
			chip.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
			chip.TooltipText = r.Ruined ? "Ruined by it: has spent beyond their means on the bottle. One bottle at most now, and the family complains."
				: r.Withdrawal ? "Shaking: craves the bottle and has not had enough of it. Withdrawal weakens a frail customer."
				: "Asks by name: has come to crave the bottle, and takes more each month.";
			chip.MouseFilter = MouseFilterEnum.Pass;
		}
		return chip;
	}

	/// <summary>A word set in woodtype in a ruled box, or reversed, white on black.</summary>
	private static Control Chip(string text, int size, bool inverted)
	{
		var l = Print.Label(text, Print.Rye, size, color: inverted ? Print.Paper : Print.Ink, wrap: false);
		// a chip keeps its own colours when the card around it is printed in reverse
		l.SetMeta("chip", true);
		// on a reversed card a reversed chip keeps a paper rule round it, and a ruled one its paper
		return Print.Panel(l, inverted ? Print.Box(Print.Ink, Print.Paper, 1, 2) : Print.Box(Print.Paper, Print.Ink, 1, 2));
	}

	// ---------- the composing bench ----------

	private Control TypeCase(DeskModel m)
	{
		var v = SlipBody("THE TYPE CASE");
		var product = m.Formulas.FirstOrDefault(x => x.Id == _adFormula) ?? m.Formulas.FirstOrDefault();
		Control Done()
		{
			var s = Print.Slip(v, SlipPad);
			s.SizeFlagsVertical = SizeFlags.ExpandFill;
			return s;
		}
		if (product == null)
		{
			v.AddChild(Print.Body("[i]No remedy yet: open the formula book.[/i]", 13, justify: false));
			return Done();
		}
		// the case's matter scrolls under its title; a slug at its foot, in a row of its own that is
		// always kept, says when there is more below, and never covers a claim
		var frame = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
		Print.InkScrollbar(frame);
		// the case keeps its place when the bench is reset under the hand
		var keepScroll = _slipsScroll;
		frame.Ready += () => Callable.From(() => { if (IsInstanceValid(frame)) frame.ScrollVertical = keepScroll; }).CallDeferred();
		var c = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
		c.AddThemeConstantOverride("separation", 5);
		frame.AddChild(c);
		v.AddChild(frame);

		var large = LargePrint.Level > 0;
		if (!large) c.AddChild(Print.Label("SPACE IN THE GAZETTE", Print.Rye, 12));
		var sizes = new HBoxContainer();
		sizes.AddThemeConstantOverride("separation", large ? 3 : 4);
		var tier = AdSpace.TierFor(m.AdSpendCents);
		for (var i = 0; i < AdSpace.Tiers.Length; i++)
		{
			var t = AdSpace.Tiers[i];
			// at the larger sizes each size carries its rate under it, in place of a line of rates
			var b = Print.Button(large ? $"{t.Short}\n{(t.Cents == 0 ? "—" : EditionWriter.Money(t.Cents))}" : t.Short, 11, solid: m.CanAdvertise && i == tier);
			b.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			if (large) Tight(b, 2);
			b.TooltipText = t.Cents == 0 ? "No notice in the Gazette" : $"{t.Name}: {EditionWriter.Money(t.Cents)} a month";
			Print.SetLive(b, m.CanAdvertise);
			var cents = t.Cents;
			b.Pressed += () => Act(_game.SetAdSpend(cents));
			sizes.AddChild(b);
		}
		// at the larger sizes, space not yet for sale is said in a line in place of five shut tickets
		if (!large || m.CanAdvertise) c.AddChild(sizes);
		else sizes.QueueFree();
		// why the space cannot be bought yet, printed where the tickets are, not only in a tooltip
		var adsNext = m.NextUnlocks.FirstOrDefault(u => u.Name.Contains("Advertis"));
		var ready = adsNext.Name != null && adsNext.Progress >= 0.999;
		if (!m.CanAdvertise) c.AddChild(Print.Body(large
			? (ready ? "[b]SPACE IN THE GAZETTE[/b] [i]can be bought from next month.[/i]" : "[b]SPACE IN THE GAZETTE[/b] [i]is not for sale until you have $100 saved and a testimonial printed.[/i]")
			: ready ? "[i]Both conditions are met: space can be bought from next month.[/i]"
			: "[i]Not yet: the Gazette wants $100 in savings and a printed testimonial first.[/i]", 12, justify: false));
		// the rates, printed rather than hidden in the tooltips
		if (!large) c.AddChild(Print.Label("Rates: " + string.Join(" · ", AdSpace.Tiers.Where(t => t.Cents > 0).Select(t => $"{t.Short.ToLowerInvariant()} {EditionWriter.Money(t.Cents)}")) + " a month.", Print.FellIt, 12));
		// a second remedy has its own label: choose which one the claims are set for
		if (m.Formulas.Count > 1)
		{
			var tabs = new HFlowContainer();
			tabs.AddThemeConstantOverride("h_separation", 4);
			tabs.AddThemeConstantOverride("v_separation", 4);
			foreach (var g in m.Formulas)
			{
				var t = Print.Button(g.Name.ToUpperInvariant(), 10, solid: g.Id == product.Id);
				var gid = g.Id;
				t.TooltipText = $"Set the claims on the label of {g.Name}.";
				t.Pressed += () => { _adFormula = gid; Refresh(); };
				tabs.AddChild(t);
			}
			c.AddChild(tabs);
		}
		// at the larger sizes the line is set as a head in woodtype, to leave the case three rows of claims
		if (large)
		{
			var head = Print.Label("CLAIMS, UP TO FOUR: THE CORONER READS THEM", Print.Rye, 11, wrap: false);
			head.TooltipText = $"Claims for {product.Name}, up to four. Bolder type (the more !) sells more, and the coroner reads it too.";
			head.MouseFilter = MouseFilterEnum.Pass;
			c.AddChild(head);
		}
		else c.AddChild(Print.Body($"[i]Claims for {Print.Escape(product.Name)}, up to four. Bolder type sells more, and the coroner reads it too.[/i]", 12, justify: false));
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
				var next = on ? product.ClaimIds.Where(x => x != claim).ToList() : product.ClaimIds.Append(claim).ToList();
				Act(_game.SetClaims(product.Id, next));
			};
			flow.AddChild(b);
		}
		c.AddChild(flow);
		if (_note != null)
		{
			var why = _note.Contains("At most", StringComparison.Ordinal) ? _note + " Strike one out to make room." : _note;
			c.AddChild(Print.Body($"[b]Refused.[/b] [i]{Print.Escape(why)}[/i]", 12, justify: false));
		}
		// the slug: inked when there is more below, a hairline when the foot is in view
		var slugText = Print.Label("", Print.Rye, 11, HorizontalAlignment.Center, Print.Paper, wrap: false);
		slugText.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		var slug = Print.Panel(slugText, Print.Box(new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), 0, 2));
		slug.CustomMinimumSize = new Vector2(0, 20);
		slug.MouseFilter = MouseFilterEnum.Ignore;
		v.AddChild(slug);
		var bar = frame.GetVScrollBar();
		void ShowMore()
		{
			if (!IsInstanceValid(frame) || !IsInstanceValid(slug)) return;
			var over = bar.MaxValue - bar.Page > 4;
			var below = over && bar.Value < bar.MaxValue - bar.Page - 4;
			slugText.Text = below ? "MORE CLAIMS BELOW ▼  (scroll)" : over ? "▲ THE TOP OF THE CASE ABOVE" : "";
			slugText.AddThemeColorOverride("font_color", below ? Print.Paper : Print.Ink);
			slug.AddThemeStyleboxOverride("panel", below ? Print.Box(Print.Ink, Print.Ink, 0, 2) : Print.Box(new Color(0, 0, 0, 0), new Color(0, 0, 0, 0), 0, 2));
		}
		bar.Changed += ShowMore;
		bar.ValueChanged += val => { _slipsScroll = (int)val; ShowMore(); Untip(); };
		return Done();
	}

	/// <summary>The inner margin of the bench's slips: closer at the larger sizes, where the type wants the room.</summary>
	private static int SlipPad => LargePrint.Pick(10, 7, 6);

	/// <summary>
	/// The post: how many letters wait, and the button to them, on one line at the top of a slip of
	/// one height at every size, so OPEN THE LETTERS is always in the same place.
	/// </summary>
	private Control LettersSlip(DeskModel m)
	{
		var answered = MonthBook.LettersAnswered(_game);
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 4);
		var head = new HBoxContainer();
		head.AddThemeConstantOverride("separation", 6);
		var title = Print.Label(m.Letters.Count == 0 ? (answered > 0 ? "THE POST IS ANSWERED" : "NO LETTERS") : m.Letters.Count == 1 ? "ONE LETTER" : $"{Spell(m.Letters.Count).ToUpperInvariant()} LETTERS", Print.Rye, 17, wrap: false);
		title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		title.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		head.AddChild(title);
		if (m.Letters.Count > 0)
		{
			var open = Print.Button("OPEN THE LETTERS", 13, solid: true);
			open.Pressed += () => OpenDrawer("letters");
			head.AddChild(open);
		}
		v.AddChild(head);
		v.AddChild(Print.Rule(1.4f, 0));
		if (m.Letters.Count == 0)
		{
			var none = Print.Label(answered > 0
				? $"{Cap(Spell(answered))} letter{(answered == 1 ? "" : "s")} answered this month; nothing more in the post."
				: "Nothing in the post this month but a circular for galvanic belts.", Print.FellIt, 13);
			v.AddChild(none);
		}
		foreach (var l in m.Letters.Take(1))
		{
			// one line: the first letter's title, shortened by whole words to fit the slip at the size
			// it is set in (the whole post is in the drawer), and how many more
			var t = EditionWriter.Set(l.Title);
			var tail = m.Letters.Count > 1 ? $"; and {Spell(m.Letters.Count - 1)} more" : "";
			if (answered > 0) tail += $" ({Spell(answered)} answered)";
			var room = BenchWidth - 2 * (4 + 8) - 2 * 3 - 4;
			float Wide(string s) => Print.Fell.GetStringSize(s, HorizontalAlignment.Left, -1, LargePrint.Size(13)).X;
			var cut = t;
			while (Wide(cut + (cut == t ? "" : "…") + tail + ".") > room && cut.Contains(' '))
				cut = cut[..cut.LastIndexOf(' ')].TrimEnd(' ', ',');
			var text = cut + (cut == t ? "" : "…") + tail + ".";
			if (Wide(text) > room) text = cut + "…";
			var line = Print.Label(text, Print.Fell, 13, wrap: false);
			line.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
			line.ClipText = true;
			line.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			line.TooltipText = string.Join("\n", m.Letters.Select(x => x.Title));
			line.MouseFilter = MouseFilterEnum.Pass;
			v.AddChild(line);
		}
		var slip = Print.Slip(v, 8);
		// (the line under the rule may run to two at the larger sizes: the slip's height is its own)
		slip.ClipContents = true;
		return slip;
	}

	/// <summary>The post's slip, one height at every size and in every month.</summary>
	private static int PostHeight => 88;

	private Control WorksSlip(DeskModel m)
	{
		var v = SlipBody("THE WORKS");
		v.AddThemeConstantOverride("separation", 3);
		var large = LargePrint.Level > 0;
		// in words that never break inside a phrase ("better / known: 16"): a line breaks only between them
		static string Keep(string s) => s.Replace(' ', '\u00a0');
		var stock = m.Formulas.Sum(f => f.Stock);
		var brewing = m.Brewing.Sum(b => b.Bottles);
		var shelf = $"{stock} bottle{(stock == 1 ? "" : "s")} on the shelf";
		var still = brewing > 0 ? $"{brewing} brewing" : m.StillsFree > 0 ? "the still is cold" : "the still is busy";
		var cash = Print.Label(EditionWriter.Money(m.CashCents), Print.Rye, 22, HorizontalAlignment.Right, wrap: false);
		cash.SizeFlagsVertical = SizeFlags.ShrinkBegin;
		// the good name is moved by letters and deaths: show it, not only suspicion
		var nameText = large ? $"good name: {m.Reputation:0} of 100" : $"good name in the ward: {m.Reputation:0} of 100";
		var knownText = m.Notoriety >= 0.5 ? $"better known: {m.Notoriety:0}" : null;
		const string nameTip = "Good name in the ward: what the ward thinks of the shop, out of 100. A good name brings callers and keeps regulars; letters and deaths move it. A letter's \"good name +1\" adds to this figure.";
		const string knownTip = "How well known the shop is, from letters answered and inquests sat. It does no harm by itself, but the better known the shop, the likelier a verdict bought from the coroner is to come out. A letter's \"better known +1\" adds to this figure.";
		if (!large)
		{
			// two columns: the shelf and the still; the cash and the good name
			var grid = new HBoxContainer();
			grid.AddThemeConstantOverride("separation", 14);
			var left = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
			left.AddThemeConstantOverride("separation", 0);
			left.AddChild(Print.Label(shelf, Print.Fell, 14, wrap: false));
			left.AddChild(Print.Label(still, Print.FellIt, 13, wrap: false));
			grid.AddChild(left);
			var books = new VBoxContainer();
			books.AddThemeConstantOverride("separation", -2);
			books.AddChild(cash);
			var standing = Print.Label(Keep(nameText), Print.FellIt, 13, HorizontalAlignment.Right, wrap: false);
			standing.TooltipText = nameTip;
			standing.MouseFilter = MouseFilterEnum.Pass;
			books.AddChild(standing);
			if (knownText != null)
			{
				var known = Print.Label(Keep(knownText), Print.FellIt, 13, HorizontalAlignment.Right, wrap: false);
				known.TooltipText = knownTip;
				known.MouseFilter = MouseFilterEnum.Pass;
				books.AddChild(known);
			}
			grid.AddChild(books);
			v.AddChild(grid);
		}
		else
		{
			// at the larger sizes, a line each: the cash beside the shelf and the still, then the good
			// name and how well known, breaking only between the two
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 10);
			var left = Print.Label(Keep(shelf) + " · " + Keep(still), Print.Fell, 14);
			left.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			row.AddChild(left);
			row.AddChild(cash);
			v.AddChild(row);
			var standing = Print.Label(Keep(nameText) + (knownText != null ? " · " + Keep(knownText) : ""), Print.FellIt, 13);
			standing.TooltipText = nameTip + (knownText != null ? "\n" + knownTip : "");
			standing.MouseFilter = MouseFilterEnum.Pass;
			v.AddChild(standing);
		}
		// the regulars' health and the bottle's hold on them, at a glance: where the next deaths will
		// come from. The worse it is, the heavier it is set: the gravely ill and those shaking or ruined
		// for want of the bottle reversed, white on black; the poorly and the craving in capitals
		var grave = m.Regulars.Count(r => r.Looks == "gravely_ill");
		var poorly = m.Regulars.Count(r => r.Looks == "poorly");
		var ruined = m.Regulars.Count(r => r.Ruined);
		var shaking = m.Regulars.Count(r => !r.Ruined && r.Withdrawal);
		var craving = m.Regulars.Count(r => !r.Ruined && !r.Withdrawal && r.Craving);
		var health = new HFlowContainer();
		health.AddThemeConstantOverride("h_separation", 4);
		health.AddThemeConstantOverride("v_separation", 2);
		var rye = LargePrint.Pick(11, 13, 14);
		void Add(Control c) { c.SizeFlagsVertical = SizeFlags.ShrinkCenter; c.MouseFilter = MouseFilterEnum.Ignore; health.AddChild(c); }
		if (grave > 0) Add(Chip($"{grave} GRAVELY ILL", rye, inverted: true));
		if (poorly > 0) Add(Print.Label($"{poorly} POORLY", Print.Rye, rye, wrap: false));
		if (ruined > 0) Add(Chip($"{ruined} RUINED BY IT", rye, inverted: true));
		if (shaking > 0) Add(Chip($"{shaking} SHAKING", rye, inverted: true));
		if (craving > 0) Add(Chip($"{craving} CRAVING", rye, inverted: false));
		if (health.GetChildCount() == 0) Add(Print.Label("the regulars look well", Print.FellIt, 13, wrap: false));
		var frail = new List<string>();
		if (grave > 0) frail.Add($"{grave} gravely ill");
		if (poorly > 0) frail.Add($"{poorly} poorly");
		if (ruined > 0) frail.Add($"{ruined} ruined by the bottle");
		if (shaking > 0) frail.Add($"{shaking} shaking for want of it");
		if (craving > 0) frail.Add($"{craving} craving it");
		health.TooltipText = "How the regulars look across the counter: " + (frail.Count > 0 ? string.Join(", ", frail) : "all well") + ". The gravely ill and the poorly are the ones most likely to die: of their ailments, of age, or of a strong bottle taken once too often. Craving: they ask for the remedy by name and take more each month; shaking: they want it and have not had it; ruined: they have spent beyond their means on it. A bottle whose habit the assay calls \"slight\" never does it; a stronger habit does, a little at a time. ALL THE REGULARS, at the counter, lists every one.";
		health.MouseFilter = MouseFilterEnum.Pass;
		v.AddChild(health);
		v.AddChild(Suspicion(m.Heat, SuspicionLines(_game.Facts)));
		// where last month's money went, in a line; THE BOOKS has the rest (and at the larger sizes,
		// where the line would cost the type case its claims, the cash's tooltip and THE BOOKS)
		var till = TillLine();
		if (till != null && large) cash.TooltipText = till + "\nOpen THE BOOKS for where every cent went.";
		if (till != null && !large)
		{
			var t = Print.Label(till, Print.FellIt, 13);
			t.TooltipText = "Last month's takings and the books, from the edition. Open THE BOOKS for where every cent went, what a bottle earns, and the months before.";
			t.MouseFilter = MouseFilterEnum.Pass;
			v.AddChild(t);
		}
		var slip = Print.Slip(v, SlipPad);
		// a slip of one height for the size, whatever it holds, so the post under it never moves
		slip.CustomMinimumSize = new Vector2(0, WorksHeight);
		return slip;
	}

	/// <summary>The works' height at each size: room for its fullest month (a reversed suspicion, a second line of the books).</summary>
	private static int WorksHeight => LargePrint.Pick(170, 200, 214);

	/// <summary>
	/// The drawer tickets, in a row of their own just above the lever: the same size and place at
	/// every text size and in every month. Something new behind a drawer (a still, a venture on
	/// offer, a new method) is printed in reverse until looked at.
	/// </summary>
	private Control DrawerRow(DeskModel m)
	{
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 4);
		var stills = m.Stills > 0 ? m.Stills : m.StillsFree + m.Brewing.Count;
		if (_seenStills == 0) _seenStills = stills;
		var offers = m.Investments.Count(i => !i.Held && i.CanBuy);
		var procs = m.Processes.Count;
		if (_seenProcs == 0) _seenProcs = procs;
		var fresh = new Dictionary<string, bool>
		{
			["still"] = stills > _seenStills && _seenStills > 0,
			["formula"] = procs > _seenProcs && _seenProcs > 0,
			["ventures"] = offers > _seenOffers,
		};
		foreach (var (key, label) in new[] { ("still", "THE STILL"), ("formula", "FORMULA"), ("supplies", "SUPPLIES"), ("ventures", "VENTURES"), ("books", "BOOKS") })
		{
			var isNew = fresh.GetValueOrDefault(key);
			var b = Print.Button(label, 11, solid: isNew);
			b.TooltipText = isNew
				? key switch { "still" => "New: another still is yours.", "formula" => "New: a method of making in the formula book.", _ => "New: a venture is on offer." }
				: key switch
				{
					"still" => "Light the still: brew bottles for the shelf.",
					"formula" => "The formula book: the recipe, its price, and what the assay finds in it.",
					"supplies" => $"Order drugs from {m.Supplier.Name.Replace("J. & W. HOBART & CO.", "Hobart's")} of {m.Supplier.Street}.",
					"ventures" => "The prospectus: improvements a growing house may take up.",
					_ => "The books: where last month's money went, and every month before.",
				};
			b.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			Tight(b, 4);
			var k = key;
			b.Pressed += () =>
			{
				if (k == "still") _seenStills = stills;
				if (k == "ventures") _seenOffers = offers;
				if (k == "formula") _seenProcs = procs;
				OpenDrawer(k);
			};
			row.AddChild(b);
		}
		// on a strip of paper, so a ticket printed in reverse still shows its edge against the table
		return Print.Panel(row, Print.Box(Print.Paper, Print.Ink, 2, 3));
	}

	/// <summary>A ticket set close: narrower side margins, so five fit across the bench.</summary>
	private static void Tight(Button b, int lr)
	{
		foreach (var st in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled", "focus" })
			if (b.GetThemeStylebox(st) is StyleBoxFlat f) { f.ContentMarginLeft = lr; f.ContentMarginRight = lr; }
	}
	private int _seenProcs;

	/// <summary>
	/// Suspicion, set heavier as it climbs, so it reads without the words: a pencilled italic line,
	/// then roman, then a ruled box in capitals, then a reversed slug, white on black, at its worst
	/// larger still; with a rule of ink beside it that fills toward the hundred. Its tooltip says
	/// what moved it last month, cause by cause.
	/// </summary>
	private static Control Suspicion(double heat, List<string> why)
	{
		var talk = DeskView.Talk(heat);
		var n = $"{heat:0} of 100";
		// at its worst the slug runs the width of the slip, and the gauge goes under it; at the
		// larger sizes the gauge always goes under it, so the words keep the width
		var worst = heat >= 40;
		var stacked = worst || LargePrint.Level > 0;
		BoxContainer h = stacked ? new VBoxContainer() : new HBoxContainer();
		h.AddThemeConstantOverride("separation", stacked ? 3 : 8);
		Control slug;
		if (heat < 3) slug = Print.Label($"suspicion: {talk} ({n})", Print.FellIt, 13, wrap: stacked);
		else if (heat < 15) slug = Print.Label($"suspicion: {talk} ({n})", Print.Fell, 14, wrap: stacked);
		else if (heat < 40) slug = Print.Panel(Print.Label($"SUSPICION: {talk.ToUpperInvariant()} · {n.ToUpperInvariant()}", Print.Rye, LargePrint.Pick(12, 13, 14), wrap: stacked), Print.Box(new Color(0, 0, 0, 0), Print.Ink, 2, 3));
		else slug = Print.Panel(Print.Label($"☞ SUSPICION {heat:0} OF 100 · {talk.ToUpperInvariant()}", Print.Rye, heat < 70 ? LargePrint.Pick(14, 15, 16) : LargePrint.Pick(16, 17, 18), HorizontalAlignment.Center, Print.Paper), Print.Box(Print.Ink, Print.Ink, 0, 4));
		slug.SizeFlagsHorizontal = stacked ? SizeFlags.ExpandFill : SizeFlags.ShrinkBegin;
		slug.SizeFlagsVertical = SizeFlags.ShrinkCenter;
		h.AddChild(slug);
		// the gauge: a ruled track, filled with ink to the figure
		var track = new Control { CustomMinimumSize = new Vector2(0, stacked ? 10 : 12), SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore };
		var frame = new Panel { MouseFilter = MouseFilterEnum.Ignore };
		frame.SetAnchorsPreset(LayoutPreset.FullRect);
		frame.AddThemeStyleboxOverride("panel", Print.Box(new Color(0, 0, 0, 0), Print.Ink, 1, 0));
		track.AddChild(frame);
		var fill = new ColorRect { Color = Print.Ink, AnchorTop = 0, AnchorBottom = 1, AnchorLeft = 0, AnchorRight = (float)Math.Clamp(heat / 100.0, 0.02, 1), MouseFilter = MouseFilterEnum.Ignore };
		track.AddChild(fill);
		h.AddChild(track);
		h.TooltipText = $"Suspicion, {n}: what the ward and the authorities say about your bottles. The more it talks, the more inquests the coroner calls; at 100 the Board of Health moves to close the shop.\nLast month: {string.Join(" ", why)} (The proof's SUSPICION column has the same.)";
		h.MouseFilter = MouseFilterEnum.Pass;
		slug.MouseFilter = MouseFilterEnum.Pass;
		slug.TooltipText = h.TooltipText;
		return h;
	}

	/// <summary>Last month's books in one line a non-gamer reads: what came in, and what was left.</summary>
	private string? TillLine()
	{
		var f = _game.Facts;
		if (f.FirstEdition || f.CashLines.Count == 0) return null;
		var tookIn = f.CashLines.Where(x => x.Cents > 0).Sum(x => x.Cents);
		var net = f.CashCents - f.CashStartCents;
		var fixedCents = -f.CashLines.Where(x => BooksDrawer.Fixed.Contains(x.Label)).Sum(x => x.Cents);
		var kept = net >= 0 ? $"kept {EditionWriter.Money(net)}" : $"lost {EditionWriter.Money(-net)}";
		return $"Last month: took {EditionWriter.Money(tookIn)}, {kept}" + (fixedCents > 0 ? $"; rent and charges {EditionWriter.Money(fixedCents)}." : ".");
	}

	private bool _armed;

	private Control Lever(bool nothingToSell)
	{
		var b = Print.Button("SEND TO PRESS ☞", 28, solid: true, sound: null);
		b.CustomMinimumSize = new Vector2(0, 58);
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
			holder.OffsetLeft = 14; holder.OffsetRight = -14; holder.OffsetTop = 6; holder.OffsetBottom = 0;
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
			frame.GetVScrollBar().ValueChanged += _ => Untip();
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
					case "books": { _tool = new BooksDrawer(_game); break; }
					case "regulars": { _tool = new RegularsList(_game); break; }
					default: { var t = new OrderForm(_game); t.Changed += Rebuild; _tool = t; break; }
				}
				_toolKind = which;
				_tool.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			}
			_tool.GetParent()?.RemoveChild(_tool);
			var frame = new ScrollContainer { SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, FollowFocus = true };
			Print.QuietScrollbar(frame);
			// a tooltip does not stay floating over the list once it has scrolled from under it
			frame.GetVScrollBar().ValueChanged += _ => Untip();
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
		v.AddChild(Print.Label(s, Print.Rye, LargePrint.Pick(15, 16, 17), HorizontalAlignment.Center));
		v.AddChild(Print.Dash(60));
		return v;
	}


	/// <summary>Last month's lines, struck through by the printer (nothing for the proprietor to do), with a note under each if given.</summary>
	private static void Struck(VBoxContainer col, IEnumerable<string> lines, string none, IReadOnlyList<string>? notes = null)
	{
		var all = lines.ToList();
		var list = all.Take(3).ToList();
		// the compositor's pencil is dark enough to read (it was a grey of 4.7:1 at five pixels)
		if (list.Count == 0) { col.AddChild(Pencil(none)); return; }
		if (all.Count > 3) list[^1] += $" And {Spell(all.Count - 3)} more.";
		var key = Pencil("last month's, struck out by the printer");
		key.TooltipText = "The printer strikes last month's matter from the proof (\"dele\") and sets this month's after press. Nothing for you to do.";
		key.MouseFilter = MouseFilterEnum.Pass;
		col.AddChild(key);
		for (var i = 0; i < list.Count; i++)
		{
			col.AddChild(Print.Body($"[s]{Print.Escape(EditionWriter.Set(list[i]))}[/s]", 12, new Color(Print.Ink, 0.9f), justify: false));
			if (notes != null && i < notes.Count && notes[i].Length > 0)
				col.AddChild(Print.Body($"[i]{Print.Escape(notes[i])}[/i]", 12, justify: false));
		}
	}

	/// <summary>
	/// Whether the shop's bottle played a part in a death, as far as the proprietor can know it: the
	/// register's cause, what the assay says of the remedy, and whether an inquest was called.
	/// </summary>
	private static string BottleNote(PersonFact d, PaperFacts f, DeskModel m)
	{
		var product = f.Products.FirstOrDefault(p => p.Name == d.Product) ?? (d.Product != null ? f.Products.FirstOrDefault() : null);
		var blamed = d.Cause is "overdose" or "wasting";
		var cause = d.Cause switch
		{
			"overdose" => "Found insensible after a dose: the register blames the bottle.",
			"wasting" => "Wasted away, as from a slow poison: the register blames the bottle.",
			"old_age" => "Of age, the register says.",
			"ailment" => d.Ailment.Length > 0 ? $"Of the {d.Ailment.ToLowerInvariant()}, the register says." : "Of the complaint, the register says.",
			_ => "Of a fever going about, the register says.",
		};
		var part = blamed ? ""
			: d.Product == null ? " Nothing had been bought at the shop in six months."
			: product != null && product.Danger < 0.8 && product.Accumulation < 0.3 && !d.Craving ? " The remedy is harmless by the assay; the bottle is not blamed."
			: " A strong bottle may have hastened it; nobody can say.";
		var q = f.Inquests.FirstOrDefault(x => x.Person.Id == d.Id);
		var sits = m.Letters.Any(l => l.Title.StartsWith("Inquest", StringComparison.Ordinal) && !d.Anonymous && d.Name.Length > 0 && l.Title.Contains(SurnameOf(d.Name), StringComparison.OrdinalIgnoreCase));
		var inquest = q != null ? $" The jury found: \u201c{q.Verdict}.\u201d" : sits ? " An inquest sits: see your letters." : " No inquest was called.";
		return cause + part + inquest;
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
		// the bench's titles give up a little to the larger type under them
		v.AddThemeConstantOverride("separation", LargePrint.Pick(5, 4, 3));
		v.AddChild(Print.Label(title, Print.Rye, LargePrint.Pick(17, 15, 15), HorizontalAlignment.Center));
		v.AddChild(Print.Rule(LargePrint.Pick(2.5f, 2f, 2f), 1, 2));
		return v;
	}
}
