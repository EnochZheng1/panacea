using System;
using System.Collections.Generic;
using Godot;

namespace Panacea.UI;

/// <summary>
/// Frank Liang's hyphenation, as TeX does it, with the American English patterns shipped in
/// data/hyph (hyph_en_US.dic: the plain TeX table, BSD-style licence, see its README). Words are
/// broken only where the patterns allow, leaving at least two letters before the break and three
/// after it. Proper names (a capital followed by lower case) are never broken.
/// </summary>
public static class Hyphenator
{
	private const int LeftMin = 2, RightMin = 3;
	private static Dictionary<string, int[]>? _patterns;
	private static int _maxLen;

	private static Dictionary<string, int[]> Patterns
	{
		get
		{
			if (_patterns != null) return _patterns;
			_patterns = new Dictionary<string, int[]>();
			using var f = FileAccess.Open("res://data/hyph/hyph_en_US.dic", FileAccess.ModeFlags.Read);
			if (f == null) return _patterns;
			while (!f.EofReached())
			{
				var line = f.GetLine().Trim();
				// the header (charset, minimums) and any non-standard entries are skipped
				if (line.Length == 0 || line.Contains('/') || char.IsUpper(line[0])) continue;
				var letters = new System.Text.StringBuilder();
				var values = new List<int> { 0 };
				foreach (var ch in line)
				{
					if (char.IsDigit(ch)) values[^1] = ch - '0';
					else { letters.Append(ch); values.Add(0); }
				}
				var key = letters.ToString();
				_patterns[key] = values.ToArray();
				_maxLen = Math.Max(_maxLen, key.Length);
			}
			return _patterns;
		}
	}

	/// <summary>Positions (indices into the word) before which a hyphen may be set.</summary>
	public static List<int> Points(string word)
	{
		var points = new List<int>();
		if (word.Length < LeftMin + RightMin) return points;
		// a capital then lower case is a name: the compositor carries it whole
		if (char.IsUpper(word[0]) && word.Length > 1 && char.IsLower(word[1])) return points;
		var p = Patterns;
		var w = "." + word.ToLowerInvariant() + ".";
		var levels = new int[w.Length + 1];
		for (var i = 0; i < w.Length; i++)
			for (var len = 1; len <= Math.Min(_maxLen, w.Length - i); len++)
				if (p.TryGetValue(w.Substring(i, len), out var v))
					for (var k = 0; k < v.Length; k++)
						levels[i + k] = Math.Max(levels[i + k], v[k]);
		// levels[i + 1] is the value between word[i - 1] and word[i]
		for (var i = LeftMin; i <= word.Length - RightMin; i++)
			if (levels[i + 1] % 2 == 1) points.Add(i);
		return points;
	}
}
