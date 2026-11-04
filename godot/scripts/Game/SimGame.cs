using System;
using System.Collections.Generic;
using System.Linq;
using Panacea.Paper;
using Panacea.Sim;

namespace Panacea.Game;

/// <summary>
/// The screens' view of a real game, over the C# simulation (sim-cs/Panacea.Sim). It reads only
/// what <see cref="Machine.Observe"/> exposes (never the simulation's hidden truth), and every
/// change goes through a simulation action.
/// </summary>
public sealed class SimGame : IGame
{
	private readonly ContentPack _content;
	private readonly GameState _state;
	private readonly string _seed;

	public PaperFacts Facts { get; private set; }
	public bool Ended => _state.Meta.Status != GameStatus.Running;

	private SimGame(ContentPack content, GameState state, string seed)
	{
		_content = content;
		_state = state;
		_seed = seed;
		Facts = BuildFacts(null);
	}

	public static IGame Start(string seed)
	{
		var content = Content.LoadEmbedded();
		var state = Sim.Game.NewGame(content, new NewGameOptions { Seed = seed });
		return new SimGame(content, state, seed);
	}

	// ---------- actions ----------

	private string? Do(GameAction action)
	{
		var r = Sim.Game.ApplyActionInPlace(_state, action, _content);
		return r.Ok ? null : Sentence(r.Error);
	}

	private static string? Sentence(string? error) =>
		string.IsNullOrEmpty(error) ? error : char.ToUpperInvariant(error[0]) + error[1..] + (error.EndsWith('.') ? "" : ".");

	public string? Buy(string ingredientId, double ounces) => Do(new BuyIngredient(ingredientId, (int)Math.Round(ounces)));

	public string? CreateFormula(string name, IReadOnlyList<(string IngredientId, double Ounces)> components, string processId, long priceCents) =>
		Do(new CreateFormula(name, Components(components), processId, (int)priceCents));

	public string? SetPrice(string formulaId, long priceCents) => Do(new SetPrice(formulaId, (int)priceCents));
	public string? Reformulate(string formulaId, IReadOnlyList<(string IngredientId, double Ounces)> components, string processId) =>
		Do(new Reformulate(formulaId, Components(components), processId));
	public string? SetClaims(string formulaId, IReadOnlyList<string> claimIds) => Do(new SetClaims(formulaId, claimIds.ToList()));
	public string? SetAdSpend(long cents) => Do(new SetAdSpend((int)cents));
	public string? Brew(string formulaId, int bottles) => Do(new BrewBatch(formulaId, bottles));
	public string? Resolve(string instanceId, string choiceId) => Do(new ResolveEvent(instanceId, choiceId));

	public FormulaPreview Preview(IReadOnlyList<(string IngredientId, double Ounces)> components, string processId)
	{
		var comps = Components(components);
		var error = Formulas.Validate(comps, processId, _state, _content);
		if (error != null) return new FormulaPreview { Error = Sentence(error) };
		var a = Formulas.Analyze(comps, processId, _content);
		return new FormulaPreview { UnitCostCents = (long)Math.Ceiling(a.UnitCostCents), Properties = Props(a.Properties) };
	}

	/// <summary>Everyone ever seen at the counter, as last seen (the paper remembers them).</summary>
	private readonly Dictionary<string, Regular> _known = new();

	public void GoToPress()
	{
		if (Ended) return;
		foreach (var r in Desk().Regulars) _known[r.Name] = r;
		var before = Facts.Sales.Sum(x => x.Bottles);
		var report = Sim.Game.EndTurnInPlace(_state, _content);
		Facts = BuildFacts(report);
		Facts.SoldLastMonth = before;
	}

	private static List<FormulaComponent> Components(IReadOnlyList<(string IngredientId, double Ounces)> components) =>
		components.Where(c => c.Ounces > 0).Select(c => new FormulaComponent(c.IngredientId, c.Ounces)).ToList();

	private static Dictionary<string, double> Props(Properties p) => Properties.Keys.ToDictionary(k => k, k => p[k]);

	// ---------- the desk ----------

