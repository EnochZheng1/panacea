using System.Text.Json;

namespace Panacea.Sim;

/// <summary>
/// Save files (packages/sim/src/save.ts): <c>{ "saveVersion": n, "state": … }</c> as JSON,
/// compatible in both directions with the TypeScript build (deserializeGame / serializeGame).
/// Numbers and strings are written exactly as JavaScript's JSON.stringify would write them; only
/// the property order can differ from a TS-written save, which no JSON reader depends on.
/// </summary>
public static class SaveFile
{
    /// <summary>MIGRATIONS[n] upgrades a version-n state (as JSON) to version n+1. None yet.</summary>
    private static readonly Dictionary<int, Func<JsonElement, JsonElement>> Migrations = new();

    public static string Serialize(GameState state)
    {
        var sb = new System.Text.StringBuilder(64 * 1024);
        sb.Append("{\"saveVersion\":").Append(GameState.StateVersion).Append(",\"state\":");
        JsJson.Write(sb, StateJson.ToElement(state), sortKeys: false);
        sb.Append('}');
        return sb.ToString();
    }

    public static GameState Deserialize(string json)
    {
        JsonElement root;
        try
        {
            root = JsonDocument.Parse(json).RootElement;
        }
        catch (JsonException e)
        {
            throw new InvalidDataException("not a Panacea save file", e);
        }
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("saveVersion", out var versionElement) || versionElement.ValueKind != JsonValueKind.Number ||
            !root.TryGetProperty("state", out var stateElement) || stateElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException("not a Panacea save file");
        }
        double version = versionElement.GetDouble();
        if (version > GameState.StateVersion)
        {
            throw new InvalidDataException(
                $"save version {JsJson.FormatNumber(version)} is newer than this build ({GameState.StateVersion})");
        }
        for (int v = (int)version; v < GameState.StateVersion; v++)
        {
            if (!Migrations.TryGetValue(v, out var migrate)) throw new InvalidDataException($"no migration from save version {v}");
            stateElement = migrate(stateElement);
        }
        return stateElement.Deserialize<GameState>(StateJson.Options) ?? throw new InvalidDataException("empty state");
    }
}
