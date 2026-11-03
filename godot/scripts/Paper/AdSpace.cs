namespace Panacea.Paper;

/// <summary>
/// The space a proprietor can buy in the Gazette. The simulation only knows the monthly spend;
/// the paper turns it into a size and a position on the page. The front-page tier prints your
/// notice as the lead story ("reading notice"), the period's most effective and least honest ad.
/// </summary>
public static class AdSpace
{
	public static readonly (string Name, string Short, long Cents)[] Tiers =
	{
		("No notice", "NONE", 0),
		("A quarter column", "¼ COL", 300),
		("Half a column", "½ COL", 600),
		("A full column", "FULL COL", 1100),
		("The front page, as news", "FRONT", 2000),
	};

	public const int Front = 4;

	/// <summary>The largest tier a monthly spend pays for.</summary>
	public static int TierFor(long cents)
	{
		var t = 0;
		for (var i = 0; i < Tiers.Length; i++) if (cents >= Tiers[i].Cents) t = i;
		return t;
	}
}