	public DeskModel Desk()
	{
		var o = Machine.Observe(_state, _content);
		var specs = Machine.LegalActions(_state, _content);
		var maxBrew = specs.OfType<BrewBatchSpec>().ToDictionary(s => s.FormulaId, s => s.MaxBottles);
		var claimSpec = specs.OfType<SetClaimsSpec>().FirstOrDefault();
		var d = new DeskModel
		{
			Turn = o.Turn,
			Year = o.Date.Year,
			Month = o.Date.Month,
			CashCents = o.CashCents,
			Reputation = o.Reputation,
			Heat = o.Heat,
			StillsFree = o.StillsFree,
			BottleCapacityOunces = _content.Config.BottleCapacity,
			BottleCostCents = _content.Config.BottleCostCents,
			CanAdvertise = o.CanAdvertise,
			AdSpendCents = o.AdSpendCents,
			Customers = o.Customers.Count,
			CustomerCapacity = o.CustomerCapacity,
			MaxClaims = claimSpec?.MaxClaims ?? _content.Config.MaxClaimsPerFormula,
		};
		foreach (var i in o.Ingredients)
		{
			o.Inventory.TryGetValue(i.Id, out var onHand);
			d.Ingredients.Add(new IngredientRow
			{
				Id = i.Id,
				Name = _content.Text($"ingredient.{i.Id}.name"),
				Description = _content.Text($"ingredient.{i.Id}.description"),
				OnHand = onHand,
				CostPerOunceCents = (long)Math.Round(i.CostPerUnitCents),
				Opacity = _content.ById.Ingredients.TryGetValue(i.Id, out var ing) ? ing.Appearance.Opacity : 0.5,
				DrugClass = ing?.DrugClass ?? "",
			});
		}
		foreach (var p in o.Processes) d.Processes.Add((p, _content.Text($"process.{p}.name")));
		foreach (var c in o.Claims)
		{
			d.Claims.Add((c, _content.Text($"claim.{c}.name")));
			d.ClaimBoldness[c] = _content.ById.Claims.TryGetValue(c, out var claim) ? claim.Boldness : 0;
		}
		foreach (var c in o.Customers)
		{
			d.Regulars.Add(new Regular
			{
				Id = c.Id, Name = c.Name, Segment = c.Segment, Ailment = _content.Text($"ailment.{c.Ailment}.name"),
				Infant = c.Consumer == "infant", Looks = c.Looks, Mood = c.Mood, Craving = c.Craving, Withdrawal = c.Withdrawal, Ruined = c.Ruined,
				MonthsAsCustomer = c.MonthsAsCustomer, BottlesLastMonth = c.BottlesLastMonth,
			});
		}
		foreach (var f in o.Formulas)
		{
			d.Formulas.Add(new FormulaRow
			{
				Id = f.Id,
				Name = f.Name,
				PriceCents = f.PriceCents,
				UnitCostCents = (long)Math.Ceiling(f.UnitCostCents),
				Stock = f.Stock,
				MaxBrewable = maxBrew.GetValueOrDefault(f.Id),
				ClaimIds = f.Claims.ToList(),
				Properties = Props(f.Properties),
				Components = _state.Lab.Formulas.First(x => x.Id == f.Id).Components.Select(c => (c.IngredientId, c.Amount)).ToList(),
				ProcessId = _state.Lab.Formulas.First(x => x.Id == f.Id).ProcessId,
			});
		}
		foreach (var b in o.BatchesInProgress)
			d.Brewing.Add((o.Formulas.FirstOrDefault(f => f.Id == b.FormulaId)?.Name ?? b.FormulaId, b.Bottles));
		foreach (var e in o.PendingEvents)
		{
			// an inquest into an infant names the infant, not the parent who bought the bottle
			var dead = e.DeathId == null ? null : o.Deaths.FirstOrDefault(x => x.Id == e.DeathId);
			var subject = string.IsNullOrWhiteSpace(e.SubjectName) || dead is { Anonymous: true } ? Unknown(dead?.Segment)
				: dead is { Consumer: "infant" } ? $"the Infant of {e.SubjectName}" : e.SubjectName;
			string Sub(string s) => s.Replace("{name}", subject ?? "a customer")
				.Replace("{cost}", e.BribeCostCents is long c ? EditionWriter.Money(c) : "");
			var letter = new Letter
			{
				InstanceId = e.InstanceId,
				EventId = e.EventId,
				Title = Sub(_content.Text($"event.{e.EventId}.title")),
				Body = Sub(_content.Text($"event.{e.EventId}.body")),
			};
			foreach (var c in e.ChoiceIds) letter.Choices.Add((c, Sub(_content.Text($"event.{e.EventId}.choice.{c}"))));
			// the jury's odds, as the sim will roll them the moment the reply is sent
			var record = e.DeathId == null ? null : _state.Deaths.Find(x => x.Id == e.DeathId);
			if (record != null && e.EventId == "inquest")
				foreach (var (c, approach) in new[] { ("testify", "testify"), ("stay_silent", "silent") })
					letter.Odds[c] = Scrutiny.ImplicationChance(_state, record, approach);
			if (record != null && e.EventId == "inquest")
				letter.Odds["bribe_coroner"] = Scrutiny.BribeExposureChance(_state, _content);
			d.Letters.Add(letter);
		}
		foreach (var u in o.NextUnlocks) d.NextUnlocks.Add((_content.Text($"unlock.{u.Id}.name"), u.Progress));
		return d;
	}

