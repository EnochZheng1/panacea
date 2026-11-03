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
	string? SetClaims(string formulaId, IReadOnlyList<string> claimIds);
	string? SetAdSpend(long cents);
	string? Brew(string formulaId, int bottles);
	string? Resolve(string instanceId, string choiceId);

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
}

public sealed class IngredientRow
{
	public string Id = "";
	public string Name = "";
	public string Description = "";
	public double OnHand;
	public long CostPerOunceCents;
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
}

public sealed class Letter
{
	public string InstanceId = "";
	public string Title = "";
	public string Body = "";
	public List<(string Id, string Text)> Choices = new();
}

public sealed class FormulaPreview
{
	public string? Error;
	public long UnitCostCents;
	public Dictionary<string, double> Properties = new();
}
