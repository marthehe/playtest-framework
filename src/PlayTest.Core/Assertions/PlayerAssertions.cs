namespace PlayTest.Core.Assertions;

using Demo.PlayPlatform.Players;

public static class PlayerAssertions
{
    public static void ShouldBeActive(this Player player)
    {
        if (player.Status != PlayerStatus.Active)
            throw new AssertionException($"Expected player '{player.Username}' to be Active but was {player.Status}.");
    }

    public static void ShouldHaveUsername(this Player player, string expected)
    {
        if (!string.Equals(player.Username, expected, StringComparison.Ordinal))
            throw new AssertionException($"Expected username '{expected}' but got '{player.Username}'.");
    }
}

public class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}