	/// <summary>An unknown dead customer, described as the deaths column describes them.</summary>
	private static string Unknown(string? segment) => segment switch
	{
		"labourer" => "an Unknown Labouring Man",
		"dockworker" => "an Unknown Longshoreman",
		"mother" => "an Unknown Young Mother",
		"elderly" => "an Unknown Elderly Person",
		"clerk" => "an Unknown Clerk",
		"seamstress" => "an Unknown Seamstress",
		"well_to_do" => "an Unknown Person of Means",
		_ => "a Person Unknown",
	};

	// ---------- the paper ----------

	private PaperFacts BuildFacts(LedgerEntry? report)
	{
		var o = Machine.Observe(_state, _content);
		// the edition reports the month just closed; the first edition, the day the shop opened
		var date = report?.Date ?? o.Date;
		var f = new PaperFacts
		{
			Seed = _seed,
			Turn = report?.Turn ?? -1,
			Year = date.Year,
			Month = date.Month,
			FirstEdition = report == null,
			CashCents = o.CashCents,
			Reputation = o.Reputation,
			Heat = o.Heat,
			CanAdvertise = o.CanAdvertise,
			AdSpendCents = o.AdSpendCents,
			Ended = Ended,
			EndReason = _state.Meta.EndReason,
			StockValueCents = o.StockValue.TotalCents,
			StockValueStartCents = report?.StockValueStartCents ?? o.StockValue.TotalCents,
		};
		if (f.FirstEdition) f.Turn = 0;
		else f.Turn = report!.Turn + 1;

		foreach (var p in o.Formulas)
		{
			f.Products.Add(new ProductFact
			{
				Id = p.Id,
				Name = p.Name,
				PriceCents = p.PriceCents,
				Stock = p.Stock,
				ClaimNames = p.Claims.Select(c => _content.Text($"claim.{c}.name")).ToList(),
			});
		}
		foreach (var i in o.Ingredients.Where(i => i.CostPerUnitCents > 0).OrderByDescending(i => i.CostPerUnitCents).Take(4))
			f.Markets.Add((_content.Text($"ingredient.{i.Id}.name"), (long)Math.Round(i.CostPerUnitCents)));
		foreach (var c in o.Customers.Where(c => c.Mood == "delighted" || c.Craving))
			f.Enthusiasts.Add(Person(c));

		if (report != null)
		{
			f.CashStartCents = report.CashStartCents;
			f.TurnedAwayByPrice = report.TurnedAwayByPrice;
			foreach (var g in report.Deltas.Where(x => x.Metric == "cash").GroupBy(x => x.Cause))
				f.CashLines.Add((_content.Text($"cause.{g.Key}"), (long)Math.Round(g.Sum(x => x.Amount))));
			foreach (var g in report.Deltas.Where(x => x.Metric == "heat").GroupBy(x => x.Cause))
				f.HeatLines.Add((_content.Text($"cause.{g.Key}"), g.Sum(x => x.Amount)));
		}

		if (report == null)
		{
			foreach (var c in o.Customers.Take(4)) f.Joined.Add(Person(c));
			return f;
		}

		string FormulaName(string? id) => o.Formulas.FirstOrDefault(x => x.Id == id)?.Name ?? "Dr. ——'s remedy";
		foreach (var s in report.Sales.GroupBy(s => s.FormulaId))
			f.Sales.Add(new SaleFact(FormulaName(s.Key), s.Sum(x => x.Bottles), s.Sum(x => x.RevenueCents)));

		foreach (var n in report.News)
		{
			switch (n)
			{
				case DeathNews dn:
				{
					var d = o.Deaths.FirstOrDefault(x => x.Id == dn.DeathId);
					if (d != null) f.Deaths.Add(Person(d));
					break;
				}
				case BribeExposedNews bx:
				{
					var d = o.Deaths.FirstOrDefault(x => x.Id == bx.DeathId);
					if (d != null) f.Exposed.Add(Person(d));
					break;
				}
				case InquestNews q:
				{
					var d = o.Deaths.FirstOrDefault(x => x.Id == q.DeathId);
					if (d == null) break;
					var verdict = _content.Strings.ContainsKey($"verdict.{q.Verdict}") ? _content.Text($"verdict.{q.Verdict}") : q.Verdict;
					f.Inquests.Add(new InquestFact(Person(d), q.Outcome, verdict, d.FormulaId == null ? null : FormulaName(d.FormulaId)));
					break;
				}
				case CustomerJoinedNews j:
				{
					var c = o.Customers.FirstOrDefault(x => x.Id == j.CustomerId);
					if (c != null) f.Joined.Add(Person(c));
					break;
				}
				case CustomerLeftNews l:
					f.Left.Add(new LeftFact(Remembered(new PersonFact { Id = l.CustomerId, Name = l.Name, Female = IsFemale(l.Name) }), l.Reason));
					break;
				case TimelineNews t:
					f.Era.Add((_content.Text($"timeline.{t.Id}.headline"), _content.Text($"timeline.{t.Id}.body")));
					break;
				case UnlockNews u:
					f.Unlocks.Add((u.Id, _content.Text($"unlock.{u.Id}.name"), _content.Text($"unlock.{u.Id}.description")));
					break;
				case BadBatchCaughtNews b:
					f.BadBatches.Add((FormulaName(b.FormulaId), b.Bottles));
					break;
				case StockSeizedNews s:
					f.BottlesSeized += s.Bottles;
					break;
				case AdsLapsedNews:
					f.AdsLapsed = true;
					break;
				case ShutdownWarningNews w:
					f.ShutdownMonthsLeft = w.MonthsLeft;
					break;
			}
		}
		return f;
	}

