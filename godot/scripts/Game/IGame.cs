using System.Collections.Generic;
using Panacea.Paper;

namespace Panacea.Game;

/// <summary>
/// What the screens need from a running game. Implemented over the C# simulation by
/// <see cref="SimGame"/>. Every method that changes the game goes through the simulation's
/// actions, so the screens can never bend the rules.
/// </summary>
public interface IGame
{
	/// <summary>Facts for the edition the player is reading (the month just closed).</summary>
	PaperFacts Facts { get; }
	DeskModel Desk();
	bool Ended { get; }

	/// <summary>Each returns null on success, or the reason the action was refused.</summary>
	string? Buy(string ingredientId, double ounces);
	string? CreateFormula(string name, IReadOnlyList<(string IngredientId, double Ounces)> components, string processId, long priceCents);
	string? SetPrice(string formulaId, long priceCents);
	/// <summary>Quietly changes what goes in the bottle. Stock already brewed is unaffected.</summary>
	string? Reformulate(string formulaId, IReadOnlyList<(string IngredientId, double Ounces)> components, string processId);
	string? SetClaims(string formulaId, IReadOnlyList<string> claimIds);
	string? SetAdSpend(long cents);
	string? Brew(string formulaId, int bottles);
	string? Resolve(string instanceId, string choiceId);
	/// <summary>Buys an investment (a branch, an almanac, a wholesale contract, a laboratory).</summary>
	string? Invest(string investmentId);

	/// <summary>Analyses a draft recipe without changing the game.</summary>
	FormulaPreview Preview(IReadOnlyList<(string IngredientId, double Ounces)> components, string processId);

	/// <summary>Ends the month and prints the next edition.</summary>
	void GoToPress();
}

public sealed class DeskModel
{
	public int Turn;
	public int Year;
	public int Month;
	public long CashCents;
	public double Reputation;
	public double Heat;
	public int StillsFree;
	/// <summary>Stills the shop has, and the most bottles one makes a batch.</summary>
	public int Stills;
	public int StillBottles = 48;
	/// <summary>What the next rung of capacity waits for, in words ("the third still: 450 of 600 bottles sold").</summary>
	public string? NextCapacity;
	/// <summary>Of the last three months, how many closed with the shelf bare.</summary>
	public int ShelfBareMonths;
	public double BottleCapacityOunces;
	public bool CanAdvertise;
	public long AdSpendCents;
	public int Customers;
	public int CustomerCapacity;
	public List<IngredientRow> Ingredients = new();
	public List<(string Id, string Name)> Processes = new();
	public List<(string Id, string Name)> Claims = new();
	public int MaxClaims;
	public List<FormulaRow> Formulas = new();
	public List<(string Formula, int Bottles)> Brewing = new();
	public List<Letter> Letters = new();
	public List<(string Name, double Progress)> NextUnlocks = new();
	public List<Regular> Regulars = new();
	public long BottleCostCents;
	public Dictionary<string, double> ClaimBoldness = new();
	/// <summary>Every investment in the content, held or not, in content order.</summary>
	public List<InvestmentRow> Investments = new();
}

/// <summary>Something the proprietor can buy that pays back over months or years.</summary>
public sealed class InvestmentRow
{
	public string Id = "";
	public string Name = "";
	public string Description = "";
	/// <summary>Paid once, when bought (and again to renew one that has run out).</summary>
	public long CostCents;
	/// <summary>Paid every month-end while held (branch rent, the chemist's wages).</summary>
	public long UpkeepCents;
	/// <summary>How long it runs once bought, or null for good.</summary>
	public int? Months;
	public bool Held;
	/// <summary>Held, with a term: month-ends left, this one included.</summary>
	public int? MonthsLeft;
	public bool CanBuy;
	/// <summary>Why it cannot be bought now, as a sentence ("Not yet offered."), or null.</summary>
	public string? Refusal;
	/// <summary>Extra callers a month while held.</summary>
	public int WalkInsPerMonth;
	/// <summary>Suspicion a month per point of label boldness while held (the almanac prints the claims).</summary>
	public double HeatPerBoldness;
	/// <summary>Wholesale: bottles the druggists would order this month, at the shop's present name.</summary>
	public int? WholesaleBottles;
	/// <summary>Wholesale: what they would pay a bottle, for the bottle the shop has most of.</summary>
	public long? WholesalePriceCents;
	/// <summary>Orders by post (the almanac's readers): bottles a month, and what each brings.</summary>
	public int? PostBottles;
	public long? PostPriceCents;
	/// <summary>What it still waits for, in words ("1,120 of 1,200 bottles sold"); empty once held.</summary>
	public List<string> Progress = new();
	/// <summary>Chance of an accident each month while held (a laboratory fire).</summary>
	public double? MishapChance;
}

/// <summary>A regular as the proprietor sees them across the counter (never their true health).</summary>
public sealed class Regular
{
	public string Id = "";
	public string Name = "";
	public string Segment = "";
	public string Ailment = "";
	public bool Infant;
	/// <summary>hale, peaky, poorly or gravely_ill.</summary>
	public string Looks = "hale";
	/// <summary>delighted, content, grumbling or unhappy.</summary>
	public string Mood = "content";
	public bool Craving;
	public bool Withdrawal;
	/// <summary>Has spent beyond their means on the bottle: one bottle at most, and the family complains.</summary>
	public bool Ruined;
	public int MonthsAsCustomer;
	public int BottlesLastMonth;
}

public sealed class IngredientRow
{
	public string Id = "";
	public string Name = "";
	public string Description = "";
	public double OnHand;
	public long CostPerOunceCents;
	/// <summary>0..1: how much the drug colours the liquid (from the content's appearance).</summary>
	public double Opacity = 0.5;
	/// <summary>alcohol, opiate, cocaine, or empty.</summary>
	public string DrugClass = "";
}

public sealed class FormulaRow
{
	public string Id = "";
	public string Name = "";
	public long PriceCents;
	public long UnitCostCents;
	public int Stock;
	public int MaxBrewable;
	public List<string> ClaimIds = new();
	public Dictionary<string, double> Properties = new();
	public List<(string IngredientId, double Ounces)> Components = new();
	public string ProcessId = "simple_mix";
}

public sealed class Letter
{
	public string InstanceId = "";
	/// <summary>The kind of letter (content event id): decides the letterhead.</summary>
	public string EventId = "";
	public string Title = "";
	public string Body = "";
	/// <summary>Who wrote it, when the paper knows (a grateful customer signs her own name).</summary>
	public string? From;
	/// <summary>For an inquest: the chance the jury names your bottle, by reply id.</summary>
	public Dictionary<string, double> Odds = new();
	public List<(string Id, string Text)> Choices = new();
}

public sealed class FormulaPreview
{
	public string? Error;
	public long UnitCostCents;
	public Dictionary<string, double> Properties = new();
}
