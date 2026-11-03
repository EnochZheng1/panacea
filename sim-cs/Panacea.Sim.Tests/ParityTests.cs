using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Panacea.Sim.Tests;

/// <summary>
/// Bit-for-bit parity with the TypeScript reference sim. Each fixture is a full bot playthrough
/// recorded by packages/playtest/src/cli/parity-fixtures.ts: the options, the replay log, the
/// state hash after newGame and after every endTurn, per-section hashes, and full state
/// snapshots at a few checkpoints. The C# sim replays the log and must reproduce every hash.
/// </summary>
public class ParityTests(ITestOutputHelper output)
{
    private static readonly string[] Sections =
        ["meta", "player", "lab", "marketing", "customers", "deaths", "scrutiny", "stats", "unlocks", "events", "timeline", "month"];

    public static IEnumerable<object[]> Runs => Fixtures.RunFiles();

    private static Dictionary<string, string> SectionHashes(GameState s) => new()
    {
        ["meta"] = Hash.HashValue(s.Meta),
        ["player"] = Hash.HashValue(s.Player),
        ["lab"] = Hash.HashValue(s.Lab),
        ["marketing"] = Hash.HashValue(s.Marketing),
        ["customers"] = Hash.HashValue(s.Customers),
        ["deaths"] = Hash.HashValue(s.Deaths),
        ["scrutiny"] = Hash.HashValue(s.Scrutiny),
        ["stats"] = Hash.HashValue(s.Stats),
        ["unlocks"] = Hash.HashValue(s.Unlocks),
        ["events"] = Hash.HashValue(s.Events),
        ["timeline"] = Hash.HashValue(s.Timeline),
        ["month"] = Hash.HashValue(s.Month),
        ["ledgerLast"] = Hash.HashValue(s.Ledger.Count > 0 ? s.Ledger[^1] : null),
        ["ledgerLength"] = s.Ledger.Count.ToString(),
    };

    private sealed class Run
    {
        public required string File;
        public required NewGameOptions Options;
        public required List<LogEntry> Log;
        public required List<string> Hashes;
        public required List<Dictionary<string, string>> SectionHashes;
        public required SortedDictionary<int, string> Checkpoints;
        public required string? EndReason;
        public required int Turns;
    }

