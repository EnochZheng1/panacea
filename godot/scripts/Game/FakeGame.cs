using System.Collections.Generic;
using Panacea.Paper;

namespace Panacea.Game;

/// <summary>
/// Canned game for layout work and screenshots (dev argument --fake). Has no rules: actions
/// succeed and do nothing, so it must never be used for play.
/// </summary>
public sealed class FakeGame : IGame
{
	private int _turn;

	public PaperFacts Facts { get; private set; } = Sample(0);
	public bool Ended => false;

	public static PaperFacts Sample(int turn)
	{
		var f = new PaperFacts { Seed = "fake", Turn = turn, Year = 1885 + turn / 12, Month = turn % 12 + 1, FirstEdition = turn == 0, CashCents = 13240, Reputation = 18, Heat = 12, CanAdvertise = turn > 0, AdSpendCents = turn > 0 ? 300 : 0 };
		f.Products.Add(new ProductFact { Id = "f1", Name = "Dr. ——'s Celebrated Panacea", PriceCents = 60, ClaimNames = { "Soothes the Nerves", "Cures Consumption" }, Stock = 14 });
		f.Markets.AddRange(new[] { ("Laudanum", 9L), ("Grain alcohol", 2L), ("Peppermint oil", 4L), ("Calomel", 6L) });
		if (turn == 0) return f;
		f.Sales.Add(new SaleFact("Dr. ——'s Celebrated Panacea", 46, 2760));
		var pike = new PersonFact { Id = "c7", Name = "Mr. Albert Pike", Segment = "clerk", Ailment = "A persistent cough" };
		var doyle = new PersonFact { Id = "c9", Name = "Mrs. Margaret Doyle", Segment = "mother", Ailment = "Nerves", Female = true };
		var anon = new PersonFact { Id = "d3", Segment = "dockworker", Anonymous = true };
		f.Deaths.AddRange(new[] { pike, anon });
		f.Inquests.Add(new InquestFact(pike, "bribed", "Died by the Visitation of God", null));
		f.Joined.Add(doyle);
		f.Left.Add(new LeftFact(new PersonFact { Id = "c2", Name = "Mr. Jan Kowalski", Segment = "labourer", Ailment = "Rheumatism" }, "dissatisfied"));
		f.Enthusiasts.Add(doyle);
		f.Era.Add(("New Coal-Tar Remedy for Fever", "German chemists offer acetanilide, sold as Antifebrin, for headache and fever."));
		f.Unlocks.Add(("newspaper_ads", "Newspaper Advertising", "With savings in hand and a testimonial to print, the papers will take your money."));
		return f;
	}

	public DeskModel Desk()
	{
		var d = new DeskModel { Turn = _turn, Year = Facts.Year, Month = Facts.Month, CashCents = 13240, Reputation = 18, Heat = 12, StillsFree = 1, BottleCapacityOunces = 8, CanAdvertise = _turn > 0, AdSpendCents = 300, Customers = 11, CustomerCapacity = 30, MaxClaims = 4 };
		foreach (var (id, name, cost) in new[] { ("water", "Water", 0L), ("grain_alcohol", "Grain alcohol", 2L), ("sugar_syrup", "Sugar syrup", 1L), ("peppermint_oil", "Oil of peppermint", 4L), ("laudanum", "Laudanum", 9L), ("gentian", "Gentian root", 3L) })
			d.Ingredients.Add(new IngredientRow { Id = id, Name = name, CostPerOunceCents = cost, OnHand = 16 });
		d.Processes.AddRange(new[] { ("simple_mix", "Simple Mixture"), ("steep", "Steeping") });
		d.Claims.AddRange(new[] { ("soothes_nerves", "Soothes the Nerves"), ("purifies_the_blood", "Purifies the Blood"), ("cures_consumption", "Cures Consumption") });
		d.Formulas.Add(new FormulaRow { Id = "f1", Name = "Dr. ——'s Celebrated Panacea", PriceCents = 60, UnitCostCents = 9, Stock = 14, MaxBrewable = 40, ClaimIds = { "soothes_nerves" }, Properties = new() { ["kick"] = 6.2, ["calm"] = 3.1, ["habit"] = 1.4, ["bitterness"] = 2.0, ["acuteToxicity"] = 0.9, ["accumulation"] = 0, ["detectability"] = 1.2 } });
		d.Letters.Add(new Letter { InstanceId = "e1", Title = "A Grateful Letter", Body = "A customer writes that your tonic restored her to health after three doctors failed. She would be honoured to see her words in print.", Choices = { ("print_testimonial", "Print it as a testimonial."), ("file_away", "File it away.") } });
		d.NextUnlocks.AddRange(new[] { ("A Second Still", 0.4), ("The Percolator", 0.2), ("\"Doctor\"", 0.1) });
		d.ClaimBoldness = new() { ["soothes_nerves"] = 0.5, ["purifies_the_blood"] = 1, ["cures_consumption"] = 3 };
		d.Regulars.AddRange(new[]
		{
			new Regular { Id = "r1", Name = "Mrs. Bridget Halloran", Segment = "mother", Ailment = "A teething infant", Infant = true, Looks = "peaky", Mood = "content", MonthsAsCustomer = 5 },
			new Regular { Id = "r2", Name = "Mr. Albert Pike", Segment = "clerk", Ailment = "A persistent cough", Looks = "gravely_ill", Mood = "delighted", Craving = true, MonthsAsCustomer = 5 },
			new Regular { Id = "r3", Name = "Mr. Jan Kowalski", Segment = "labourer", Ailment = "Rheumatism", Looks = "hale", Mood = "delighted", Withdrawal = true, MonthsAsCustomer = 4 },
			new Regular { Id = "r4", Name = "Miss Cornelia Vanderhoef", Segment = "well_to_do", Ailment = "Nerves", Looks = "hale", Mood = "content", MonthsAsCustomer = 0 },
		});
		return d;
	}

	public string? Buy(string ingredientId, double ounces) => null;
	public string? CreateFormula(string name, IReadOnlyList<(string IngredientId, double Ounces)> components, string processId, long priceCents) => null;
	public string? SetPrice(string formulaId, long priceCents) => null;
	public string? Reformulate(string formulaId, IReadOnlyList<(string IngredientId, double Ounces)> components, string processId) => null;
	public string? SetClaims(string formulaId, IReadOnlyList<string> claimIds) => null;
	public string? SetAdSpend(long cents) => null;
	public string? Brew(string formulaId, int bottles) => null;
	public string? Resolve(string instanceId, string choiceId) => null;
	public FormulaPreview Preview(IReadOnlyList<(string IngredientId, double Ounces)> components, string processId) =>
		new() { UnitCostCents = 8, Properties = new() { ["kick"] = 5, ["calm"] = 2, ["habit"] = 1, ["bitterness"] = 1.5, ["acuteToxicity"] = 0.5, ["accumulation"] = 0, ["detectability"] = 1 } };

	public void GoToPress()
	{
		_turn++;
		Facts = Sample(_turn);
	}
}