	public static string Describe(Regular r)
	{
		var looks = r.Looks switch { "gravely_ill" => "gravely ill", "poorly" => "poorly", "peaky" => "a little peaky", _ => "hale" };
		var extra = r.Withdrawal ? "shaking for want of a dose" : r.Craving ? "asking for it by name" : r.Mood switch
		{
			"delighted" => "delighted with it", "unhappy" => "unhappy with it", "grumbling" => "grumbling", _ => "content",
		};
		return $"{looks}, {extra}";
	}

	private PersonFact Remembered(PersonFact p)
	{
		if (!_known.TryGetValue(p.Name, out var r)) return p;
		p.Months = r.MonthsAsCustomer;
		p.LastSeen = Describe(r);
		if (string.IsNullOrEmpty(p.Segment)) p.Segment = r.Segment;
		if (string.IsNullOrEmpty(p.Ailment)) p.Ailment = r.Ailment;
		return p;
	}

	private PersonFact Person(ObservedCustomer c) => new()
	{
		Id = c.Id,
		Name = c.Name,
		Segment = c.Segment,
		Ailment = _content.Text($"ailment.{c.Ailment}.name"),
		Infant = c.Consumer == "infant",
		Female = IsFemale(c.Name),
		Months = c.MonthsAsCustomer,
		LastSeen = Describe(new Regular { Looks = c.Looks, Mood = c.Mood, Craving = c.Craving, Withdrawal = c.Withdrawal }),
	};

	private PersonFact Person(ObservedDeath d) => Remembered(new PersonFact
	{
		Id = d.Id,
		Name = d.Name,
		Segment = d.Segment,
		Infant = d.Consumer == "infant",
		Anonymous = d.Anonymous,
		Female = IsFemale(d.Name),
	});

	private static bool IsFemale(string name) => name.StartsWith("Mrs.") || name.StartsWith("Miss") || name.StartsWith("Widow") || name.StartsWith("Mother");
}
