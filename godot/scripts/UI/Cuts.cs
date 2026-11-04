namespace Panacea.UI;

/// <summary>
/// The composing room's stock of customer cuts. Each trade has a base engraving and two variants
/// (<c>_b</c>, <c>_c</c>: a different hat or hair, beard and build), so a row at the counter is not
/// three of the same man. The variant is chosen by a stable hash of the person, so a regular is
/// always printed from the same block.
/// </summary>
public static class Cuts
{
	private static readonly string[] Suffixes = { "", "_b", "_c" };

	/// <summary>The cut for a customer: the trade's base cut, then a variant fixed by <paramref name="personId"/>.</summary>
	public static string For(string segment, bool female, string personId)
	{
		var cut = BaseFor(segment, female);
		return cut + Suffixes[Fnv1a(personId ?? "") % (uint)Suffixes.Length];
	}

	/// <summary>The base cut for a variant: strips a trailing <c>_b</c> or <c>_c</c>.</summary>
	public static string Base(string cut)
	{
		if (cut != null && cut.Length > 2 && cut[^2] == '_' && (cut[^1] == 'b' || cut[^1] == 'c'))
			return cut[..^2];
		return cut;
	}

	/// <summary>Which base engraving stands for a trade and sex.</summary>
	public static string BaseFor(string segment, bool female) => segment switch
	{
		"mother" => "wc_mother",
		"labourer" or "dockworker" => "wc_longshoreman",
		"seamstress" or "clerk" => female ? "wc_seamstress" : "wc_clerk",
		"elderly" => female ? "wc_widow" : "wc_oldman",
		"well_to_do" => female ? "wc_lady" : "wc_oldman",
		// the widow is the stock fallback; a man of no listed trade gets the old gentleman
		_ => female ? "wc_widow" : "wc_oldman",
	};

	/// <summary>32-bit FNV-1a over the UTF-16 code units, low byte then high byte: stable across runs and platforms.</summary>
	private static uint Fnv1a(string s)
	{
		uint h = 2166136261;
		foreach (var ch in s)
		{
			h = (h ^ (byte)ch) * 16777619;
			h = (h ^ (byte)(ch >> 8)) * 16777619;
		}
		return h;
	}
}
