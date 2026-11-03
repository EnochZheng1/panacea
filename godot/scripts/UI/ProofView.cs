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

	public ProofView()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
	}

	public void Bind(IGame game)
	{
		_game = game;
		_drawer = null;
		Rebuild();
	}

	private void Refresh() => Rebuild();

	/// <summary>Opens a tool over the proof: letters, still, formula or supplies.</summary>
	public void OpenDrawer(string which)
	{
		_drawer = which;
		Rebuild();
	}

	private string? _note;

	private void Act(string? error)
	{
		_note = error;
		Rebuild();
	}

	public void Rebuild()
	{
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
		bench.AddChild(TypeCase(m));
		bench.AddChild(LettersSlip(m));
		bench.AddChild(WorksSlip(m));
		bench.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });
		bench.AddChild(Lever());

		if (_drawer != null) leftStack.AddChild(Drawer(_drawer));
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

		var cols = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
		cols.AddThemeConstantOverride("separation", 0);
		page.AddChild(cols);

		// left column: what will be set after press
		var lc = Col(186);
		lc.AddChild(Head("DEATHS."));
		lc.AddChild(Print.Label("set after press", Print.FellIt, 11, HorizontalAlignment.Center));
		foreach (var w in new[] { 1f, 0.8f, 1f, 0.6f }) lc.AddChild(Ghost(w));
		lc.AddChild(Head("CORONER'S COURT."));
		var inquests = m.Letters.Where(l => l.Title.StartsWith("Inquest", StringComparison.Ordinal)).ToList();
		if (inquests.Count == 0) foreach (var w in new[] { 1f, 0.7f }) lc.AddChild(Ghost(w));
		foreach (var q in inquests)
			lc.AddChild(Print.Body($"{Print.Escape(q.Title)} sits this month. [i]See your letters.[/i]", 12));
		lc.AddChild(Head("PERSONAL."));
		foreach (var w in new[] { 1f, 0.75f, 1f }) lc.AddChild(Ghost(w));
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
			lv.AddChild(Print.Label("THE LEAD STORY", Print.Rye, 26, HorizontalAlignment.Center, new Color(Print.Ink, 0.45f)));
			lv.AddChild(Print.Body("[i]The editor sets it when the month is out. Whatever happens in the ward, it will be printed here.[/i]", 14, new Color(Print.Ink, 0.7f), centre: true));
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
		var box = new Dashed(8) { Accent = tier > 0 && product != null ? Print.Green : null };
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 3);
		var tag = Print.Panel(Print.Label($"YOUR SPACE · {AdSpace.Tiers[tier].Short}{(tier > 0 ? " · " + EditionWriter.Money(AdSpace.Tiers[tier].Cents) : "")}", Print.Rye, 11, HorizontalAlignment.Center, Print.Paper), Print.Box(Print.Ink, Print.Ink, 0, 3));
		v.AddChild(tag);
		if (product == null)
			v.AddChild(Print.Body("[i]Nothing to advertise. Enter a recipe in the formula book.[/i]", 13, centre: true));
		else if (!m.CanAdvertise)
			v.AddChild(Print.Body("[i]The Gazette will not take your money yet. Save a little, and get yourself a testimonial.[/i]", 13, centre: true));
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
		var atRisk = shown.FirstOrDefault(r => r.Looks is "gravely_ill" or "poorly");
		var others = m.Regulars.Count - shown.Count;
		var more = Print.Label(others > 0 ? $"and {others} more regulars" : m.Regulars.Count == 0 ? "No one has come in yet." : "", Print.FellIt, 13, wrap: false);
		more.HorizontalAlignment = HorizontalAlignment.Right;
		more.AnchorLeft = more.AnchorRight = 1;
		more.OffsetLeft = -300; more.OffsetRight = -14; more.OffsetTop = 28;
		strip.AddChild(more);

		// the strip is as wide as the proof column; share it out between the figures
		const float width = 1280f - 44f - 20f - BenchWidth, start = 54f;
		var slot = Math.Min(172f, (width - start - 70f) / Math.Max(shown.Count, 1));
		for (var i = 0; i < shown.Count; i++)
		{
			var r = shown[i];
			var x = start + i * slot;
			// Death stands at the shoulder of whoever is nearest the end, a head taller than anyone
			if (r == atRisk) AddFigure(strip, "wc_death", x - 44, 230, 1f);
			// the same stock cut twice in a row is printed the other way round
			var cut = CutFor(r);
			var flip = shown.Take(i).Count(o => CutFor(o) == cut) % 2 == 1;
			// people are not all one height, even when the printer owns one engraving of them
			var h = 176 + (int)(StableHash(r.Id) % 28u);
			AddFigure(strip, cut, x + 10, h, 1f, flip);
			var tag = Print.Slip(Tag(r), 5);
			tag.Position = new Vector2(x, CounterTop + 6);
			tag.CustomMinimumSize = new Vector2(slot - 10, 0);
			strip.AddChild(tag);
		}
		// nobody close to the end yet: he waits at the far end of the counter, faintly
		if (atRisk == null) AddFigure(strip, "wc_death", start + shown.Count * slot + 6, 230, 0.2f);
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

	private static void AddFigure(Control parent, string cut, float x, float height, float alpha, bool flip = false)
	{
		var tex = Print.Cut(cut);
		if (tex == null) return;
		var w = height * tex.GetWidth() / tex.GetHeight();
		parent.AddChild(new TextureRect
		{
			Texture = tex,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			Position = new Vector2(x, CounterTop - height + 12),
			Size = new Vector2(w, height),
			Modulate = new Color(1, 1, 1, alpha),
			FlipH = flip,
			MouseFilter = MouseFilterEnum.Ignore,
		});
	}

	private static string CutFor(Regular r) => CutFor(r.Segment, !r.Name.StartsWith("Mr."));

	/// <summary>Which stock cut the composing room uses for a customer.</summary>
	public static string CutFor(string segment, bool female) => segment switch
	{
		"mother" => "wc_mother",
		"labourer" or "dockworker" => "wc_longshoreman",
		"seamstress" => female ? "wc_seamstress" : "wc_clerk",
		"clerk" => female ? "wc_seamstress" : "wc_clerk",
		"elderly" => female ? "wc_widow" : "wc_oldman",
		"well_to_do" => female ? "wc_lady" : "wc_proprietor",
		_ => female ? "wc_widow" : "wc_oldman",
	};

	private static Control Tag(Regular r)
	{
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", -1);
		var name = Print.Label(r.Name, Print.Rye, 12, wrap: false);
		name.ClipText = true;
		name.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		name.TooltipText = r.Name;
		v.AddChild(name);
		v.AddChild(Print.Label((r.Infant ? "for " : "") + r.Ailment.ToLowerInvariant(), Print.Fell, 12, wrap: false));
		var status = new List<string>
		{
			r.Looks switch { "gravely_ill" => "gravely ill", "poorly" => "poorly", "peaky" => "peaky", _ => "hale" },
		};
		if (r.Withdrawal) status.Add("shaking");
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
		var product = m.Formulas.FirstOrDefault();
		if (product == null)
		{
			v.AddChild(Print.Body("[i]No remedy yet: open the formula book.[/i]", 13, justify: false));
			return Print.Slip(v, 10);
		}
		v.AddChild(Print.Body($"[i]Claims for {Print.Escape(product.Name)}. Bolder type sells more, and the coroner reads it too.[/i]", 12, justify: false));
		var flow = new HFlowContainer();
		flow.AddThemeConstantOverride("h_separation", 4);
		flow.AddThemeConstantOverride("v_separation", 4);
		foreach (var (id, name) in m.Claims)
		{
			var on = product.ClaimIds.Contains(id);
			var bold = m.ClaimBoldness.GetValueOrDefault(id);
			var weight = bold >= 3 ? " !!!" : bold >= 1.5 ? " !!" : bold >= 1 ? " !" : "";
			// long claims are set short on the slug; the full wording is in the tooltip
			var slug = name.Length > 26 ? name[..24].TrimEnd(' ', ',') + "…" : name;
			var b = Print.Button(slug.ToUpperInvariant() + weight, 11, solid: on);
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
		v.AddChild(Print.Label("SPACE IN THE GAZETTE", Print.Rye, 12));
		var sizes = new HBoxContainer();
		sizes.AddThemeConstantOverride("separation", 4);
		var tier = AdSpace.TierFor(m.AdSpendCents);
		for (var i = 0; i < AdSpace.Tiers.Length; i++)
		{
			var t = AdSpace.Tiers[i];
			var b = Print.Button(t.Short, 11, solid: m.CanAdvertise && i == tier);
			b.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			b.TooltipText = $"{t.Name}: {EditionWriter.Money(t.Cents)} a month";
			b.Disabled = !m.CanAdvertise;
			var cents = t.Cents;
			b.Pressed += () => Act(_game.SetAdSpend(cents));
			sizes.AddChild(b);
		}
		v.AddChild(sizes);
		if (!m.CanAdvertise) v.AddChild(Print.Body("[i]Not yet: the Gazette wants savings and a testimonial first.[/i]", 12, justify: false));
		if (_note != null) v.AddChild(Print.Body($"[b]Refused.[/b] [i]{Print.Escape(_note)}[/i]", 12, justify: false));
		return Print.Slip(v, 10);
	}

	private Control LettersSlip(DeskModel m)
	{
		var v = SlipBody(m.Letters.Count == 0 ? "NO LETTERS" : m.Letters.Count == 1 ? "1 LETTER" : $"{m.Letters.Count} LETTERS");
		if (m.Letters.Count == 0) v.AddChild(Print.Body("[i]Nothing in the post but a circular for galvanic belts.[/i]", 13, justify: false));
		foreach (var l in m.Letters.Take(2)) v.AddChild(Print.Body($"[b]{Print.Escape(l.Title)}.[/b]", 13, justify: false));
		if (m.Letters.Count > 0)
		{
			var open = Print.Button("OPEN THE LETTERS", 13, solid: true);
			open.Pressed += () => { _drawer = "letters"; Rebuild(); };
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
		left.AddChild(Print.Label($"{stock} bottles on the shelf", Print.Fell, 14));
		left.AddChild(Print.Label(brewing > 0 ? $"{brewing} brewing" : m.StillsFree > 0 ? "the still is cold" : "the still is busy", Print.FellIt, 13));
		grid.AddChild(left);
		var books = new VBoxContainer();
		books.AddThemeConstantOverride("separation", -2);
		books.AddChild(Print.Label(EditionWriter.Money(m.CashCents), Print.Rye, 22, HorizontalAlignment.Right, wrap: false));
		books.AddChild(Print.Label($"talk: {DeskView.Talk(m.Heat)}", Print.FellIt, 12, HorizontalAlignment.Right, wrap: false));
		grid.AddChild(books);
		v.AddChild(grid);
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 4);
		foreach (var (key, label) in new[] { ("still", "THE STILL"), ("formula", "FORMULA BOOK"), ("supplies", "SUPPLIES") })
		{
			var b = Print.Button(label, 12);
			b.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			var k = key;
			b.Pressed += () => { _drawer = k; Rebuild(); };
			row.AddChild(b);
		}
		v.AddChild(row);
		return Print.Slip(v, 10);
	}

	private Control Lever()
	{
		var b = Print.Button("SEND TO PRESS ▸", 28, solid: true);
		b.CustomMinimumSize = new Vector2(0, 84);
		b.TooltipText = "End the month. The lead story is written by what happens.";
		b.AddThemeStyleboxOverride("normal", Print.Box(Print.Ink, Print.Paper, 3, 10));
		b.AddThemeStyleboxOverride("hover", Print.Box(Print.Paper, Print.Paper, 3, 10));
		b.AddThemeColorOverride("font_hover_color", Print.Ink);
		b.Pressed += () => PressRequested?.Invoke();
		return b;
	}

	// ---------- drawers: a tool laid over the proof ----------

	private Control Drawer(string which)
	{
		var shade = new ColorRect { Color = new Color(0, 0, 0, 0.55f) };
		shade.SetAnchorsPreset(LayoutPreset.FullRect);
		var holder = new VBoxContainer();
		holder.SetAnchorsPreset(LayoutPreset.FullRect);
		holder.OffsetLeft = 40; holder.OffsetRight = -40; holder.OffsetTop = 10; holder.OffsetBottom = -6;
		var close = Print.Button("CLOSE ✕", 14, solid: true);
		close.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
		close.AddThemeStyleboxOverride("normal", Print.Box(Print.Ink, Print.Paper, 2, 6));
		close.Pressed += () => { _drawer = null; Rebuild(); };
		holder.AddChild(close);
		var desk = new DeskView { Only = which, SizeFlagsVertical = SizeFlags.ExpandFill };
		desk.SetAnchorsPreset(LayoutPreset.TopLeft);
		desk.Changed += Rebuild;
		holder.AddChild(desk);
		desk.Bind(_game);
		shade.AddChild(holder);
		shade.GuiInput += e => { if (e is InputEventMouseButton { Pressed: true } mb && mb.ButtonIndex == MouseButton.Left && !holder.GetGlobalRect().HasPoint(mb.GlobalPosition)) { _drawer = null; CallDeferred(nameof(Refresh)); } };
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
