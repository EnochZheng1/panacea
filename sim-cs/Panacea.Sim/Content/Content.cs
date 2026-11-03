using System.Reflection;
using System.Text.Json;

namespace Panacea.Sim;

/// <summary>Id lookups over the content lists (packages/content/src/pack.ts `byId`).</summary>
public sealed class ContentById
{
    public required IReadOnlyDictionary<string, Ingredient> Ingredients { get; init; }
    public required IReadOnlyDictionary<string, Process> Processes { get; init; }
    public required IReadOnlyDictionary<string, Unlock> Unlocks { get; init; }
    public required IReadOnlyDictionary<string, GameEvent> Events { get; init; }
    public required IReadOnlyDictionary<string, Background> Backgrounds { get; init; }
    public required IReadOnlyDictionary<string, Segment> Segments { get; init; }
    public required IReadOnlyDictionary<string, Ailment> Ailments { get; init; }
    public required IReadOnlyDictionary<string, Claim> Claims { get; init; }
}

/// <summary>Validated content, ready for the simulation. Immutable by convention.</summary>
public sealed class ContentPack
{
    public required Config Config { get; init; }
    public required IReadOnlyList<Ingredient> Ingredients { get; init; }
    public required IReadOnlyList<Process> Processes { get; init; }
    public required IReadOnlyList<Unlock> Unlocks { get; init; }
    public required IReadOnlyList<GameEvent> Events { get; init; }
    public required IReadOnlyList<TimelineEntry> Timeline { get; init; }
    public required IReadOnlyList<Regular> Regulars { get; init; }
    public required IReadOnlyList<Background> Backgrounds { get; init; }
    public required IReadOnlyList<Segment> Segments { get; init; }
    public required IReadOnlyList<Ailment> Ailments { get; init; }
    public required IReadOnlyList<Claim> Claims { get; init; }
    public required Names Names { get; init; }
    /// <summary>Source-language strings (English), by key.</summary>
    public required IReadOnlyDictionary<string, string> Strings { get; init; }
    public required ContentById ById { get; init; }

    /// <summary>The string for a key, or the key itself if missing.</summary>
    public string Text(string key) => Strings.TryGetValue(key, out var s) ? s : key;
}

/// <summary>Loads the game's content (packages/content/src/load.ts).</summary>
public static class Content
{
    private static readonly Lazy<ContentPack> Embedded = new(() => Load(ReadEmbedded));

    /// <summary>The content embedded in this assembly (cached; the pack is immutable).</summary>
    public static ContentPack LoadEmbedded() => Embedded.Value;

    /// <summary>
    /// Loads content from JSON texts. <paramref name="read"/> maps a file name such as
    /// "data/config.json" or "strings/en.json" to its text.
    /// </summary>
    public static ContentPack Load(Func<string, string> read)
    {
        var o = StateJson.Options;
        T Parse<T>(string file) =>
            JsonSerializer.Deserialize<T>(read(file), o) ?? throw new InvalidDataException($"{file} is empty");

        var config = Parse<Config>("data/config.json");
        var ingredients = Parse<List<Ingredient>>("data/ingredients.json");
        var processes = Parse<List<Process>>("data/processes.json");
        var unlocks = Parse<List<Unlock>>("data/unlocks.json");
        var events = Parse<List<GameEvent>>("data/events.json");
        var timeline = Parse<List<TimelineEntry>>("data/timeline.json");
        var regulars = Parse<List<Regular>>("data/regulars.json");
        var backgrounds = Parse<List<Background>>("data/backgrounds.json");
        var segments = Parse<List<Segment>>("data/segments.json");
        var ailments = Parse<List<Ailment>>("data/ailments.json");
        var claims = Parse<List<Claim>>("data/claims.json");
        var names = Parse<Names>("data/names.json");
        var strings = Parse<Dictionary<string, string>>("strings/en.json");

        static IReadOnlyDictionary<string, T> ById<T>(IEnumerable<T> items, Func<T, string> id)
        {
            var d = new Dictionary<string, T>(StringComparer.Ordinal);
            foreach (var item in items) d[id(item)] = item; // last wins, like `new Map(entries)`
            return d;
        }

        return new ContentPack
        {
            Config = config,
            Ingredients = ingredients,
            Processes = processes,
            Unlocks = unlocks,
            Events = events,
            Timeline = timeline,
            Regulars = regulars,
            Backgrounds = backgrounds,
            Segments = segments,
            Ailments = ailments,
            Claims = claims,
            Names = names,
            Strings = strings,
            ById = new ContentById
            {
                Ingredients = ById(ingredients, x => x.Id),
                Processes = ById(processes, x => x.Id),
                Unlocks = ById(unlocks, x => x.Id),
                Events = ById(events, x => x.Id),
                Backgrounds = ById(backgrounds, x => x.Id),
                Segments = ById(segments, x => x.Id),
                Ailments = ById(ailments, x => x.Id),
                Claims = ById(claims, x => x.Id),
            },
        };
    }

    /// <summary>Loads content from a directory laid out like packages/content (data/, strings/).</summary>
    public static ContentPack LoadFromDirectory(string root) =>
        Load(file => File.ReadAllText(Path.Combine(root, file)));

    private static string ReadEmbedded(string file)
    {
        string name = "Panacea.Content." + file.Replace('/', '.');
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new InvalidDataException($"embedded content \"{name}\" is missing");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
