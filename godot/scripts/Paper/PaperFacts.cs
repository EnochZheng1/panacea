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
    public List<(string Title, string Description)> Unlocks = new();
    public List<(string Product, int Bottles)> BadBatches = new();
    public int BottlesSeized;
    public bool AdsLapsed;
    public int ShutdownMonthsLeft;

    /// <summary>Regulars a reporter could quote: delighted, or unable to do without it.</summary>
    public List<PersonFact> Enthusiasts = new();
    public List<(string Name, long CostCents)> Markets = new();
    public bool Ended;
    public string? EndReason;
}

public sealed class ProductFact
{
    public string Id = "";
    public string Name = "";
    public long PriceCents;
    public List<string> ClaimNames = new();
    public int Stock;
}

public sealed record SaleFact(string Product, int Bottles, long RevenueCents);

public sealed class PersonFact
{
    public string Id = "";
    /// <summary>Full display name with title, e.g. "Mrs. Bridget Halloran". Empty if anonymous.</summary>
    public string Name = "";
    public string Segment = "";
    public string Ailment = "";
    public bool Infant;
    public bool Anonymous;
    public bool Female;
}

public sealed record InquestFact(PersonFact Person, string Outcome, string Verdict, string? Product);

public sealed record LeftFact(PersonFact Person, string Reason);