    private static Run Load(string file)
    {
        using var doc = Fixtures.ReadJson(file);
        var r = doc.RootElement;
        var opts = r.GetProperty("options");
        return new Run
        {
            File = file,
            Options = new NewGameOptions
            {
                Seed = opts.GetProperty("seed").GetString()!,
                BackgroundId = opts.TryGetProperty("backgroundId", out var bg) ? bg.GetString() : null,
            },
            Log = r.GetProperty("log").Deserialize<List<LogEntry>>(StateJson.Options)!,
            Hashes = r.GetProperty("hashes").EnumerateArray().Select(h => h.GetString()!).ToList(),
            SectionHashes = r.GetProperty("sections").EnumerateArray()
                .Select(s => s.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!)).ToList(),
            Checkpoints = new SortedDictionary<int, string>(r.GetProperty("checkpoints").EnumerateObject()
                .ToDictionary(p => int.Parse(p.Name), p => p.Value.GetString()!)),
            EndReason = r.GetProperty("endReason").GetString(),
            Turns = r.GetProperty("turns").GetInt32(),
        };
    }

    /// <summary>Replays a fixture, yielding the state after newGame (index 0) and after each endTurn.</summary>
    private static IEnumerable<(int Index, GameState State)> Replay(Run run)
    {
        var content = Fixtures.Content;
        var state = Game.NewGame(content, run.Options);
        int index = 0;
        yield return (index, state);
        foreach (var entry in run.Log)
        {
            if (entry.IsEndTurn)
            {
                Game.EndTurnInPlace(state, content);
                index++;
                yield return (index, state);
            }
            else
            {
                var result = Game.ApplyActionInPlace(state, entry.Action!, content);
                if (!result.Ok)
                    throw new InvalidOperationException(
                        $"after {index} endTurns: logged action {entry.Action!.Type} failed in C#: {result.Error}");
            }
        }
    }

    [Theory]
    [MemberData(nameof(Runs))]
    public void ReplayMatchesTypeScriptHashes(string file)
    {
        var run = Load(file);
        var content = Fixtures.Content;
        int matched = 0;
        GameState? last = null;
        foreach (var (index, state) in Replay(run))
        {
            last = state;
            string hash = Game.HashState(state);
            if (index >= run.Hashes.Count) Assert.Fail($"{file}: C# replay produced more months than the fixture");
            if (hash != run.Hashes[index])
            {
                Assert.Fail(Explain(run, index, state));
            }
            if (run.Checkpoints.TryGetValue(index, out var snapshot))
            {
                Assert.Equal(snapshot, Hash.StableStringify(state));
            }
            var violations = Invariants.Check(state, content);
            Assert.True(violations.Count == 0, $"{file} after {index} endTurns: {string.Join("; ", violations)}");
            matched++;
        }
        Assert.Equal(run.Hashes.Count, matched);
        Assert.NotNull(last);
        Assert.Equal(run.EndReason, last!.Meta.EndReason);
        output.WriteLine($"{file}: {matched} hashes matched ({run.Turns} turns, {run.Log.Count} log entries, end {run.EndReason})");
    }

    /// <summary>Localises a divergence: which sections differ, and the first differing JSON paths at the nearest checkpoint.</summary>
    private static string Explain(Run run, int index, GameState state)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{run.File}: state hash differs after {index} endTurns (turn {state.Meta.Turn}).");
        var mine = SectionHashes(state);
        var theirs = run.SectionHashes[index];
        var differing = mine.Keys.Where(k => !theirs.TryGetValue(k, out var h) || h != mine[k]).ToList();
        sb.AppendLine($"  differing sections: {string.Join(", ", differing)}");

        int? checkpoint = run.Checkpoints.Keys.Where(k => k >= index).Cast<int?>().FirstOrDefault();
        if (checkpoint is null)
        {
            sb.AppendLine("  (no checkpoint at or after this month)");
            return sb.ToString();
        }
        try
        {
            var at = Replay(run).First(x => x.Index == checkpoint.Value).State;
            using var expected = JsonDocument.Parse(run.Checkpoints[checkpoint.Value]);
            using var actual = JsonDocument.Parse(Hash.StableStringify(at));
            sb.AppendLine($"  first differences against the checkpoint after {checkpoint} endTurns:");
            foreach (var d in Fixtures.Diff(expected.RootElement, actual.RootElement)) sb.AppendLine($"    {d}");
        }
        catch (Exception e)
        {
            sb.AppendLine($"  replay to checkpoint {checkpoint} failed: {e.Message}");
        }
        return sb.ToString();
    }

    [Fact]
    public void FixturesCoverTheInterestingPaths()
    {
        var runs = Fixtures.RunFiles().Select(f => Load((string)f[0])).ToList();
        Assert.True(runs.Count >= 12);
        Assert.True(runs.Select(r => r.File.Split('/')[1].Split('-')[0]).Distinct().Count() >= 4);
        Assert.Contains(runs, r => r.EndReason == EndReasons.TimelineEnd);
        Assert.Contains(runs, r => r.EndReason == EndReasons.Bankrupt);
        Assert.Contains(runs, r => r.EndReason == EndReasons.ShutDown);
        Assert.Contains(runs, r => r.Options.BackgroundId == "showman");
        var actions = runs.SelectMany(r => r.Log).Where(e => e.Action is not null).Select(e => e.Action!.Type).Distinct().ToList();
        foreach (var type in new[] { "BuyIngredient", "CreateFormula", "Reformulate", "SetClaims", "SetAdSpend", "BrewBatch", "ResolveEvent", "SetPrice" })
            Assert.Contains(type, actions);
    }
}
