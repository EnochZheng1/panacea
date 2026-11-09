using System.Collections.Generic;

namespace Panacea.Paper;

/// <summary>
/// Everything one edition of the paper may report, in player-visible terms only (no simulation
/// truth). Built from the simulation by <c>SimFacts</c>; kept separate so the writer and the
/// views do not depend on the simulation's types.
/// </summary>
public sealed class PaperFacts
{
    public string Seed = "";
    public int Turn;
    public int Year;
    public int Month;
    public bool FirstEdition;

    public long CashCents;
    public double Reputation;
    public double Heat;
    public bool CanAdvertise;
    public long AdSpendCents;

    public List<ProductFact> Products = new();
    public List<SaleFact> Sales = new();
    public List<PersonFact> Deaths = new();
    public List<InquestFact> Inquests = new();
    public List<PersonFact> Joined = new();
    public List<LeftFact> Left = new();
    public List<(string Headline, string Body)> Era = new();
    public List<(string Id, string Title, string Description)> Unlocks = new();
    public List<(string Product, int Bottles)> BadBatches = new();
    public int BottlesSeized;
    public bool AdsLapsed;
    public int ShutdownMonthsLeft;

    /// <summary>Regulars a reporter could quote: delighted, or unable to do without it.</summary>
    public List<PersonFact> Enthusiasts = new();
    public List<(string Name, long CostCents)> Markets = new();
    public bool Ended;
    public string? EndReason;
    /// <summary>The whole career, for the last edition: set only once the business has ended.</summary>
    public CareerFact? Career;

    /// <summary>The month's books by cause, as the Ledger keeps them.</summary>
    public long CashStartCents;
    /// <summary>
    /// The stock at cost (ingredients on hand, bottles brewing or on the shelf) now and when the
    /// month opened, so the books can show cash spent on stock as stock, not as a loss.
    /// </summary>
    public long StockValueCents;
    public long StockValueStartCents;
    /// <summary>The nearest things the shop can earn, with how far along it is (0..1).</summary>
    public List<(string Id, string Name, double Progress)> Prospects = new();
    /// <summary>Prospects the shop has earned but must still take up (the shop next door, offered in VENTURES).</summary>
    public HashSet<string> ProspectsReady = new();
    /// <summary>For a prospect, the count it waits on, in figures ("112 of 200 bottles sold").</summary>
    public Dictionary<string, string> ProspectCounts = new();
    /// <summary>The customer whose grateful letter was printed, if it was printed this month.</summary>
    public PersonFact? Testimonial;
    /// <summary>Bottles sold the month before, so the paper can tell a slump from a steady trade.</summary>
    public int SoldLastMonth;
    /// <summary>The most bottles sold in any month before this one.</summary>
    public int BestSoldBefore;
    /// <summary>What the ward remembers of the shop's harm (0–100), and its tally over the last year.</summary>
    public double WardMemory;
    public int RememberedDeaths, RememberedVerdicts, RememberedInfants;
    /// <summary>
    /// Callers who asked the price and left this month: walk-ins, and regulars come for their
    /// first bottle, who liked something on the shelf but would not pay what it cost (or went to a
    /// rival's cheaper bottle).
    /// </summary>
    public int TurnedAwayByPrice;
    /// <summary>Inquests whose bought verdict came out this month.</summary>
    public List<PersonFact> Exposed = new();
    public List<(string Label, long Cents)> CashLines = new();
    public List<(string Label, double Amount)> HeatLines = new();

    /// <summary>Investments bought, run out, lost or burned this month: change is bought, lapsed, lost or mishap.</summary>
    public List<(string Id, string Name, string Change)> InvestmentNews = new();
    /// <summary>Investments held when the edition went to press, with month-ends left (null: for good).</summary>
    public List<(string Id, string Name, int? MonthsLeft)> Held = new();
    /// <summary>
    /// Callers at the shop this month by where they came from: "ward", "newspaper", "subscriptions",
    /// and an investment id ("branch_shop", "almanac") for those it brought.
    /// </summary>
    public List<(string Source, int Callers)> Callers = new();
    /// <summary>Bottles the druggists out of town ordered, and got, this month, and what they paid.</summary>
    public int WholesaleOrdered;
    public int WholesaleDelivered;
    public long WholesaleRevenueCents;
    /// <summary>Laid out this month rather than lost to trade (ventures, a letter's price, bribes): an outlay, not a bad month.</summary>
    public long InvestedCents;
    /// <summary>Bottles sent by post to the almanac's readers this month, and what they brought.</summary>
    public int PostDelivered;
    public long PostRevenueCents;
    /// <summary>Flags the paper prints: gold_medal_1893 and radio_active (lines on the shop's notice), feeney_closed.</summary>
    public HashSet<string> Flags = new();
}

public sealed class ProductFact
{
    public string Id = "";
    public string Name = "";
    public long PriceCents;
    /// <summary>The price last month, when it was different (the markets report the change).</summary>
    public long? PrevPriceCents;
    public List<string> ClaimNames = new();
    public int Stock;
    /// <summary>The bottle as the formula bench assays it (Kick, Calm, Habit, Danger, Lingers).</summary>
    public double Kick, Calm, Habit, Danger, Accumulation;
    /// <summary>What a bottle costs to make at today's prices: ingredients and bottling.</summary>
    public long UnitCostCents;
}

/// <summary>A remedy's month: all its bottles, and of them the ones sold over the counter (regulars and callers); -1 if not known.</summary>
public sealed record SaleFact(string Product, int Bottles, long RevenueCents, int CounterBottles = -1);

public sealed class PersonFact
{
    public string Id = "";
    /// <summary>Full display name with title, e.g. "Mrs. Bridget Halloran". Empty if anonymous.</summary>
    public string Name = "";
    public string Segment = "";
    public string Ailment = "";
    public bool Infant;
    public bool Anonymous;
    /// <summary>A stranger out of town who drank a bottle sold by the wholesale druggists.</summary>
    public bool OutOfTown;
    public bool Female;
    /// <summary>How long they had been a regular (-1: never known at the shop).</summary>
    public int Months = -1;
    /// <summary>How they looked across the counter the last time they came in.</summary>
    public string LastSeen = "";
    /// <summary>For the dead: the cause as the ward and the register give it (overdose, wasting, ailment, old_age, natural).</summary>
    public string Cause = "";
    /// <summary>For the dead: the remedy whose bottle was theirs, if the shop's.</summary>
    public string? Product;
    /// <summary>Asking for the remedy by name, or shaking for want of it, when last seen.</summary>
    public bool Craving;
}

public sealed record InquestFact(PersonFact Person, string Outcome, string Verdict, string? Product);

public sealed record LeftFact(PersonFact Person, string Reason);

/// <summary>A career in figures, for the last edition's review of it.</summary>
public sealed class CareerFact
{
    public int Months;
    public int Bottles;
    public long RevenueCents;
    public int Deaths;
    public int Inquests;
    public int Named;
    public int Bribes;
    /// <summary>What the 1906 law makes the label declare, for the house's chief remedy ("Alcohol, 2 oz the bottle").</summary>
    public List<string> Declared = new();
    /// <summary>What the law did not ask after, though it was in the bottle (mercury, arsenic).</summary>
    public List<string> Undeclared = new();
}
