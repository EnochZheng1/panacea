namespace Panacea.Game;

/// <summary>Placeholder until the C# simulation port lands: serves the canned game.</summary>
public static class SimGame
{
	public static IGame Start(string seed) => new FakeGame();
}
